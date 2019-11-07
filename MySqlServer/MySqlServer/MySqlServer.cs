using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MySqlServer
{
    public class MySqlServer
    {
        private int mSequence = 0;

        public MySqlServer()
        {

        }

        public void ExecuteServer()
        {
            // Establish the local endpoint  
            // for the socket. Dns.GetHostName 
            // returns the name of the host  
            // running the application.
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 3306);

            // Creation TCP/IP Socket using  
            // Socket Class Costructor 
            Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {

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
                byte[] buffer = new Byte[200];
                clientSocket.Receive(buffer);
                HandleLogin(buffer);

                // Send ok packet
                Console.WriteLine("start ok");
                SendOkPacket(clientSocket);

                mSequence = 0;
                // Command phase
                // Handle query
                Console.WriteLine("Command phase");
                
                while(true)
                {
                    buffer = new Byte[200];
                    int bytesLength = clientSocket.Receive(buffer);
                    if (bytesLength != 0)
                    {
                        Console.WriteLine("get command, length: {0}", bytesLength);
                        HandleCommand(buffer, clientSocket);
                        mSequence = 0;
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void HandleCommand(byte[] data, Socket clientSocket)
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

            // Get request command query
            byte[] requestCommandQuery = new byte[packetLengthInt];
            Array.Copy(data, 4, requestCommandQuery, 0, packetLengthInt);
            byte command = requestCommandQuery[0];
            if(command == 0x03) //text protocol
            {
                Console.WriteLine("get text protocol");
                GetSequence();
                byte[] statement = new byte[packetLengthInt - 1];
                Array.Copy(requestCommandQuery, 1, statement, 0, packetLengthInt - 1);
                HandleQuery(statement, clientSocket);
            }
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
            string query = Encoding.UTF8.GetString(queryBytes, 0, queryBytes.Length);
            Console.WriteLine("handle query: {0}", query);
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
            SendLengthEncodedPacket(data.headers.Length, clientSocket);

            // send data header packet
            foreach(var header in data.headers)
            {
                SendHeaderPacket(header, clientSocket);
            }

            // send eof packet
            SendEofPacket(clientSocket);

            // send data row packet
            foreach(var row in data.rows)
            {
                SendRowPacket(row, clientSocket);
            }

            // send eof packet
            SendEofPacket(clientSocket);
        }

        private void SendRowPacket(Row r, Socket clientSocket)
        {
            List<byte> packet = new List<byte>();
            packet.AddRange(EncodeString(r.Col1.ToString()));
            packet.AddRange(EncodeString(r.Col2.ToString()));
            clientSocket.Send(GetSendPacket(packet.ToArray()));
        }

        private void SendEofPacket(Socket clientSocket)
        {
            List<byte> packet = new List<byte>();
            packet.Add(0xfe); // EOF Header
            packet.Add(0x00); // warnings
            packet.Add(0x00);
            packet.Add(0x02); // status flags
            packet.Add(0x00);
            clientSocket.Send(GetSendPacket(packet.ToArray()));
        }

        private void SendHeaderPacket(Header h, Socket clientSocket)
        {
            List<byte> packet = new List<byte>();
            int character_set = 33; //utf8_general_ci
            int max_col_length = 1024; //This is totally made up.  it shouldn't be
            byte column_type = 0xfd; //Fallback to varchar?
            if(h.type == "int")
            {
                column_type = 0x09;
            }
            else if(h.type == "str")
            {
                column_type = 0xfd;
            }

            packet.AddRange(EncodeString("def"));
            packet.AddRange(EncodeString(h.name));
            packet.AddRange(EncodeString("virtual_table"));
            packet.AddRange(EncodeString("physical_table"));
            packet.AddRange(EncodeString(h.name));
            packet.AddRange(EncodeString(h.name));
            packet.Add(0x0c);
            packet.AddRange(ToByteArray(character_set, 2));
            packet.AddRange(ToByteArray(max_col_length, 4));
            packet.AddRange(ToByteArray(column_type, 1));


            packet.Add(0x00); //Flags?
            packet.Add(0x00);

            packet.Add(0x00); //Only doing ints/static strings now

            packet.Add(0x00); //Filler
            packet.Add(0x00);
            clientSocket.Send(GetSendPacket(packet.ToArray()));
        }

        private void SendLengthEncodedPacket(int lengh, Socket clientSocket)
        {
            clientSocket.Send(GetSendPacket(new byte[] { EncodeLength(lengh) }));
        }

        private byte[] EncodeString(string str)
        {
            List<byte> enc = new List<byte>();
            enc.Add(EncodeLength(str.Length));
            byte[] stringByte = Encoding.UTF8.GetBytes(str);
            enc.AddRange(stringByte);
            return enc.ToArray();
        }

        private byte EncodeLength(int value)
        {
            if(value>250)
            {
                // raise exception
            }

            return Convert.ToByte(value);
        }

        private void HandleLogin(byte[] data)
        {
            GetSequence();

            byte[] packetLength = new byte[3];
            byte sequence = data[3];
            Array.Copy(data, packetLength, 3);
            int i = packetLength[0];
            Console.WriteLine("packetLength: {0}", i);
            Console.WriteLine("sequence: {0}", (int)sequence);

            // get login request package
            byte[] loginRequest = new byte[i];
            Array.Copy(data, 4, loginRequest, 0, i);
            // print login request
            foreach (byte b in loginRequest)
            {
                //Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
        }

        private void HandleHandshake(Socket clientSocket)
        {
            List<byte> handshake = new List<byte>();
            byte[] server_version = Encoding.ASCII.GetBytes("8.0.18");
            byte[] connection_id = BitConverter.GetBytes(993);

            handshake.Add(0x0a);
            handshake.AddRange(server_version);
            handshake.Add(0x00);
            handshake.AddRange(connection_id);
            handshake.Add(0x00);

            for (var i = 0; i < 8; i++)
            {
                handshake.Add(0x00);
            }
            for (var i = 0; i < 2; i++)
            {
                handshake.Add(0x00);
            }

            byte[] packet = GetSendPacket(handshake.ToArray());
            clientSocket.Send(packet);
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

        private byte[] GetSendPacket(byte[] byte_value)
        {
            byte[] length = ToByteArray(byte_value.Length, 3);
            byte[] seq = ToByteArray(GetSequence(), 1);
            byte[] value = new List<byte>()
                .Concat(length)
                .Concat(seq)
                .Concat(byte_value)
                .ToArray();
            return value;
        }

        private byte[] ToByteArray(int theInt, int length)
        {
            byte[] resultArray = new byte[length];
            for (int i = length - 1; i >= 0; i--)
            {
                resultArray[i] = (byte)((theInt >> (i * 8)) & 0xff);
            }
            return resultArray;
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
    }
}
