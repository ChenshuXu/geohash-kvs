using System;
using MySqlServer;

namespace RunServer
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
            server.Debug = true;

            Console.Write("Server starting...");
            server.StartAsync();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (; ; )
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                }
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }
    }
}
