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

namespace MySqlServer
{
    public class Server
    {
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

        private int _ReceiveBufferSize = 4096;
        private int _IdleClientTimeoutSeconds = 0;

        private string _ListenerIp;
        private IPAddress _IPAddress;
        private int _Port;
        private bool _UseSsl = false;
        private string _CertFilename;
        private string _CertPassword;

        private X509Certificate2 _SslCertificate = null;
        private X509Certificate2Collection _SslCertificateCollection = null;

        private ConcurrentDictionary<string, ClientMetadata> _Clients = new ConcurrentDictionary<string, ClientMetadata>();
        private ConcurrentDictionary<string, DateTime> _ClientsLastSeen = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, DateTime> _ClientsKicked = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, DateTime> _ClientsTimedout = new ConcurrentDictionary<string, DateTime>();
        
        private TcpListener _Listener = null;
        private bool _Running;

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private CancellationToken _Token;

        private DatabaseController _DatabaseController;

        private bool Debug = true;

        #region Constructors-and-Factories

        public Server(string listenerIp= "127.0.0.1", int port=3306, string CertFilename=null, string CertPassword=null)
        {
            if (String.IsNullOrEmpty(listenerIp)) throw new ArgumentNullException(nameof(listenerIp));
            if (port < 0) throw new ArgumentException("Port must be zero or greater.");

            _ListenerIp = listenerIp;
            _IPAddress = IPAddress.Parse(_ListenerIp);
            _Port = port;
            _CertFilename = CertFilename;
            _CertPassword = CertPassword;
            _Running = false;
            _Token = _TokenSource.Token;

            _DatabaseController = new DatabaseController();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start synchronous server
        /// </summary>
        public void StartSync()
        {
            _IPAddress = IPAddress.Parse(_ListenerIp);
            _Listener = new TcpListener(_IPAddress, _Port);
            _Listener.Start();
            ClientMetadata client;
            while (true)
            {
                try
                {
                    Log("Waiting connection ... ");
                    byte[] buffer;
                    // Setup client
                    TcpClient tcpClient = _Listener.AcceptTcpClient();
                    client = new ClientMetadata(tcpClient);

                    client.ClientConnected();

                    if (client._UseSsl)
                    {
                        _SslCertificate = new X509Certificate2(_CertFilename, _CertPassword);

                        _SslCertificateCollection = new X509Certificate2Collection { _SslCertificate };

                        if (AcceptInvalidCertificates)
                        {
                            client.SslStream = new SslStream(client.NetworkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate));
                        }
                        else
                        {
                            client.SslStream = new SslStream(client.NetworkStream, false);
                        }

                        bool success = StartTls(client);

                        if (!success)
                        {
                            client.Dispose();
                        }
                        else
                        {
                            buffer = new byte[_ReceiveBufferSize];
                            client.SslStream.Read(buffer);
                            client.DataReceived(buffer);
                        }
                    }

                    while (client._ServerPhase == ClientMetadata.Phase.CommandPhase)
                    {
                        buffer = new Byte[_ReceiveBufferSize];
                        if (!client._UseSsl)
                        {
                            client.NetworkStream.Read(buffer);
                        }
                        else
                        {
                            client.SslStream.Read(buffer);
                        }

                        client.DataReceived(buffer);
                    }
                }
                catch (Exception e)
                {
                    Log("disconnect");
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// Start asynchronous server
        /// </summary>
        /// <returns></returns>
        public void StartAsync()
        {

        }

        #endregion

        #region Private-Methods

        #region Tcp connection part

        private bool StartTls(ClientMetadata client)
        {
            try
            {
                client.SslStream.AuthenticateAsServer(
                    _SslCertificate,
                    MutuallyAuthenticate,
                    SslProtocols.Tls12,
                    !AcceptInvalidCertificates);

                if (!client.SslStream.IsEncrypted)
                {
                    Log("[" + client.IpPort + "] not encrypted");
                    client.Dispose();
                    return false;
                }

                if (!client.SslStream.IsAuthenticated)
                {
                    Log("[" + client.IpPort + "] stream not authenticated");
                    client.Dispose();
                    return false;
                }

                if (MutuallyAuthenticate && !client.SslStream.IsMutuallyAuthenticated)
                {
                    Log("[" + client.IpPort + "] failed mutual authentication");
                    client.Dispose();
                    return false;
                }
            }
            catch (Exception e)
            {
                Log("[" + client.IpPort + "] TLS exception" + Environment.NewLine + e.ToString());
                client.Dispose();
                return false;
            }
            Log("ssl stream ok");
            return true;
        }

        private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // return true; // Allow untrusted certificates.
            return AcceptInvalidCertificates;
        }

        #endregion

        private void Log(string msg)
        {
            if (Debug) Console.WriteLine(msg);
        }

        #endregion
    }
}
