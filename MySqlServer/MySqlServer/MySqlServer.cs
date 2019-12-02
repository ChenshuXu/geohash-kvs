using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using OpenSSL.X509Certificate2Provider;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;

namespace MySqlServer
{
    public class Server
    {
        /// <summary>
        /// Maximum amount of time to wait before considering a client idle and disconnecting them. 
        /// By default, this value is set to 0, which will never disconnect a client due to inactivity.
        /// The timeout is reset any time a message is received from a client or a message is sent to a client.
        /// For instance, if you set this value to 30, the client will be disconnected if the server has not received a message from the client within 30 seconds or if a message has not been sent to the client in 30 seconds.
        /// </summary>
        public int IdleClientTimeoutSeconds
        {
            get
            {
                return _IdleClientTimeoutSeconds;
            }
            set
            {
                if (value < 0) throw new ArgumentException("IdleClientTimeoutSeconds must be zero or greater.");
                _IdleClientTimeoutSeconds = value;
            }
        }

        public string CertFilename
        {
            get { return _CertFilename; }
        }

        public string CertPassword
        {
            get { return _CertPassword; }
        }

        /// <summary>
        /// Enable or disable console debugging.
        /// </summary>
        public bool Debug = false;

        // Enable or disable acceptance of invalid SSL certificates.
        public bool AcceptInvalidCertificates = true;
        // Enable or disable mutual authentication of SSL client and server.
        public bool MutuallyAuthenticate = false;

        /// <summary>
        /// Reason why a client disconnected.
        /// </summary>
        public enum DisconnectReason
        {
            /// <summary>
            /// Normal disconnection.
            /// </summary>
            Normal = 0,
            /// <summary>
            /// Client connection was intentionally terminated programmatically or by the server.
            /// </summary>
            Kicked = 1,
            /// <summary>
            /// Client connection timed out; server did not receive data within the timeout window.
            /// </summary>
            Timeout = 2
        }

        private int _IdleClientTimeoutSeconds = 0;

        private string _ListenerIp;
        private int _ListenerPort;
        private IPAddress _IPAddress;
        private TcpListener _Listener = null;
        private string _CertFilename;
        private string _CertPassword;

        private ConcurrentDictionary<string, DateTime> _UnauthenticatedClients = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, ClientSession> _Clients = new ConcurrentDictionary<string, ClientSession>();
        private ConcurrentDictionary<string, DateTime> _ClientsLastSeen = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, DateTime> _ClientsKicked = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, DateTime> _ClientsTimedout = new ConcurrentDictionary<string, DateTime>();
        
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private CancellationToken _Token;

        private DatabaseController _DatabaseController;

        #region Constructors-and-Factories

        public Server(string listenerIp= "127.0.0.1",
            int port=3306,
            string CertFilename=null,
            string CertPassword=null)
        {
            _ListenerIp = listenerIp;
            _IPAddress = IPAddress.Parse(_ListenerIp);
            _ListenerPort = port;
            _CertFilename = CertFilename;
            _CertPassword = CertPassword;
            _Token = _TokenSource.Token;

            _DatabaseController = new DatabaseController();
            _DatabaseController.Debug = Debug;

            Task.Run(() => MonitorForIdleClients(), _Token);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start synchronous server, single client
        /// </summary>
        public void StartSync()
        {
            _IPAddress = IPAddress.Parse(_ListenerIp);
            _Listener = new TcpListener(_IPAddress, _ListenerPort);
            _Listener.Start();
            
            while (true)
            {
                try
                {
                    Log("Waiting connection ... ");
                    // Setup client
                    TcpClient tcpClient = _Listener.AcceptTcpClient();
                    ClientSession client = new ClientSession(tcpClient, this, _DatabaseController);
                    client.Debug = Debug;

                    client.ClientConnected();

                    while(true)
                    {
                        byte[] buffer = new Byte[client._ReceiveBufferSize];
                        if (!client._UseSsl)
                        {
                            client.NetworkStream.Read(buffer);
                        }
                        else if (client._UseSsl)
                        {
                            client.SslStream.Read(buffer);
                        }

                        client.DataReceived(buffer);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// Start asynchronous server, multi client
        /// </summary>
        /// <returns></returns>
        public void StartAsync()
        {
            Log("server starting on " + _ListenerIp + ":" + _ListenerPort);
            _Listener = new TcpListener(_IPAddress, _ListenerPort);
            _Listener.Start();

            _Clients = new ConcurrentDictionary<string, ClientSession>();

            Task.Run(() => AcceptConnections(), _Token);
        }

        public void Start()
        {
            Log("server starting on " + _ListenerIp + ":" + _ListenerPort);
            _Listener = new TcpListener(_IPAddress, _ListenerPort);
            _Listener.Start();
        }

        public void Restart()
        {

        }

        public void Stop()
        {
            
        }

        #endregion

        #region Private-Methods

        private async Task ClientConnected(string ipPort)
        {
            Log("[" + ipPort + "] client connected function");
            _Clients[ipPort].ClientConnected();
        }

        private async Task DataReceived(string ipPort, byte[] data)
        {
            Log("[" + ipPort + "] data received function");
            _Clients[ipPort].DataReceived(data);
        }

        private async Task ClientDisconnected(string ipPort, DisconnectReason reason)
        {
            Log("[" + ipPort + "] client disconnected function: " + reason.ToString());
            _Clients[ipPort].ClientDisconnected();
        }

        /// <summary>
        /// Disconnects the specified client.
        /// </summary>
        /// <param name="ipPort">IP:port of the client.</param>
        public void DisconnectClient(string ipPort)
        {
            if (String.IsNullOrEmpty(ipPort)) throw new ArgumentNullException(nameof(ipPort));

            if (!_Clients.TryGetValue(ipPort, out ClientSession client))
            {
                Log("*** DisconnectClient unable to find client " + ipPort);
            }
            else
            {
                if (!_ClientsTimedout.ContainsKey(ipPort))
                {
                    Log("[" + ipPort + "] kicking");
                    _ClientsKicked.TryAdd(ipPort, DateTime.Now);
                }

                _Clients.TryRemove(client.IpPort, out ClientSession destroyed);
                client.Dispose();
                Log("[" + ipPort + "] disposed");
            }
        }

        #region Tcp connection part

        private async Task AcceptConnections()
        {
            
            while (true)
            {
                
                ClientSession client = null;

                try
                {
                    Log("[] before AcceptTcpClientAsync");
                    TcpClient tcpClient = await _Listener.AcceptTcpClientAsync();
                    //TcpClient tcpClient = _Listener.AcceptTcpClient();
                    Log("[] after AcceptTcpClientAsync");
                    string clientIp = tcpClient.Client.RemoteEndPoint.ToString();

                    Log("[" + clientIp + "] starting data receiver");

                    client = new ClientSession(tcpClient, this, _DatabaseController);
                    client.Debug = Debug;

                    _Clients.TryAdd(clientIp, client);
                    _ClientsLastSeen.TryAdd(clientIp, DateTime.Now);

                    await Task.Run(() => ClientConnected(clientIp));

                    Task dataRecv = Task.Run(() => DataReceiver(client), _Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    if (client != null) client.Dispose();
                    continue;
                }
                catch (Exception e)
                {
                    if (client != null) client.Dispose();
                    Log("*** AcceptConnections exception: " + e.ToString());
                    continue;
                }
                finally
                {
                }
            }
        }


        private async Task DataReceiver(ClientSession client)
        {
            string header = "[" + client.IpPort + "]";
            Log(header + " data receiver started");
            Task unawaited = null;

            while (true)
            {
                try
                {
                    if (!IsClientConnected(client.TcpClient))
                    {
                        Log(header + " client no longer connected");
                        break;
                    }

                    if (client.Token.IsCancellationRequested)
                    {
                        Log(header + " cancellation requested");
                        break;
                    }

                    byte[] data = await DataReadAsync(client);
                    if (data == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }
                    unawaited = Task.Run(() => DataReceived(client.IpPort, data));
                }
                catch (Exception e)
                {
                    Log(
                        Environment.NewLine +
                        header + " data receiver exception:" +
                        Environment.NewLine +
                        e.ToString() +
                        Environment.NewLine);

                    break;
                }
            }

            Log(header + " data receiver terminated");

            unawaited = null;

            if (_ClientsKicked.ContainsKey(client.IpPort))
            {
                unawaited = Task.Run(() => ClientDisconnected(client.IpPort, DisconnectReason.Kicked));
            }
            else if (_ClientsTimedout.ContainsKey(client.IpPort))
            {
                unawaited = Task.Run(() => ClientDisconnected(client.IpPort, DisconnectReason.Timeout));
            }
            else
            {
                unawaited = Task.Run(() => ClientDisconnected(client.IpPort, DisconnectReason.Normal));
            }
            

            DateTime removedTs;
            _Clients.TryRemove(client.IpPort, out ClientSession destroyed);
            _ClientsLastSeen.TryRemove(client.IpPort, out removedTs);
            _ClientsKicked.TryRemove(client.IpPort, out removedTs);
            _ClientsTimedout.TryRemove(client.IpPort, out removedTs);

            client.Dispose();
        }

        private async Task<byte[]> DataReadAsync(ClientSession client)
        {
            if (client.Token.IsCancellationRequested) throw new OperationCanceledException();
            if (!client.NetworkStream.CanRead) return null;
            if (!client.NetworkStream.DataAvailable) return null;

            byte[] buffer = new byte[client._ReceiveBufferSize];
            int read = 0;

            while (true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    if (!client._UseSsl)
                    {
                        Log("read NetworkStream");
                        read = await client.NetworkStream.ReadAsync(buffer, 0, buffer.Length);
                    }
                    else if (client._UseSsl)
                    {
                        Log("read SslStream");
                        read = await client.SslStream.ReadAsync(buffer, 0, buffer.Length);
                    }

                    if (read > 0)
                    {
                        ms.Write(buffer, 0, read);
                        return ms.ToArray();
                    }
                    else
                    {
                        throw new SocketException();
                    }
                }
            }
        }

        private async Task MonitorForIdleClients()
        {
            while (!_Token.IsCancellationRequested)
            {
                if (_IdleClientTimeoutSeconds > 0 && _ClientsLastSeen.Count > 0)
                {
                    MonitorForIdleClientsTask();
                }
                await Task.Delay(5000, _Token);
            }
        }

        private void MonitorForIdleClientsTask()
        {
            DateTime idleTimestamp = DateTime.Now.AddSeconds(-1 * _IdleClientTimeoutSeconds);

            foreach (KeyValuePair<string, DateTime> curr in _ClientsLastSeen)
            {
                if (curr.Value < idleTimestamp)
                {
                    _ClientsTimedout.TryAdd(curr.Key, DateTime.Now);
                    Log("Disconnecting client " + curr.Key + " due to idle timeout");
                    DisconnectClient(curr.Key);
                }
            }
        }

        private void UpdateClientLastSeen(string ipPort)
        {
            if (_ClientsLastSeen.ContainsKey(ipPort))
            {
                DateTime ts;
                _ClientsLastSeen.TryRemove(ipPort, out ts);
            }

            _ClientsLastSeen.TryAdd(ipPort, DateTime.Now);
        }

        private bool IsClientConnected(System.Net.Sockets.TcpClient client)
        {
            if (client.Connected)
            {
                if ((client.Client.Poll(0, SelectMode.SelectWrite)) && (!client.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        #endregion

        public void Log(string msg)
        {
            if (Debug)
            {
                string timeStr = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();
                Console.WriteLine("[" + timeStr + "]" + msg);
            }
        }

        #endregion
    }
}
