using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSQL;
using TSQL.Tokens;

namespace MySqlServer
{
    public class ClientSession : IDisposable
    {
        #region Client Capability Flags
        // More capability flages in https://dev.mysql.com/doc/internals/en/capability-flags.html
        static uint CLIENT_LONG_PASSWORD = 0x00000001;
        static uint CLIENT_FOUND_ROWS = 0x00000002;
        static uint CLIENT_LONG_FLAG = 0x00000004;
        static uint CLIENT_CONNECT_WITH_DB = 0x00000008;
        static uint CLIENT_NO_SCHEMA = 0x00000010;
        static uint CLIENT_COMPRESS = 0x00000020;

        static uint CLIENT_LOCAL_FILES = 0x00000080;
        static uint CLIENT_IGNORE_SPACE = 0x00000100;
        static uint CLIENT_PROTOCOL_41 = 0x00000200;
        static uint CLIENT_INTERACTIVE = 0x00000400;
        static uint CLIENT_SSL = 0x00000800;

        static uint CLIENT_TRANSACTIONS = 0x00002000;
        static uint CLIENT_SECURE_CONNECTION = 0x00008000;
        static uint CLIENT_MULTI_STATEMENTS = 0x00010000;
        static uint CLIENT_MULTI_RESULTS = 0x00020000;

        static uint CLIENT_PLUGIN_AUTH = 0x00080000;
        static uint CLIENT_CONNECT_ATTRS = 0x00100000;
        static uint CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA = 0x00200000;
        
        static uint CLIENT_SESSION_TRACK = 0x00800000;
        static uint CLIENT_DEPRECATE_EOF = 0x01000000;
        
        #endregion

        #region Server Status Flags
        // More server flags in https://dev.mysql.com/doc/internals/en/status-flags.html
        static uint SERVER_STATUS_IN_TRANS = 0x0001; //	a transaction is active
        static uint SERVER_STATUS_AUTOCOMMIT = 0x0002; // auto-commit is enabled
        static uint SERVER_MORE_RESULTS_EXISTS = 0x0008;
        static uint SERVER_STATUS_NO_GOOD_INDEX_USED = 0x0010;
        static uint SERVER_STATUS_NO_INDEX_USED = 0x0020;
        static uint SERVER_STATUS_CURSOR_EXISTS = 0x0040; // Used by Binary Protocol Resultset to signal that COM_STMT_FETCH must be used to fetch the row-data.
        static uint SERVER_STATUS_LAST_ROW_SENT = 0x0080;
        static uint SERVER_STATUS_DB_DROPPED = 0x0100;
        static uint SERVER_STATUS_NO_BACKSLASH_ESCAPES = 0x0200;
        static uint SERVER_STATUS_METADATA_CHANGED = 0x0400;
        static uint SERVER_QUERY_WAS_SLOW = 0x0800;
        static uint SERVER_PS_OUT_PARAMS = 0x1000;
        static uint SERVER_STATUS_IN_TRANS_READONLY = 0x2000; // in a read-only transaction
        static uint SERVER_SESSION_STATE_CHANGED = 0x4000; // connection state information has changed
        #endregion

        #region Column type
        // More types in https://dev.mysql.com/doc/internals/en/com-query-response.html#column-type
        public enum ColumnType
        {
            MYSQL_TYPE_LONG = 0x03,
            MYSQL_TYPE_FLOAT = 0x04,
            MYSQL_TYPE_DOUBLE = 0x05,
            MYSQL_TYPE_NULL = 0x06,
            MYSQL_TYPE_TIMESTAMP = 0x07,
            MYSQL_TYPE_LONGLONG = 0x08,
            MYSQL_TYPE_INT24 = 0x09,

            MYSQL_TYPE_TIME = 0x0b,

            MYSQL_TYPE_VARCHAR = 0x0f,
            MYSQL_TYPE_VAR_STRING = 0xfd,
            MYSQL_TYPE_STRING = 0xfe
        }
        #endregion

        #region Public-Members

        public TcpClient TcpClient
        {
            get { return _TcpClient; }
        }

        public NetworkStream NetworkStream
        {
            get { return _NetworkStream; }
        }

        public SslStream SslStream
        {
            get { return _SslStream; }
            set { _SslStream = value; }
        }

        public string IpPort
        {
            get { return _IpPort; }
        }

        public string ConnectedDatabase
        {
            get { return _ConnectedDB; }
            set { UseDatabase(value); }
        }

        public CancellationTokenSource TokenSource { get; set; }

        public CancellationToken Token { get; set; }

        #endregion

        #region Private-Members

        private Server _Server;

        // ssl related members
        private X509Certificate2 _SslCertificate = null;
        private X509Certificate2Collection _SslCertificateCollection = null;

        // tcp connection related members
        public int _ReceiveBufferSize = 4096;
        private TcpClient _TcpClient = null;
        private NetworkStream _NetworkStream = null;
        private SslStream _SslStream = null;
        private string _IpPort = null;
        public bool _UseSsl = false;

        // mysql server related members
        private uint _ClientCapabilities;
        private uint _ServerStatus = 0x0200;
        private byte[] ServerStatus {
            get { return BitConverter.GetBytes(_ServerStatus); }
        }
        private string _ConnectedDB = "dummy";
        private DatabaseController _DatabaseController;
        public enum Phase { Waiting, ConnectionPhase, CommandPhase, WaitingDataPhase };
        public Phase _ServerPhase = Phase.Waiting;
        private int _Sequence = 0;
        private byte[] _Salt1; // 8 bytes
        private byte[] _Salt2; // 12 bytes

        private List<byte[]> _FileBuffer = new List<byte[]>();

        public bool Debug = false;

        #endregion

        #region Constructors-and-Factories

        public ClientSession(TcpClient tcp, Server server, DatabaseController db)
        {
            if (tcp == null) throw new ArgumentNullException(nameof(tcp));

            _Server = server;

            _TcpClient = tcp;
            _NetworkStream = tcp.GetStream();
            _IpPort = tcp.Client.RemoteEndPoint.ToString();

            _DatabaseController = db;
            _ServerPhase = Phase.Waiting;
        }

        public ClientSession()
        {
        }
        #endregion

        #region Public-Methods

        public void ClientConnected()
        {
            try
            {
                LogBasic("start handshake");
                SetState(Phase.ConnectionPhase);
                HandleHandshake();

                byte[] buffer = new byte[_ReceiveBufferSize];
                _NetworkStream.Read(buffer);
                HandleHandshakeResponse(buffer);

                if (_UseSsl)
                {
                    Log("use ssl");
                    _SslCertificate = new X509Certificate2(_Server.CertFilename, _Server.CertPassword);

                    _SslCertificateCollection = new X509Certificate2Collection { _SslCertificate };

                    if (_Server.AcceptInvalidCertificates)
                    {
                        _SslStream = new SslStream(_NetworkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate));
                    }
                    else
                    {
                        _SslStream = new SslStream(_NetworkStream, false);
                    }

                    bool success = StartTls();

                    if (!success)
                    {
                        Dispose();
                    }
                    else
                    {
                        Log("start tls success");
                    }
                }
            }
            catch (Exception ex)
            {
                SendErrPacket(ex.ToString());
            }
        }

        public void DataReceived(byte[] data)
        {
            try
            {
                switch (_ServerPhase)
                {
                    case Phase.ConnectionPhase:
                        Log("handshake after start tls");
                        HandleHandshakeResponse(data);
                        break;
                    case Phase.CommandPhase:
                        _Sequence = 0;
                        HandleCommand(data);
                        break;
                    case Phase.WaitingDataPhase:
                        Log("DataReceived: " + data.Length);
                        _FileBuffer.Add(data); // TODO: may have improvements on buffering
                        break;
                }
            }
            catch (Exception ex)
            {
                SendErrPacket(ex.ToString());
            }
        }

        public void ClientDisconnected()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_SslStream != null)
            {
                _SslStream.Close();
                _SslStream.Dispose();
                _SslStream = null;
            }

            if (_NetworkStream != null)
            {
                _NetworkStream.Close();
                _NetworkStream.Dispose();
                _NetworkStream = null;
            }

            if (_TcpClient != null)
            {
                _TcpClient.Close();
                _TcpClient.Dispose();
                _TcpClient = null;
            }
        }

        #endregion

        #region Private-Methods

        private bool StartTls()
        {
            try
            {
                _SslStream.AuthenticateAsServer(
                    _SslCertificate,
                    _Server.MutuallyAuthenticate,
                    SslProtocols.Tls12,
                    !_Server.AcceptInvalidCertificates);
                if (!_SslStream.IsEncrypted)
                {
                    Log("[" + IpPort + "] not encrypted");
                    Dispose();
                    return false;
                }

                if (!_SslStream.IsAuthenticated)
                {
                    Log("[" + IpPort + "] stream not authenticated");
                    Dispose();
                    return false;
                }

                if (_Server.MutuallyAuthenticate && !_SslStream.IsMutuallyAuthenticated)
                {
                    Log("[" + IpPort + "] failed mutual authentication");
                    Dispose();
                    return false;
                }
            }
            catch (Exception e)
            {
                Log("[" + IpPort + "] TLS exception" + Environment.NewLine + e.ToString());
                Dispose();
                return false;
            }
            Log("ssl stream ok");
            return true;
        }

        private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // return true; // Allow untrusted certificates.
            return _Server.AcceptInvalidCertificates;
        }

        private void DisconnectClient()
        {
            LogBasic("disconnect");
            Dispose();
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
                // exit waiting data phase
                case Phase.WaitingDataPhase:
                    //HandleDataLoadResponse();
                    break;
            }

            _ServerPhase = phase;

            // enter state
            switch (_ServerPhase)
            {
                case Phase.WaitingDataPhase:
                    //Task.Run(() => RunDataLoad());
                    break;
            }
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
            // 4 bytes connection id TODO: send random number
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
            Log("send initial handshake");
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
            Log("packetLength: " + packetLengthInt);

            byte sequence = data[3];
            //Log("sequence: {0}", (int)sequence);

            // get login request package
            byte[] loginRequestBytes = SubArray(data, 4, packetLengthInt);
            long currentHead = 0;

            // capability flags
            byte[] clientCapabilitiesBytes = SubArray(loginRequestBytes, currentHead, 4); // client capabilities
            currentHead += 4;
            uint clientCapabilities = BitConverter.ToUInt32(clientCapabilitiesBytes, 0);
            _ClientCapabilities = clientCapabilities;

            // max-packet size, max size of a command packet that the client wants to send to the server
            byte[] maxPacketSize = SubArray(loginRequestBytes, currentHead, 3);
            _ReceiveBufferSize = FixedLengthInteger_toInt(maxPacketSize);
            Log("maxPacketSize: " + FixedLengthInteger_toInt(maxPacketSize));
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
                return;
            }


            // username, string nul type
            byte[] usernameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
            string usernameStr = NulTerminatedString_bytesToString(usernameBytes);
            currentHead += NulTerminatedString_stringLength(usernameBytes);
            Log("username: " + usernameStr);

            byte[] correctPassword = Encoding.ASCII.GetBytes(_DatabaseController.GetUserPassword(usernameStr));
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
                Log("password length: {"+ passwordLength + "}, {"+ ByteArrayToHexString(passwordBytes) + "}");

                passedVerification = VerifyPassword(passwordBytes, correctPassword);
            }
            else if (Convert.ToBoolean(clientCapabilities & CLIENT_SECURE_CONNECTION))
            {
                // 1 byte password length
                int passwordLength = Convert.ToInt32(loginRequestBytes[currentHead]);
                currentHead += 1;

                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Log("password length: {" + passwordLength + "}, {" + ByteArrayToHexString(passwordBytes) + "}");

                passedVerification = VerifyPassword(passwordBytes, correctPassword);
            }
            else
            {
                // string nul type password
                byte[] passwordBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                int passwordLength = NulTerminatedString_stringLength(passwordBytes);
                byte[] password = SubArray(loginRequestBytes, currentHead, passwordLength);
                currentHead += passwordLength;
                Log("password length: {" + passwordLength + "}, {" + ByteArrayToHexString(passwordBytes) + "}");

                passedVerification = VerifyPassword(passwordBytes, correctPassword);
            }

            // If have database name input
            if (Convert.ToBoolean(clientCapabilities & CLIENT_CONNECT_WITH_DB))
            {
                // string nul type
                byte[] databaseNameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                string databaseName = NulTerminatedString_bytesToString(databaseNameBytes);
                currentHead += NulTerminatedString_stringLength(databaseNameBytes);
                Log("database name: {"+ databaseName + "}");
                UseDatabase(databaseName);
            }

            // If has auth plugin name
            if (Convert.ToBoolean(clientCapabilities & CLIENT_PLUGIN_AUTH))
            {
                // string nul type
                byte[] authNameBytes = SubArray(loginRequestBytes, currentHead, packetLengthInt - currentHead);
                string authPluginName = NulTerminatedString_bytesToString(authNameBytes);
                currentHead += NulTerminatedString_stringLength(authNameBytes);
                Log("auth plugin name: {"+ authPluginName + "}");
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
            byte[] packetLengthBytes = SubArray(data, 0, 3);
            int packetLengthInt = FixedLengthInteger_toInt(packetLengthBytes);
            Log("sql packet length: "+ packetLengthInt);

            // Get sequence
            int sequence = FixedLengthInteger_toInt(SubArray(data, 3, 1));

            // Get text protocol
            byte[] queryPacket = SubArray(data, 4, packetLengthInt);
            byte textProtocol = queryPacket[0];

            switch (textProtocol)
            {
                // COM_QUIT
                case 0x01:
                    Log("COM_QUIT disconnect");
                    SetState(Phase.Waiting);
                    DisconnectClient();
                    return;

                // COM_INIT_DB
                case 0x02:
                    Log("COM_INIT_DB use database");
                    // string[EOF]
                    byte[] dbBytes = SubArray(queryPacket, 1, packetLengthInt - 1);
                    string dbString = RestOfPacketString_bytesToString(dbBytes);
                    Log(dbString);
                    UseDatabase(dbString);
                    SendOkPacket();
                    return;

                // COM_QUERY
                case 0x03:
                    Log("get COM_QUERY");
                    // string[EOF]
                    GetSequence();
                    byte[] queryBytes = SubArray(queryPacket, 1, packetLengthInt - 1);
                    string queryString = RestOfPacketString_bytesToString(queryBytes);
                    HandleQuery(queryString);
                    return;

                // COM_PING
                case 0x0e:
                    Log("COM_PING ping");
                    SendOkPacket();
                    return;
            }

            Log("other command: " + textProtocol);
            throw new Exception("other command not implemented: " + textProtocol);
        }

        private void HandleQuery(string query)
        {
            LogBasic("handle query: {" + query + "}");
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);

            Table returnTable = _DatabaseController.GetDatabase("dummy").GetTable("dummy");

            switch (tokens[0].Text.ToLower())
            {
                case "select":
                    returnTable = Select(tokens);
                    break;
                case "show":
                    returnTable = Show(tokens);
                    break;
                case "set":
                    _DatabaseController.Set(tokens);
                    SendOkPacket();
                    return;
                case "load":
                    LoadData(tokens);
                    return;
            }

            Column[] h = returnTable.Columns;
            Row[] r = returnTable.Rows;

            /*
            Log("Query results:");
            Log("Cols:");
            foreach (var col in h)
            {
                Log("\tname: {" + col.ColumnName + "}, type: {" + col._ColumnType + "}");
            }
            Log("Rows:");
            foreach (var row in r)
            {
                foreach (var v in row._Values)
                {
                    Log("\tvalue: " + v);
                }
            }
            */
            // send length encoded packet
            SendPacket(LengthEncodedInteger(h.Length));

            // send column definition packet
            SendColumnDefinition(h);

            // send eof packet
            // If the CLIENT_DEPRECATE_EOF client capability flag is set, OK_Packet is sent; else EOF_Packet is sent.
            // ?? which seems not correct. more info in https://dev.mysql.com/doc/internals/en/capability-flags.html
            if (Convert.ToBoolean(_ClientCapabilities & CLIENT_DEPRECATE_EOF))
            {
                //SendOkPacket();
            }
            else
            {
                SendEofPacket();
            }

            // send row packet
            SendTextResultsetRow(r);

            // send eof packet
            SendEofPacket();
            return;
        }

        /// <summary>
        /// Handle query with TIMEDIFF funtion
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>a table object that contains diff information</returns>
        private Table HandleTimediff(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table("");
            virtualTable.AddColumn(
                new Column("TIMEDIFF(NOW(), UTC_TIMESTAMP())", ClientSession.ColumnType.MYSQL_TYPE_TIME)
            );
            virtualTable.AddRow(
                new Row(
                    new Object[] { "08:00:00" }
                )
            );

            return virtualTable;
        }

        /// <summary>
        /// Handle select query
        /// </summary>
        /// <param name="tokens">query tokens</param>
        /// <returns>a table object that contains result column information and row information</returns>
        private Table Select(List<TSQLToken> tokens)
        {
            // select clause
            // get output columns
            PopAndCheck(ref tokens, "select");
            Log("SELECT:");

            // handle select TIMEDIFF
            if (tokens[0].Text == "TIMEDIFF")
            {
                Log("TIMEDIFF:");
                return HandleTimediff(tokens);
            }

            bool readingVariable = false;
            List<Column> outPutColumns = new List<Column>();

            while (true)
            {
                Column qualifiedColumnName = GetQualifiedColumnName(ref tokens);
                outPutColumns.Add(qualifiedColumnName);
                Log("\ttable name: {" + qualifiedColumnName.TableName + "}, column name: {" + qualifiedColumnName.ColumnName + "}");
                // Handle veriable tokens
                if (qualifiedColumnName._TokenType == TSQLTokenType.Variable)
                {
                    readingVariable = true;
                }

                if (tokens.Count == 0)
                {
                    break;
                }

                TSQLToken nextToken = tokens[0];
                if (nextToken.Text.ToLower() == "from" || nextToken.Text.ToLower() == "limit")
                {
                    break;
                }

                if (!readingVariable && nextToken.Text != ",")
                {
                    throw new Exception("should be ,");
                }
                // check ,
                if (nextToken.Text == ",")
                {
                    tokens.RemoveAt(0);
                    continue;
                }
            }

            // from clause
            // get from table name
            Table fromTable = _DatabaseController.InformationSchema.GetTable("information schema"); // default is info schema
            // when reading system variable, don't have from table clause
            if (!readingVariable)
            {
                PopAndCheck(ref tokens, "from");
                Log("FROM:");
                Table tableNameObj = GetQualifiedTableName(ref tokens);
                // TODO: from database
                fromTable = _DatabaseController.GetDatabase(tableNameObj.DatabaseName).GetTable(tableNameObj.TableName); // TODO: throw error when table name not exist
                Log("\tdb name: {" + fromTable.DatabaseName + "}, table name: {" + fromTable.TableName + "}");
                // TODO: throw error when have join keyword,
                // only support from one table,
                // only support table from dummy database

            }

            // TODO: remaining clause
            Log("remaining tokens:");
            foreach (var token in tokens)
            {
                Log("\ttype: " + token.Type.ToString() + ", value: " + token.Text);
            }

            Log("QUERY after processed");
            foreach (var col in outPutColumns)
            {
                Log("\ttable name: {" + col.TableName + "}, column name: {" + col.ColumnName + "}");
            }
            Log("\tfrom table info, db name: {" + fromTable.DatabaseName + "}, table name: {" + fromTable.TableName + "}");


            // TODO: add column information to virtual table
            return fromTable.SelectRows(outPutColumns.ToArray());
        }

        /// <summary>
        /// Handle show query
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>table object that contains query result, ready to send to client</returns>
        private Table Show(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table("");
            PopAndCheck(ref tokens, "show");

            switch (tokens[0].Text.ToLower())
            {
                case "collation":
                    return _DatabaseController.InformationSchema.GetTable("COLLATIONS");
                case "databases":
                    // TODO: show databases
                    break;
                case "tables":
                    // TODO: show tables
                    break;
            }

            throw new Exception("show query not support");
        }

        /// <summary>
        /// Handle load data query
        /// </summary>
        /// <param name="tokens">query tokens</param>
        private void LoadData(List<TSQLToken> tokens)
        {
            LoadDataParser parser = new LoadDataParser(tokens);

            // LOCAL INFILE request packet, send file name
            List<byte> fileNamePacket = new List<byte>();
            fileNamePacket.Add(0xfb); // [fb] LOCAL INFILE
            fileNamePacket.AddRange(RestOfPacketString(parser.file_name));
            SendPacket(fileNamePacket.ToArray());

            // Start receive file
            SetState(Phase.WaitingDataPhase);

            Task t = Task.Run(() => RunDataLoad());
            t.Wait();

            byte[] file = HandleDataLoadResponse();

            // Store process
            string table_name = parser.table_name;

            string file_string = RestOfPacketString_bytesToString(file);
            //Log(file_string);

            // separate lines
            string[] lines = ParseLines(file_string, parser.lines_starting_by, parser.lines_terminated_by);
            //Log(lines);
            // separate columns

            SendOkPacket();
        }

        /// <summary>
        /// Separate lines
        /// </summary>
        /// <param name="file_string"></param>
        /// <param name="lines_starting_by"></param>
        /// <param name="lines_terminated_by"></param>
        /// <returns></returns>
        public static string[] ParseLines(string file_string, string lines_starting_by, string lines_terminated_by)
        {
            List<string> lines = new List<string>();
            int lines_starting_by_length = lines_starting_by.Length;
            int lines_terminated_by_length = lines_terminated_by.Length;
            int file_string_length = file_string.Length;
            string line;
            int current = 0; // current index
            int head1 = 0; // index of line begin
            int head2 = 0; // index of line end
            while (current < file_string_length)
            {
                if (lines_starting_by != "" && current + lines_starting_by_length <= file_string_length)
                {
                    string possible_lines_starting_by = file_string.Substring(current, lines_starting_by_length);
                    if (possible_lines_starting_by == lines_starting_by)
                    {
                        head1 = current + lines_starting_by_length;
                    }
                }

                if (current + lines_terminated_by_length <= file_string_length)
                {
                    string possible_lines_terminated_by = file_string.Substring(current, lines_terminated_by_length);
                    if (possible_lines_terminated_by == lines_terminated_by)
                    {
                        head2 = current;
                        line = file_string.Substring(head1, head2 - head1);
                        lines.Add(line);
                        head1 = current + lines_terminated_by_length;
                    }
                }
                current += 1;
            }
            return lines.ToArray();
        }

        /// <summary>
        /// Separate fields
        /// </summary>
        /// <param name="line_string"></param>
        /// <param name="fields_terminated_by"></param>
        /// <param name="fields_enclosed_by"></param>
        /// <param name="fields_escaped_by"></param>
        /// <returns></returns>
        public static string[] ParseFields(string line_string, string fields_terminated_by, string fields_enclosed_by, string fields_escaped_by)
        {
            List<string> fields = new List<string>();
            // TODO: not finish
            return fields.ToArray();
        }

        private async Task RunDataLoad()
        {
            int lastSeq = -1;
            while(true)
            {
                await Task.Delay(50);
                byte[] lastPacket = _FileBuffer.Last();
                // get last possible packet
                byte[] possibleLastPacket = lastPacket.Skip(Math.Max(0, lastPacket.Count() - 4)).ToArray();
                byte[] packetLengthBytes = SubArray(possibleLastPacket, 0, 3);
                int packetLength = FixedLengthInteger_toInt(packetLengthBytes);
                int sequence = FixedLengthInteger_toInt(SubArray(possibleLastPacket, 3, 1));
                //Log("sequence: " + sequence);
                //Log("length: " + packetLength);

                // TODO: ssl not receive all packets, why???
                if (packetLength == 0) // TODO: || sequence == lastSeq
                {
                    Log("last packet sequence: " + sequence);
                    SetState(Phase.CommandPhase);
                    break;
                }

                lastSeq = sequence;
                
            }
        }

        /// <summary>
        /// Handle file data packet in load data statement
        /// </summary>
        /// <returns>file bytes</returns>
        private byte[] HandleDataLoadResponse()
        {
            Log("all file data received, packets: " + _FileBuffer.Count());

            List<byte> allFileBytes = new List<byte>();
            List<byte> allFileBufferBytes = new List<byte>();

            foreach(var v in _FileBuffer)
            {
                allFileBufferBytes.AddRange(v);
            }
            //WriteToFile(allFileBufferBytes.ToArray(), "received-bytes");

            int lastSequence = 1;
            int head = 0;
            while (true)
            {
                byte[] packetLengthBytes = allFileBufferBytes.GetRange(head, 3).ToArray();
                head += 3;
                int packetLength = FixedLengthInteger_toInt(packetLengthBytes);
                Log("packet length: " + packetLength);
                int sequence = FixedLengthInteger_toInt(allFileBufferBytes.GetRange(head, 1).ToArray());
                head += 1;
                if (sequence != lastSequence + 1)
                {
                    Log("sequence not match error at " + sequence);
                    //throw new Exception("sequence not match error at " + sequence);
                }
                lastSequence = sequence;
                Log("sequence: " + sequence);

                if (packetLength == 0)
                {
                    Log("arrive last packet");
                    break;
                }

                // get file bytes in this packet
                byte[] fileBytes = allFileBufferBytes.GetRange(head, packetLength).ToArray();
                head += packetLength;
                allFileBytes.AddRange(fileBytes);
                
                if (head >= allFileBufferBytes.Count())
                {
                    Log("error arrive the end");
                    break;
                }
            }

            //WriteToFile(allFileBytes.ToArray(), "imptest-result.txt");

            return allFileBytes.ToArray();
        }

        #region Generic Response Packets

        private void SendOkPacket(int effectedRow = 0, int lastInsertId = 0, int numberOfWarnings = 0, string msg = null)
        {
            List<byte> packet = new List<byte>();
            packet.Add(0x00); // OK packet

            packet.AddRange(LengthEncodedInteger(effectedRow)); // affected rows
            packet.AddRange(LengthEncodedInteger(lastInsertId)); // last insert id

            if (Convert.ToBoolean(_ClientCapabilities & CLIENT_PROTOCOL_41))
            {
                // server status, int<2>
                packet.AddRange(ServerStatus);
                // warnings
                packet.AddRange(FixedLengthInteger(numberOfWarnings, 2));
            }
            else if (Convert.ToBoolean(_ClientCapabilities & CLIENT_TRANSACTIONS))
            {
                // server status, int<2>
                packet.AddRange(ServerStatus);
            }

            // message
            if (msg != null)
            {
                if (Convert.ToBoolean(_ClientCapabilities & CLIENT_SESSION_TRACK))
                {
                    packet.AddRange(LengthEncodedString(msg));
                    if (Convert.ToBoolean(_ServerStatus & SERVER_SESSION_STATE_CHANGED))
                    {
                        // TODO: what is SERVER_SESSION_STATE_CHANGED???
                        packet.AddRange(LengthEncodedString("???"));
                    }
                }
                else
                {
                    packet.AddRange(RestOfPacketString(msg));
                }
            }

            SendPacket(packet.ToArray());
            Log("send ok packet");
        }

        private void SendErrPacket(String message)
        {
            List<byte> bytes = new List<byte>();

            bytes.Add(0xff); //[ff] header of the ERR packet

            byte[] errorCode = { 0x15, 0x04 };
            bytes.AddRange(errorCode);

            if (Convert.ToBoolean(_ClientCapabilities & CLIENT_PROTOCOL_41))
            {
                // sql_state_marker, string[1]
                bytes.Add(0x23);

                // sql_state, string[5]
                bytes.AddRange(FixedLengthString("28000", 5));
            }

            bytes.AddRange(RestOfPacketString(message));

            SendPacket(bytes.ToArray());
            Log("send err packet:" + message);
        }

        private void SendEofPacket()
        {
            List<byte> packet = new List<byte>();

            // [fe] EOF header
            packet.Add(0xfe);

            if (Convert.ToBoolean(_ClientCapabilities & CLIENT_PROTOCOL_41))
            {
                // number of warnings
                packet.Add(0x00); // warnings
                packet.Add(0x00);

                // Status Flags
                packet.Add(0x22);
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
                _NetworkStream.Write(packetArray, 0, packetArray.Length);
                _NetworkStream.Flush();
            }
            else
            {
                _SslStream.Write(packetArray, 0, packetArray.Length);
                _SslStream.Flush();
            }
        }

        #endregion

        #endregion

        #region Integer types

        /// <summary>
        /// Type int<>
        /// Fixed-Length Integer
        /// </summary>
        /// <param name="theInt"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] FixedLengthInteger(int theInt, int length)
        {
            byte[] resultArray = new byte[length];
            for (int i = length - 1; i >= 0; i--)
            {
                resultArray[i] = (byte)(theInt >> (i * 8));
            }
            return resultArray;
        }

        public static int FixedLengthInteger_toInt(byte[] bytes)
        {
            int sum = 0;
            for (var i = bytes.Length - 1; i >= 0; i--)
            {
                sum += (int)(bytes[i] << (8 * i));
            }
            return sum;
        }

        /// <summary>
        /// Type int<lenenc>
        /// Length-Encoded Integer
        /// An integer that consumes 1, 3, 4, or 9 bytes, depending on its numeric value
        /// https://dev.mysql.com/doc/internals/en/integer.html#packet-Protocol::LengthEncodedInteger
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] LengthEncodedInteger(long value)
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

        public static long LengthEncodedInteger_toInt(byte[] bytes)
        {
            // 8-byte integer
            if (bytes[0] == 0xfe)
            {
                //Console.WriteLine("8-byte int");
                return FixedLengthInteger_toInt(SubArray(bytes, 1, 8));
            }
            // 3-byte integer
            if (bytes[0] == 0xfd)
            {
                //Console.WriteLine("3-byte int");
                return FixedLengthInteger_toInt(SubArray(bytes, 1, 3));
            }
            // 2-byte integer
            if (bytes[0] == 0xfc)
            {
                //Console.WriteLine("2-byte int");
                return FixedLengthInteger_toInt(SubArray(bytes, 1, 2));
            }

            // 1-byte integer
            //Console.WriteLine("1-byte int");
            return bytes[0];
        }

        public static int LengthEncodedInteger_intLength(byte[] bytes)
        {
            // 8-byte integer
            switch (bytes[0])
            {
                case 0xfe:
                    //Console.WriteLine("8-byte int");
                    return 8;
                case 0xfd:
                    //Console.WriteLine("3-byte int");
                    return 3;
                case 0xfc:
                    //Console.WriteLine("2-byte int");
                    return 2;
            }

            // 1-byte integer
            //Console.WriteLine("1-byte int");
            return 1;
        }

        #endregion

        #region string types

        /// <summary>
        /// Type string<lenenc>
        /// Length encoded string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] LengthEncodedString(string str)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(LengthEncodedInteger(str.Length));
            byte[] stringByte = Encoding.ASCII.GetBytes(str);
            bytes.AddRange(stringByte);
            return bytes.ToArray();
        }

        /// <summary>
        /// Type string<fix>
        /// Fixed length string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] FixedLengthString(string str, int length)
        {
            byte[] bytes = new byte[length];

            try
            {
                Array.Copy(Encoding.ASCII.GetBytes(str), bytes, str.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("string length longer than fix length {0}", length);
            }
            return bytes;
        }

        /// <summary>
        /// Type string<var>
        /// The length of the string is determined by another field or is calculated at runtimes
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] VariableLengthString(string str, int length)
        {
            byte[] bytes = new byte[length];
            Array.Copy(Encoding.ASCII.GetBytes(str), bytes, Math.Min(20, str.Length));
            return bytes;
        }

        /// <summary>
        /// Type string<EOF>
        /// If a string is the last component of a packet,
        /// its length can be calculated from the overall packet length minus the current position.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] RestOfPacketString(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string RestOfPacketString_bytesToString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Type string<NUL>
        /// Strings that are terminated by a [00] byte.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] NulTerminatedString(string str)
        {
            List<byte> bytes = new List<byte>();
            byte[] stringByte = Encoding.ASCII.GetBytes(str);
            bytes.AddRange(stringByte);
            bytes.Add(0x00);

            return bytes.ToArray();
        }


        /// <summary>
        /// Convert string NUL bytes to string
        /// </summary>
        /// <param name="bytes">bytes of encoded string nul</param>
        /// <returns>decoded string</returns>
        public static string NulTerminatedString_bytesToString(byte[] bytes)
        {
            List<byte> stringBytes = new List<byte>();
            for (var i = 0; i < bytes.Length; i++)
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
        /// Get length of string NUL
        /// </summary>
        /// <param name="bytes">bytes of encoded string nul</param>
        /// <returns>length of string bytes, include [00] byte</returns>
        public static int NulTerminatedString_stringLength(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0x00)
                {
                    return i + 1; // add one to include [00] byte
                }
            }
            return 0;
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
        public static T[] SubArray<T>(T[] bytes, long index, long length = 0)
        {
            if (length == 0)
            {
                length = bytes.Length - index -1;
            }
            T[] result = new T[length];
            try
            {
                Array.Copy(bytes, index, result, 0, length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error in subarray");
                throw new Exception("error in subarray " + ex);
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

        public static byte[] GetBytesFromPEM(string pemString, string section)
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

        public static string GetStringFromPEM(string pemString, string section)
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

        public static void PrintAllBytes(byte[] data)
        {
            foreach (byte b in data)
            {
                Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
        }


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

        private void GenerateSalt1()
        {
            _Salt1 = GetSalt(8);
        }

        private void GenerateSalt2()
        {
            _Salt2 = GetSalt(12);
        }

        private byte[] GetSalt(int maximumSaltLength)
        {
            var salt = new byte[maximumSaltLength];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }

            return salt;
        }

        private void UseDatabase(string dbname)
        {
            // TODO: check dabase name exists
            _ConnectedDB = dbname;
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
                    Log("wrong password");
                    return false;
                }
                result[i] = b;
            }

            Log("correct password");
            //Log("correct {0}, come in {1}", ByteArrayToHexString(result), ByteArrayToHexString(inputPassword));

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        private void SendColumnDefinition(Column[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                Column column = columns[i];
                List<byte> packet = new List<byte>();

                int character_set = 33; // utf8_general_ci
                int max_col_length = 1024; //This is totally made up.  it shouldn't be
                byte column_type = (byte)column._ColumnType;

                packet.AddRange(LengthEncodedString("def")); // catalog
                packet.AddRange(LengthEncodedString(column.ColumnName)); // schema-name
                packet.AddRange(LengthEncodedString(column.TableName)); // virtual
                packet.AddRange(LengthEncodedString(column.TableName)); // physical table-name
                packet.AddRange(LengthEncodedString(column.ColumnName)); // virtual column name
                packet.AddRange(LengthEncodedString(column.ColumnName)); // physical column name
                packet.Add(0x0c); // length of the following fields (always 0x0c)
                packet.AddRange(FixedLengthInteger(character_set, 2)); // character_set is the column character set and is defined in Protocol::CharacterSet.
                packet.AddRange(FixedLengthInteger(max_col_length, 4)); // maximum length of the field
                packet.AddRange(FixedLengthInteger(column_type, 1)); // column_type, type of the column as defined in Column Type

                // flags
                packet.Add(0x00);
                packet.Add(0x00);

                // decimals, max shown decimal digits
                packet.Add(0x00); // 0x00 for integers and static strings
                                  // 0x1f for dynamic strings, double, float
                                  // 0x00 to 0x51 for decimals

                packet.Add(0x00); //Filler
                packet.Add(0x00);

                SendPacket(packet.ToArray());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        private void SendTextResultsetRow(Row[] rows)
        {
            for (var i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                List<byte> packet = new List<byte>();

                for (var j = 0; j < row._Values.Length; j++)
                {
                    packet.AddRange(LengthEncodedString(row._Values[j].ToString()));
                }
                SendPacket(packet.ToArray());
            }
        }


        /// <summary>
        /// Read tokens, get the table name, handle . character
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>table object that contains table name and database name</returns>
        private Table GetQualifiedTableName(ref List<TSQLToken> tokens)
        {
            TSQLToken possibleColName = tokens[0];
            tokens.RemoveAt(0);

            if (tokens.Count != 0 && tokens[0].Text == ".")
            {
                tokens.RemoveAt(0);
                TSQLToken actualColName = tokens[0];
                tokens.RemoveAt(0);
                string databaseName = possibleColName.Text;
                return new Table(actualColName.Text, databaseName);
            }
            return new Table(possibleColName.Text, _ConnectedDB);
        }

        /// <summary>
        /// Read tokens, get the first column name, handle . character
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>column object that contains table name and column name</returns>
        private Column GetQualifiedColumnName(ref List<TSQLToken> tokens)
        {
            TSQLToken possibleColName = tokens[0];
            tokens.RemoveAt(0);

            if (tokens.Count != 0 && tokens[0].Text == ".")
            {
                tokens.RemoveAt(0);
                TSQLToken actualColName = tokens[0];
                tokens.RemoveAt(0);
                string tableName = possibleColName.Text;
                return new Column
                {
                    ColumnName = actualColName.Text,
                    TableName = tableName,
                    _TokenType = possibleColName.Type
                };
            }
            return new Column
            {
                ColumnName = possibleColName.Text,
                _TokenType = possibleColName.Type
            };
        }

        /// <summary>
        /// Remove first item and check if it is same as keyword
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="keyword"></param>
        private void PopAndCheck(ref List<TSQLToken> tokens, string keyword)
        {
            TSQLToken first = tokens[0];
            tokens.RemoveAt(0);
            if (first.Text.ToLower() != keyword)
            {
                throw new Exception(string.Format("{0}!={1}", first.Text, keyword));
            }
        }

        /// <summary>
        /// Get the first token in token list
        /// </summary>
        /// <param name="tokens">token list</param>
        /// <returns>first token, null if nothing</returns>
        private string GetFirst(List<TSQLToken> tokens)
        {
            if (tokens.Count == 0)
            {
                return "";
            }
            return tokens[0].Text;
        }

        /// <summary>
        /// Write bytes to file
        /// </summary>
        /// <param name="data"></param>
        /// <param name="name"></param>
        private void WriteToFile(byte[] data, string name)
        {
            string fileName = "../../../" + name;

            try
            {
                // Check if file already exists. If yes, delete it.     
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                // Create a new file     
                using (FileStream fs = File.Create(fileName))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        public void LogBasic(string msg)
        {
            string timeStr = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();
            Console.WriteLine("[client session][" + timeStr + "]" + "[" + _IpPort + "] " + msg);
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg)
        {
            if (Debug)
            {
                LogBasic(msg);
            }
        }

        public void Log(object[] objs)
        {
            if (Debug)
            {
                foreach (var obj in objs)
                {
                    LogBasic(Convert.ToString(obj));
                }
            }
        }

        #endregion
    }
}