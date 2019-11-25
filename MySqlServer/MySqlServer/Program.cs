using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace MySqlServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(
                "127.0.0.1",
                3306,
                "../../../certs/server-cert.p12",
                "pswd"
                );
            server.StartSync();
        }
    }
}
