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
            MySqlServer server = new MySqlServer();
            server.ExecuteServer();
        }
    }
}
