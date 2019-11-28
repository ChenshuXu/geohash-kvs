using System;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlServer;

namespace TestMultiClient
{
    class Program
    {
        private static int clientThreads = 10;
        const string root_password = "bG43JPmBrY92";

        static void Main(string[] args)
        {
            string certPath = "../../../certs/server-cert.p12";
            Server server = new Server(
                "127.0.0.1",
                3306,
                certPath,
                "pswd"
                );
            server.Debug = true;

            server.StartAsync();

            Console.WriteLine("Start in 3 seconds");

            Thread.Sleep(3000);

            string connStr_NoSSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";SslMode=None;Connection Timeout=3000";

            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password;

            Log("Starting client tasks");
            for (int i = 0; i < clientThreads; i++)
            {
                Log("Starting client " + i);
                Task.Run(() => ClientTask(connStr_NoSSL));
            }

            Console.ReadLine();
        }

        private static void ClientTask(string connStr)
        {
            string timeStr = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();
            Log("entering", timeStr);
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                try
                {
                    Log("Connecting to MySQL...", timeStr);
                    conn.Open();
                    Log("connected", timeStr);
                    string sql = "SELECT * FROM dummy;";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        Log(rdr[0] + " -- " + rdr[1], timeStr);
                    }
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    Log(ex.ToString(), timeStr);
                }
                conn.Close();

            }

            Log("task finished", timeStr);
        }

        private static void Log(string msg, string number)
        {
            string timeStr = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();

            Console.WriteLine("\t"+ "[" + timeStr + "]" + "[ClientTask " + number + "] " + msg);
        }

        public static void Log(string msg)
        {
            string timeStr = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();
            Console.WriteLine("[" + timeStr + "]" + msg);
            
        }
    }
}
