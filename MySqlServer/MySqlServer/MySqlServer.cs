using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;

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

        enum Phase { ConnectionPhase, CommandPhase };
        private Phase mServerPhase = Phase.ConnectionPhase;

        private int mSequence = 0;
        private byte[] mSalt1; // 8 bytes
        private byte[] mSalt2; // 12 bytes
        private string mPassword = "bG43JPmBrY92";

        public void ExecuteServer()
        {
            // Establish the local endpoint  
            // for the socket. Dns.GetHostName 
            // returns the name of the host  
            // running the application.
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 3306);

            try
            {
                // Creation TCP/IP Socket using  
                // Socket Class Costructor 
                Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // Using Bind() method we associate a 
                // network address to the Server Socket 
                // All client that will connect to this  
                // Server Socket must know this network 
                // Address 
                listener.Bind(localEndPoint);
                // Using Listen() method we create  
                // the Client list that will want 
                // to connect to Server 
                listener.Listen(10);

                while (true)
                {
                    // Suspend while waiting for
                    // incoming connection Using
                    // Accept() method the server
                    // will accept connection of client
                    Console.WriteLine("Waiting connection ... ");
                    Socket clientSocket = listener.Accept();

                    // Handshake
                    Console.WriteLine("start handshake");
                    HandleHandshake(clientSocket);

                    // Handshake response, handle login info
                    // Data buffer 
                    byte[] buffer = new Byte[1024];
                    clientSocket.Receive(buffer);
                    if (HandleLogin(buffer))
                    {
                        // Send ok packet
                        SendOkPacket(clientSocket);
                        mServerPhase = Phase.CommandPhase;
                    }
                    else
                    {
                        // fail login
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                        mServerPhase = Phase.ConnectionPhase;
                    }
                    

                    mSequence = 0;
                    // Command phase
                    // Handle query
                    Console.WriteLine("Command phase");

                    while (mServerPhase == Phase.CommandPhase)
                    {
                        buffer = new Byte[1024];
                        int bytesLength = clientSocket.Receive(buffer);
                        if (bytesLength != 0)
                        {
                            //Console.WriteLine("get command, length: {0}", bytesLength);
                            bool quit = HandleCommand(buffer, clientSocket);
                            mSequence = 0;
                            if (quit)
                            {
                                clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                                mServerPhase = Phase.ConnectionPhase;
                            }
                        }
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private bool HandleCommand(byte[] data, Socket clientSocket)
        {
            // print all bytes recieved
            //foreach (byte b in data)
            //{
            //    Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            //}

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
                HandleQuery(statement, clientSocket);
                return false;
            }
            if (textProtocol == 0x01) // COM_QUIT
            {
                return true;
            }

            return false;
        }

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

        private void HandleQuery(byte[] queryBytes, Socket clientSocket)
        {
            //foreach (byte b in queryBytes)
            //{
            //    Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            //}
            string query = Encoding.ASCII.GetString(queryBytes, 0, queryBytes.Length).ToLower();
            Console.WriteLine("handle query: {0}", query);
            
            if (query.Substring(0, 6) == "select")
            {
                Console.WriteLine("handle select");
                HandleSelect(query, clientSocket);
            }
            else
            {
                HandleSelect(query, clientSocket);
            }
        }

        private void HandleSelect(string query, Socket clientSocket)
        {
            Console.WriteLine("handle {0}", query);
            List<byte> sendPacket = new List<byte>();
            string[] queryArray = query.Split(' ');
            string columnName1 = queryArray[1]; // TODO can have multiple column names

            if (columnName1 == "@@version_comment")
            {
                Console.WriteLine("at version_comment");
                sendPacket.AddRange(GetSendPacket(LengthEncodedInteger(1)));
                sendPacket.AddRange(GetSendPacket(GetVersionCommentHeaderPacket()));
                sendPacket.AddRange(GetSendPacket(GetVersionCommentRowPacket()));
                sendPacket.AddRange(GetSendPacket(GetEofPacket()));

                clientSocket.Send(sendPacket.ToArray());
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
            clientSocket.Send(GetSendPacket(LengthEncodedInteger(data.headers.Length)));

            // send data header packet
            foreach (var header in data.headers)
            {
                clientSocket.Send(GetSendPacket(GetHeaderPacket(header)));
            }

            // send eof packet
            //clientSocket.Send(GetSendPacket(GetEofPacket()));

            // send data row packet
            foreach (var row in data.rows)
            {
                clientSocket.Send(GetSendPacket(GetRowPacket(row)));
            }

            // send eof packet
            clientSocket.Send(GetSendPacket(GetEofPacket()));
        }

        private byte[] GetRowPacket(Row r)
        {
            Console.WriteLine("Send row {0}, {1}", r.Col1, r.Col2);
            List<byte> packet = new List<byte>();
            packet.AddRange(LengthEncodedString(r.Col1.ToString()));
            packet.AddRange(LengthEncodedString(r.Col2.ToString()));
            return packet.ToArray();
        }

        private byte[] GetEofPacket2()
        {
            List<byte> packet = new List<byte>();
            packet.Add(0xfe); // EOF Header
            packet.Add(0x00); // warnings
            packet.Add(0x00);
            packet.Add(0x02); // status flags
            packet.Add(0x00);
            packet.Add(0x00); // payload
            packet.Add(0x00);
            return packet.ToArray();
        }

        private byte[] GetEofPacket()
        {
            List<byte> packet = new List<byte>();
            packet.Add(0xfe); // EOF Header
            packet.Add(0x00); // warnings
            packet.Add(0x00);
            packet.Add(0x22); // status flags
            packet.Add(0x00);
            packet.Add(0x00); // payload
            packet.Add(0x00);
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
            return LengthEncodedString("MySQL Community Server (GPL)");
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true if password is correct, false if password is wrong</returns>
        private bool HandleLogin(byte[] data)
        {
            Console.WriteLine("handle login");
            GetSequence();

            byte[] packetLength = new byte[3];
            byte sequence = data[3];
            Array.Copy(data, packetLength, 3);
            int packetLengthInt = packetLength[0];
            Console.WriteLine("whole packetLength: {0}", data.Length);
            Console.WriteLine("packetLength: {0}", packetLengthInt);
            Console.WriteLine("sequence: {0}", (int)sequence);


            // get login request package
            byte[] loginRequestBytes = SubArray(data, 4, packetLengthInt);
            long currentHead = 0;
            // print login request
            //foreach (byte b in loginRequest)
            //{
            //    Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            //}

            // capability flags
            byte[] clientCapabilitiesBytes = SubArray(loginRequestBytes, currentHead, 4); // client capabilities
            currentHead += 4;
            uint clientCapabilities = BitConverter.ToUInt32(clientCapabilitiesBytes, 0);

            // max-packet size
            byte[] maxPacketSize = SubArray(loginRequestBytes, currentHead, 4);
            currentHead += 4;

            // character set
            byte[] characterSet = SubArray(loginRequestBytes, currentHead, 1);
            currentHead += 1;

            // reserved unused
            currentHead += 23;

            // username, string nul type
            byte[] usernameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
            string usernameStr = NulTerminatedString_bytesToString(usernameBytes);
            currentHead += NulTerminatedString_stringLength(usernameBytes);
            Console.WriteLine("username: {0}", usernameStr);

            // Get user password from database
            byte[] userPassword = Encoding.ASCII.GetBytes(mPassword);
            bool passVerification = false;
            // auth-response
            if (Convert.ToBoolean(clientCapabilities & CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA))
            {
                // lenenc-int password length
                byte[] passwordLengthBytes = SubArray(loginRequestBytes, currentHead, 8);
                long passwordLength = LengthEncodedInteger_bytesToInt(passwordLengthBytes);
                currentHead += LengthEncodedInteger_intLength(passwordLengthBytes);

                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Console.WriteLine("password length: {0}, {1}", passwordLength, ByteArrayToHexString(passwordBytes));

                passVerification = VerifyPassword(passwordBytes, userPassword);
            }
            else if(Convert.ToBoolean(clientCapabilities & CLIENT_SECURE_CONNECTION))
            {
                // 1 byte password length
                int passwordLength = Convert.ToInt32(loginRequestBytes[currentHead]);
                currentHead += 1;

                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Console.WriteLine("password length: {0}, {1}", passwordLength, ByteArrayToHexString(passwordBytes));

                passVerification = VerifyPassword(passwordBytes, userPassword);
            }
            else
            {
                // string nul type password
                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                int passwordLength = NulTerminatedString_stringLength(passwordBytes);
                byte[] password = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Console.WriteLine("password length: {0}, {1}", passwordLength, ByteArrayToHexString(password));

                passVerification = VerifyPassword(passwordBytes, userPassword);
            }
            
            // if database
            if (Convert.ToBoolean(clientCapabilities & CLIENT_CONNECT_WITH_DB))
            {
                // string nul type
                byte[] databaseNameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                string databaseName = NulTerminatedString_bytesToString(databaseNameBytes);
                currentHead += NulTerminatedString_stringLength(databaseNameBytes);
                Console.WriteLine("database string bytes length {0} name: {1}", NulTerminatedString_stringLength(databaseNameBytes), databaseName);
            }

            // if has auth plugin name
            if (Convert.ToBoolean(clientCapabilities & CLIENT_PLUGIN_AUTH))
            {
                // string nul type
                byte[] authNameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                string authPluginName = NulTerminatedString_bytesToString(authNameBytes);
                currentHead += NulTerminatedString_stringLength(authNameBytes);
                Console.WriteLine("auth plugin string bytes length {0} name: {1}", NulTerminatedString_stringLength(authNameBytes), authPluginName);
            }

            // if have client connect attributs
            if (Convert.ToBoolean(clientCapabilities & CLIENT_CONNECT_ATTRS))
            {
                // lenenc int type, length of all key-values
            }

            return passVerification;

        }

        /// <summary>
        /// Verify 20-byte-password with real password
        /// </summary>
        /// <param name="inputPassword">20-byte-long input password</param>
        /// <param name="userPassword">user password</param>
        /// <returns></returns>
        private bool VerifyPassword(byte[] inputPassword, byte[] userPassword)
        {
            // password is calculated by
            // SHA1( password ) XOR SHA1( "20-bytes random data from server" <concat> SHA1( SHA1( password ) ) )
            byte[] salt = ConcatArrays(mSalt1, mSalt2);
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

        private void HandleHandshake(Socket clientSocket)
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

            // salt
            // first 8 bytes of the auth-plugin data
            // last byte 0x00 as filler
            GenerateSalt1();
            packet.AddRange(mSalt1);
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
            for(var i = 0; i<10; i++)
            {
                packet.Add(0x00);
            }

            // Salt: y5uww>'&rc
            GenerateSalt2();
            packet.AddRange(mSalt2);
            packet.Add(0x00);

            // Authentication Plugin: mysql_native_password
            packet.AddRange(NulTerminatedString("mysql_native_password"));

            clientSocket.Send(GetSendPacket(packet.ToArray()));
            Console.WriteLine("send handshake");
        }

        private void SendOkPacket(Socket clientSocket)
        {
            List<byte> ok = new List<byte>();
            ok.Add(0x00); // OK
            ok.Add(0x00); // affected rows
            ok.Add(0x00); // last insert id
            ok.Add(0x02); // Say autocommit was set
            ok.Add(0x00);
            ok.Add(0x00); // No warnings
            ok.Add(0x00);

            byte[] packet = GetSendPacket(ok.ToArray());
            clientSocket.Send(packet);
            Console.WriteLine("send ok packet");
        }

        private byte[] GetSendPacket(byte[] data)
        {
            List<byte> packet = new List<byte>();
            byte[] length = FixedLengthInteger(data.Length, 3);
            byte[] seq = FixedLengthInteger(GetSequence(), 1);
            packet.AddRange(length);
            packet.AddRange(seq);
            packet.AddRange(data);
            return packet.ToArray();
        }

        private int GetSequence()
        {
            int val = mSequence;
            mSequence += 1;
            if (mSequence > 255)
            {
                mSequence = 0;
            }
            return val;
        }

        ///
        /// Integer types
        ///

        // type int<>
        private byte[] FixedLengthInteger(int theInt, int length)
        {
            byte[] resultArray = new byte[length];
            for (int i = length - 1; i >= 0; i--)
            {
                resultArray[i] = (byte)((theInt >> (i * 8)) & 0xff);
            }
            return resultArray;
        }

        // type int<lenenc>
        private byte[] LengthEncodedInteger(long value)
        {
            if (value < 251)
            {
                // stored as a 1-byte integer
                return new byte[] { (byte)value };
            }
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

            return new byte[] { (byte)value };
        }

        private long LengthEncodedInteger_bytesToInt(byte[] bytes)
        {
            // 8-byte integer
            if (bytes[0] == 0xfe)
            {
                Console.WriteLine("8-byte int");
            }
            // 3-byte integer
            if (bytes[0] == 0xfd)
            {
                Console.WriteLine("3-byte int");
            }
            // 2-byte integer
            if (bytes[0] == 0xfc)
            {
                Console.WriteLine("2-byte int");
            }

            // 1-byte integer
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

        ///
        /// String types
        ///

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
            Array.Copy(Encoding.ASCII.GetBytes(str), bytes, Math.Min(20, str.Length));
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
            return new byte[] { };
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
            mSalt1 = GetSalt(8);
        }

        private void GenerateSalt2()
        {
            mSalt2 = GetSalt(12);
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
        /// Sub array
        /// </summary>
        /// <param name="bytes">big array</param>
        /// <param name="index">start index</param>
        /// <param name="length">length of new array</param>
        /// <returns>subarray</returns>
        public static T[] SubArray<T>(T[] bytes, long index, long length)
        {
            T[] result = new T[length];
            Array.Copy(bytes, index, result, 0, length);
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
    }
}
