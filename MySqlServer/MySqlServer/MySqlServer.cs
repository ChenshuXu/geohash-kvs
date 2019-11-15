using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using OpenSSL.X509Certificate2Provider;

namespace MySqlServer
{
    public class MySqlServer
    {
        static uint CLIENT_CONNECT_WITH_DB = 0x00000008;
        static uint CLIENT_SECURE_CONNECTION = 0x00008000;
        static uint CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA = 0x00200000;
        static uint CLIENT_PLUGIN_AUTH = 0x00080000;
        static uint CLIENT_CONNECT_ATTRS = 0x00100000;
        static uint CLIENT_SSL = 0x00000800;
        static uint CLIENT_PROTOCOL_41 = 0x00000200;

        // Enable or disable acceptance of invalid SSL certificates.
        public bool AcceptInvalidCertificates = true;
        // Enable or disable mutual authentication of SSL client and server.
        public bool MutuallyAuthenticate = false;

        enum Phase { Waiting, ConnectionPhase, CommandPhase };
        private Phase _ServerPhase = Phase.Waiting;

        private int _ReceiveBufferSize = 4096;
        private string _ListenerIp = "127.0.0.1";
        private IPAddress _IPAddress;
        private int _Port = 3307; // default port
        private bool _UseSsl = false;
        private string _CertFilename;
        private string _CertPassword;

        private TcpListener _Listener = null;
        private bool _Running;

        private ClientMetadata _Client = null;

        private Database _Database;

        private X509Certificate2 _SslCertificate = null;
        private X509Certificate2Collection _SslCertificateCollection = null;

        private int _Sequence = 0;
        private byte[] _Salt1; // 8 bytes
        private byte[] _Salt2; // 12 bytes
        private string _Password = "bG43JPmBrY92";

        private bool Debug = true;

        #region Constructors-and-Factories

        public MySqlServer(string CertFilename, string CertPassword)
        {
            _CertFilename = CertFilename;
            _CertPassword = CertPassword;

            _Database = new Database();
        }

        #endregion

        #region Public-Methods

        public void ExecuteServer()
        {
            try
            {
                _IPAddress = IPAddress.Parse(_ListenerIp);
                _Listener = new TcpListener(_IPAddress, _Port);
                _Listener.Start();

                while (true)
                {
                    Log("Waiting connection ... ");
                    _UseSsl = false;

                    // Setup client
                    TcpClient tcpClient = _Listener.AcceptTcpClient();
                    _Client = new ClientMetadata(tcpClient);

                    // Connection phase
                    Log("start handshake");
                    SetState(Phase.ConnectionPhase);
                    HandleHandshake();

                    byte[] buffer = new byte[_ReceiveBufferSize];
                    _Client.NetworkStream.Read(buffer);
                    HandleHandshakeResponse(buffer);

                    if (_UseSsl)
                    {
                        _SslCertificate = new X509Certificate2(_CertFilename, _CertPassword);

                        _SslCertificateCollection = new X509Certificate2Collection
                        {
                            _SslCertificate
                        };
                        
                        if (AcceptInvalidCertificates)
                        {
                            _Client.SslStream = new SslStream(_Client.NetworkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate));
                        }
                        else
                        {
                            _Client.SslStream = new SslStream(_Client.NetworkStream, false);
                        }

                        bool success = StartTls(_Client);

                        if (!success)
                        {
                            _Client.Dispose();
                            continue;
                        }
                        else
                        {
                            buffer = new byte[_ReceiveBufferSize];
                            _Client.SslStream.Read(buffer);
                            HandleHandshakeResponse(buffer);
                        }

                    }


                    // Command phase
                    _Sequence = 0;
                    while (_ServerPhase == Phase.CommandPhase)
                    {
                        buffer = new Byte[_ReceiveBufferSize];
                        if (!_UseSsl)
                        {
                            _Client.NetworkStream.Read(buffer);
                        }
                        else
                        {
                            _Client.SslStream.Read(buffer);
                        }

                        HandleCommand(buffer);
                        _Sequence = 0;
                        
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #endregion

        #region Private-Methods

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

        private void DisconnectClient()
        {
            Log("disconnect");
            _Client.Dispose();
        }

        // State machine
        private void SetState(Phase phase)
        {
            // exit state
            switch (_ServerPhase)
            {
                // exit command phase
                case Phase.CommandPhase:
                    break;
            }

            _ServerPhase = phase;

            // enter state
        }

        /// <summary>
        /// Send initial handshake packet
        /// </summary>
        private void HandleHandshake()
        {
            List<byte> packet = new List<byte>();

            // 1 byte protocol version
            packet.Add(0x0a);
            // server version
            byte[] server_version = NulTerminatedString("5.7.28");
            packet.AddRange(server_version);
            // 4 bytes connection id
            byte[] connection_id = BitConverter.GetBytes(30);
            packet.AddRange(connection_id); // thread id

            // Salt1
            // first 8 bytes of the auth-plugin data
            // last byte 0x00 as filler
            GenerateSalt1();
            packet.AddRange(_Salt1);
            packet.Add(0x00);

            //Server Capabilities: 0xffff
            packet.Add(0xff);
            packet.Add(0xff);

            // Server Language: Unknown (255)
            packet.Add(0xff);

            // Server Status: 0x0200
            packet.Add(0x02);
            packet.Add(0x00);

            // Extended Server Capabilities: 0xffx7
            packet.Add(0xff);
            packet.Add(0xc7);

            // Authentication Plugin Length: 21
            packet.Add(0x15);

            // Unused: 00000000000000000000
            for (var i = 0; i < 10; i++)
            {
                packet.Add(0x00);
            }

            // Salt2
            GenerateSalt2();
            packet.AddRange(_Salt2);
            packet.Add(0x00);

            // Authentication Plugin: mysql_native_password
            packet.AddRange(NulTerminatedString("mysql_native_password"));

            SendPacket(packet.ToArray());
            Console.WriteLine("send initial handshake");
        }

        /// <summary>
        /// Handle handshake response packet
        /// </summary>
        /// <param name="data">response data</param>
        private void HandleHandshakeResponse(byte[] data)
        {
            Log("handle handshake response");
            GetSequence();

            // get mysql packet length
            // first 3 bytes is packet length, fixed length integer
            byte[] packetLengthBytes = new byte[3];
            Array.Copy(data, packetLengthBytes, 3);
            int packetLengthInt = FixedLengthInteger_toInt(packetLengthBytes);
            Console.WriteLine("packetLength: {0}", packetLengthInt);

            byte sequence = data[3];
            //Console.WriteLine("sequence: {0}", (int)sequence);

            // get login request package
            byte[] loginRequestBytes = SubArray(data, 4, packetLengthInt);
            long currentHead = 0;

            // capability flags
            byte[] clientCapabilitiesBytes = SubArray(loginRequestBytes, currentHead, 4); // client capabilities
            currentHead += 4;
            uint clientCapabilities = BitConverter.ToUInt32(clientCapabilitiesBytes, 0);
            _Client.Capabilities = clientCapabilities;

            // max-packet size
            byte[] maxPacketSize = SubArray(loginRequestBytes, currentHead, 4);
            currentHead += 4;

            // character set
            byte[] characterSet = SubArray(loginRequestBytes, currentHead, 1);
            currentHead += 1;

            // reserved unused
            currentHead += 23;

            // Switch to SSL exchange
            if (Convert.ToBoolean(clientCapabilities & CLIENT_SSL) && !_UseSsl)
            {
                _UseSsl = true;
                Console.WriteLine("switch to ssl");
                return;
            }


            // username, string nul type
            byte[] usernameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
            string usernameStr = NulTerminatedString_bytesToString(usernameBytes);
            currentHead += NulTerminatedString_stringLength(usernameBytes);
            Console.WriteLine("username: {0}", usernameStr);

            // TODO: Get user password from database
            byte[] passwordInput = Encoding.ASCII.GetBytes(_Password);
            bool passedVerification = false;

            // auth-response
            // check capability flags
            if (Convert.ToBoolean(clientCapabilities & CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA))
            {
                // lenenc-int password length
                byte[] passwordLengthBytes = SubArray(loginRequestBytes, currentHead, 8);
                long passwordLength = LengthEncodedInteger_toInt(passwordLengthBytes);
                currentHead += LengthEncodedInteger_intLength(passwordLengthBytes);

                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Console.WriteLine("password length: {0}, {1}", passwordLength, ByteArrayToHexString(passwordBytes));

                passedVerification = VerifyPassword(passwordBytes, passwordInput);
            }
            else if (Convert.ToBoolean(clientCapabilities & CLIENT_SECURE_CONNECTION))
            {
                // 1 byte password length
                int passwordLength = Convert.ToInt32(loginRequestBytes[currentHead]);
                currentHead += 1;

                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Console.WriteLine("password length: {0}, {1}", passwordLength, ByteArrayToHexString(passwordBytes));

                passedVerification = VerifyPassword(passwordBytes, passwordInput);
            }
            else
            {
                // string nul type password
                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                int passwordLength = NulTerminatedString_stringLength(passwordBytes);
                byte[] password = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Console.WriteLine("password length: {0}, {1}", passwordLength, ByteArrayToHexString(password));

                passedVerification = VerifyPassword(passwordBytes, passwordInput);
            }

            // If have database name input
            if (Convert.ToBoolean(clientCapabilities & CLIENT_CONNECT_WITH_DB))
            {
                // string nul type
                byte[] databaseNameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                string databaseName = NulTerminatedString_bytesToString(databaseNameBytes);
                currentHead += NulTerminatedString_stringLength(databaseNameBytes);
                Console.WriteLine("database string bytes length {0} name: {1}", NulTerminatedString_stringLength(databaseNameBytes), databaseName);
                // TODO: set connected database name
            }

            // If has auth plugin name
            if (Convert.ToBoolean(clientCapabilities & CLIENT_PLUGIN_AUTH))
            {
                // string nul type
                byte[] authNameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                string authPluginName = NulTerminatedString_bytesToString(authNameBytes);
                currentHead += NulTerminatedString_stringLength(authNameBytes);
                Console.WriteLine("auth plugin string bytes length {0} name: {1}", NulTerminatedString_stringLength(authNameBytes), authPluginName);
            }

            // If have client connect attributs
            if (Convert.ToBoolean(clientCapabilities & CLIENT_CONNECT_ATTRS))
            {
                // TODO:lenenc int type, length of all key-values
            }

            if (passedVerification)
            { 
                SendOkPacket();
                SetState(Phase.CommandPhase);
            }
            else
            {
                // fail login
                SendErrPacket("wrong password");
                // Disconnect
                DisconnectClient();
                SetState(Phase.Waiting);
            }

        }

        private void HandleCommand(byte[] data)
        {

            // Get packet length and sequence number
            byte[] packetLength = new byte[3];
            byte sequence = data[3];
            Array.Copy(data, packetLength, 3);
            int packetLengthInt = packetLength[0];
            Console.WriteLine("sql packet length: {0}", packetLengthInt);
            Console.WriteLine("sequence: {0}", (int)sequence);

            // Get text protocol
            byte[] requestCommandQuery = new byte[packetLengthInt];
            Array.Copy(data, 4, requestCommandQuery, 0, packetLengthInt);
            byte textProtocol = requestCommandQuery[0];
            if (textProtocol == 0x03) // COM_QUERY
            {
                Console.WriteLine("get COM_QUERY");
                GetSequence();
                byte[] statement = new byte[packetLengthInt - 1];
                Array.Copy(requestCommandQuery, 1, statement, 0, packetLengthInt - 1);
                HandleQuery(statement);
                
            }
            if (textProtocol == 0x01) // COM_QUIT
            {
                Log("disconnect");
                _Client.Dispose();
                SetState(Phase.Waiting);
            }

        }

        private void HandleQuery(byte[] queryBytes)
        {
            string query = Encoding.ASCII.GetString(queryBytes, 0, queryBytes.Length).ToLower();
            Console.WriteLine("handle query: {0}", query);
            string[] queryArray = query.Split(' ');
            
            if (queryArray[0] == "select")
            {
                HandleSelect(query);
                return;
            }
            if (queryArray[0] == "show")
            {
                Console.WriteLine("handle show");
                return;
            }
            if (queryArray[0] == "set")
            {
                Console.WriteLine("handle set");
                return;
            }
        }

        private void HandleSelect(string query)
        {
            Console.WriteLine("handle {0}", query);
            List<byte> sendPacket = new List<byte>();
            string[] queryArray = query.Split(' ');
            string columnName1 = queryArray[1]; // TODO: can have multiple column names

            if (columnName1 == "@@version_comment")
            {
                Console.WriteLine("at version_comment");
                SendPacket(LengthEncodedInteger(1));
                SendPacket(GetVersionCommentHeaderPacket());
                SendPacket(GetVersionCommentRowPacket());
                SendEofPacket();
                return;
            }

            // get query data
            Header[] h =
            {
                    new Header { name = "Col1", type = "int" },
                    new Header { name = "Col2", type = "str" }
                };
            Row[] r =
            {
                    new Row { Col1 = 1, Col2 = "ok" },
                    new Row { Col1 = 1, Col2 = "A" }
                };
            QueryData data = new QueryData
            {
                headers = h,
                rows = r
            };

            // send length encoded packet
            SendPacket(LengthEncodedInteger(data.headers.Length));

            // send data header packet
            foreach (var header in data.headers)
            {
                SendPacket(GetHeaderPacket(header));
            }

            // send data row packet
            foreach (var row in data.rows)
            {
                SendPacket(GetRowPacket(row));
            }

            // send eof packet
            SendEofPacket();
        }


        private byte[] GetHeaderPacket(Header h)
        {
            List<byte> packet = new List<byte>();

            int character_set = 33; //utf8_general_ci
            int max_col_length = 1024; //This is totally made up.  it shouldn't be
            byte column_type = 0xfd; //Fallback to varchar?
            if (h.type == "int")
            {
                column_type = 0x09;
            }
            else if (h.type == "str")
            {
                column_type = 0xfd;
            }

            packet.AddRange(LengthEncodedString("def"));
            packet.AddRange(LengthEncodedString(h.name));
            packet.AddRange(LengthEncodedString("virtual_table"));
            packet.AddRange(LengthEncodedString("physical_table"));
            packet.AddRange(LengthEncodedString(h.name));
            packet.AddRange(LengthEncodedString(h.name));
            packet.Add(0x0c);
            packet.AddRange(FixedLengthInteger(character_set, 2));
            packet.AddRange(FixedLengthInteger(max_col_length, 4));
            packet.AddRange(FixedLengthInteger(column_type, 1));


            packet.Add(0x00); //Flags?
            packet.Add(0x00);

            packet.Add(0x00); //Only doing ints/static strings now

            packet.Add(0x00); //Filler
            packet.Add(0x00);

            return packet.ToArray();
        }

        private byte[] GetRowPacket(Row r)
        {
            Console.WriteLine("Send row {0}, {1}", r.Col1, r.Col2);
            List<byte> packet = new List<byte>();
            packet.AddRange(LengthEncodedString(r.Col1.ToString()));
            packet.AddRange(LengthEncodedString(r.Col2.ToString()));
            return packet.ToArray();
        }

        private byte[] GetVersionCommentHeaderPacket()
        {
            List<byte> packet = new List<byte>();

            int character_set = 33; //utf8_general_ci
            int max_col_length = 84;
            byte column_type = 0xfd; //var string

            packet.AddRange(LengthEncodedString("def"));
            packet.AddRange(LengthEncodedString(""));
            packet.AddRange(LengthEncodedString(""));
            packet.AddRange(LengthEncodedString(""));
            packet.AddRange(LengthEncodedString("@@version_comment"));
            packet.Add(0x00);
            packet.Add(0x0c);
            packet.AddRange(FixedLengthInteger(character_set, 2)); //utf8_general_ci
            packet.AddRange(FixedLengthInteger(max_col_length, 4));
            packet.AddRange(FixedLengthInteger(column_type, 1)); // var string


            packet.Add(0x00); //Flags
            packet.Add(0x00);

            packet.Add(0x1f); //Decimals: 31

            packet.Add(0x00); //Filler
            packet.Add(0x00);

            return packet.ToArray();
        }

        private byte[] GetVersionCommentRowPacket()
        {
            return LengthEncodedString("MySQL Community Server (GPL)"); // TODO: simplify
        }

        #region Generic Response Packets

        private void SendOkPacket()
        {
            List<byte> ok = new List<byte>();
            ok.Add(0x00); // OK
            ok.Add(0x00); // affected rows
            ok.Add(0x00); // last insert id
            ok.Add(0x02); // Say autocommit was set
            ok.Add(0x00);
            ok.Add(0x00); // No warnings
            ok.Add(0x00);

            SendPacket(ok.ToArray());
            Console.WriteLine("send ok packet");
        }

        private void SendErrPacket(String message)
        {
            List<byte> bytes = new List<byte>();

            bytes.Add(0xff); //[ff] header of the ERR packet

            byte[] errorCode = { 0x15, 0x04 };
            bytes.AddRange(errorCode);

            if (Convert.ToBoolean(_Client.Capabilities & CLIENT_PROTOCOL_41))
            {
                // sql_state_marker, string[1]
                bytes.Add(0x23);

                // sql_state, string[5]
                bytes.AddRange(FixedLengthString("28000", 5));
            }

            bytes.AddRange(RestOfPacketString(message));

            SendPacket(bytes.ToArray());
            Console.WriteLine("send err packet");
        }

        private void SendEofPacket()
        {
            List<byte> packet = new List<byte>();

            // [fe] EOF header
            packet.Add(0xfe);

            if (Convert.ToBoolean(_Client.Capabilities & CLIENT_PROTOCOL_41))
            {
                // number of warnings
                packet.Add(0x00); // warnings
                packet.Add(0x00);

                // Status Flags
                packet.Add(0x02);
                packet.Add(0x00);
            }

            SendPacket(packet.ToArray());
        }

        private void SendPacket(byte[] data)
        {
            List<byte> packet = new List<byte>();
            byte[] length = FixedLengthInteger(data.Length, 3);
            byte[] seq = FixedLengthInteger(GetSequence(), 1);
            packet.AddRange(length);
            packet.AddRange(seq);
            packet.AddRange(data);

            byte[] packetArray = packet.ToArray();

            if (!_UseSsl)
            {
                _Client.NetworkStream.Write(packetArray, 0, packetArray.Length);
                _Client.NetworkStream.Flush();
            }
            else
            {
                _Client.SslStream.Write(packetArray, 0, packetArray.Length);
                _Client.SslStream.Flush();
            }
        }

        #endregion

        private int GetSequence()
        {
            int val = _Sequence;
            _Sequence += 1;
            if (_Sequence > 255)
            {
                _Sequence = 0;
            }
            return val;
        }

        #region Integer types
        // type int<>
        private byte[] FixedLengthInteger(int theInt, int length)
        {
            byte[] resultArray = new byte[length];
            for (int i = length - 1; i >= 0; i--)
            {
                resultArray[i] = (byte)(theInt >> (i * 8));
            }
            return resultArray;
        }

        private int FixedLengthInteger_toInt(byte[] bytes)
        {
            int sum = 0;
            for (var i=bytes.Length-1; i>=0; i--)
            {
                sum += (int)(bytes[i] << (8 * i));
            }
            return sum;
        }

        // type int<lenenc>
        private byte[] LengthEncodedInteger(long value)
        {
            if (value >= 251 && value < Math.Pow(2, 16))
            {
                // 0xfc + 2-byte integer
                return new byte[] { 0xfc, (byte)value, (byte)(value >> 8) };
            }
            if (value >= Math.Pow(2, 16) && value < Math.Pow(2, 24))
            {
                // 0xfd + 3-byte integer
                return new byte[] { 0xfd, (byte)value, (byte)(value >> 8), (byte)(value >> 16) };
            }
            if (value >= Math.Pow(2, 24) && value < Math.Pow(2, 64))
            {
                return new byte[] { 0xfe,
                    (byte)value,
                    (byte)(value >> 8),
                    (byte)(value >> 16),
                    (byte)(value >> 24),
                    (byte)(value >> 32),
                    (byte)(value >> 40),
                    (byte)(value >> 48),
                    (byte)(value >> 56),
                    (byte)(value >> 64)
                }; // 0xfe + 8-byte integer
            }

            // stored as a 1-byte integer
            return new byte[] { (byte)value };
        }

        private long LengthEncodedInteger_toInt(byte[] bytes)
        {
            // 8-byte integer
            if (bytes[0] == 0xfe)
            {
                Console.WriteLine("8-byte int");
                return FixedLengthInteger_toInt(SubArray(bytes, 1, 8));
            }
            // 3-byte integer
            if (bytes[0] == 0xfd)
            {
                Console.WriteLine("3-byte int");
                return FixedLengthInteger_toInt(SubArray(bytes, 1, 3));
            }
            // 2-byte integer
            if (bytes[0] == 0xfc)
            {
                Console.WriteLine("2-byte int");
                return FixedLengthInteger_toInt(SubArray(bytes, 1, 2));
            }

            // 1-byte integer
            Console.WriteLine("1-byte int");
            return bytes[0];
        }

        private int LengthEncodedInteger_intLength(byte[] bytes)
        {
            // 8-byte integer
            if (bytes[0] == 0xfe)
            {
                Console.WriteLine("8-byte int");
                return 8;
            }
            // 3-byte integer
            if (bytes[0] == 0xfd)
            {
                Console.WriteLine("3-byte int");
                return 3;
            }
            // 2-byte integer
            if (bytes[0] == 0xfc)
            {
                Console.WriteLine("2-byte int");
                return 2;
            }

            // 1-byte integer
            Console.WriteLine("1-byte int");
            return 1;
        }
        #endregion

        #region string types
        // type string<lenenc>
        private byte[] LengthEncodedString(string str)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(LengthEncodedInteger(str.Length));
            byte[] stringByte = Encoding.ASCII.GetBytes(str);
            bytes.AddRange(stringByte);
            return bytes.ToArray();
        }

        // type string<fix>
        private byte[] FixedLengthString(string str, int length)
        {
            byte[] bytes = new byte[length];

            try
            {
                Array.Copy(Encoding.ASCII.GetBytes(str), bytes, str.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("string length longer than fix length {0}", ex);
            }
            return bytes;
        }

        // tyoe string<var>
        // The length of the string is determined by another field or is calculated at runtimes
        private byte[] VariableLengthString(string str, int length)
        {
            byte[] bytes = new byte[length];
            Array.Copy(Encoding.ASCII.GetBytes(str), bytes, Math.Min(20, str.Length));
            return bytes;
        }

        // type string<EOF>
        private byte[] RestOfPacketString(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        // type string<NUL>, Strings that are terminated by a [00] byte.
        private byte[] NulTerminatedString(string str)
        {
            List<byte> bytes = new List<byte>();
            byte[] stringByte = Encoding.ASCII.GetBytes(str);
            bytes.AddRange(stringByte);
            bytes.Add(0x00);

            return bytes.ToArray();
        }
        #endregion

        /// <summary>
        /// Convert string<NUL> bytes to sting
        /// </summary>
        /// <param name="bytes">bytes of encoded string nul</param>
        /// <returns>decoded string</returns>
        private string NulTerminatedString_bytesToString(byte[] bytes)
        {
            List<byte> stringBytes = new List<byte>();
            for(var i=0; i<bytes.Length; i++)
            {
                if (bytes[i] == 0x00)
                {
                    break;
                }
                stringBytes.Add(bytes[i]);
            }
            
            return Encoding.ASCII.GetString(stringBytes.ToArray());
        }

        /// <summary>
        /// Get length of string nul
        /// </summary>
        /// <param name="bytes">bytes of encoded string nul</param>
        /// <returns>length of string bytes, include [00] byte</returns>
        private int NulTerminatedString_stringLength(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0x00)
                {
                    return i+1; // add one to include [00] byte
                }
            }
            return 0;
        }

        private void GenerateSalt1()
        {
            _Salt1 = GetSalt(8);
        }

        private void GenerateSalt2()
        {
            _Salt2 = GetSalt(12);
        }

        private static byte[] GetSalt(int maximumSaltLength)
        {
            var salt = new byte[maximumSaltLength];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }

            return salt;
        }


        /// <summary>
        /// Verify 20-byte-password with real password
        /// </summary>
        /// <param name="inputPassword">20-byte-long input password</param>
        /// <param name="userPassword">user password</param>
        /// <returns></returns>
        private bool VerifyPassword(byte[] inputPassword, byte[] userPassword)
        {
            if (inputPassword.Length != 20)
            {
                return false;
            }
            // password is calculated by
            // SHA1( password ) XOR SHA1( "20-bytes random data from server" <concat> SHA1( SHA1( password ) ) )
            byte[] salt = ConcatArrays(_Salt1, _Salt2);
            SHA1 sha1Hash = SHA1.Create();

            // part 1
            byte[] part1 = sha1Hash.ComputeHash(userPassword);

            // part 2
            byte[] partBytes = ConcatArrays(salt, sha1Hash.ComputeHash(part1));
            byte[] part2 = sha1Hash.ComputeHash(partBytes);

            // XOR
            byte[] result = new byte[20];
            for (var i = 0; i < 20; i++)
            {
                byte b = (byte)(part1[i] ^ part2[i]);
                if (b != inputPassword[i])
                {
                    Console.WriteLine("wrong password");
                    return false;
                }
                result[i] = b;
            }

            Console.WriteLine("correct password");
            //Console.WriteLine("correct {0}, come in {1}", ByteArrayToHexString(result), ByteArrayToHexString(inputPassword));

            return true;
        }

        private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // return true; // Allow untrusted certificates.
            return AcceptInvalidCertificates;
        }

        private void Log(string msg)
        {
            if (Debug) Console.WriteLine(msg);
        }

        #endregion

        #region Helper-Methods

        /// <summary>
        /// Sub array
        /// </summary>
        /// <param name="bytes">big array</param>
        /// <param name="index">start index</param>
        /// <param name="length">length of new array</param>
        /// <returns>subarray</returns>
        public static T[] SubArray<T>(T[] bytes, long index, long length)
        {
            T[] result = new T[length];
            try
            {
                Array.Copy(bytes, index, result, 0, length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error in subarray");
                throw new Exception("error in subarray");
            }
            return result;
        }

        /// <summary>
        /// Combine arrays to one array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T[] ConcatArrays<T>(params T[][] list)
        {
            var result = new T[list.Sum(a => a.Length)];
            int offset = 0;
            for (int x = 0; x < list.Length; x++)
            {
                list[x].CopyTo(result, offset);
                offset += list[x].Length;
            }
            return result;
        }

        /// <summary>
        /// Convert byte array to hex strings for display;
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public byte[] GetBytesFromPEM(string pemString, string section)
        {
            var header = String.Format("-----BEGIN {0}-----", section);
            var footer = String.Format("-----END {0}-----", section);

            var start = pemString.IndexOf(header, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += header.Length;
            var end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;

            if (end < 0)
                return null;

            return Convert.FromBase64String(pemString.Substring(start, end));
        }

        public string GetStringFromPEM(string pemString, string section)
        {
            var header = String.Format("-----BEGIN {0}-----", section);
            var footer = String.Format("-----END {0}-----", section);

            var start = pemString.IndexOf(header, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += header.Length;
            var end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;

            if (end < 0)
                return null;

            return pemString.Substring(start, end);
        }

        public void PrintAllBytes(byte[] data)
        {
            foreach (byte b in data)
            {
                Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
        }

        #endregion

        public class QueryData
        {
            public Header[] headers { set; get; }
            public Row[] rows { set; get; }
        }

        public class Header
        {
            public string name { set; get; }
            public string type { set; get; }
        }

        public class Row
        {
            public int Col1 { get; set; }
            public string Col2 { get; set; }
        }
    }
}
