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
            
            Task.Run(() => server.StartAsync());

            Thread.Sleep(3000);

            string connStr_NoSSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";SslMode=None";

            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password;

            Log("Starting clients");
            for (int i = 0; i < clientThreads; i++)
            {
                Log("Starting client " + i);
                Task.Run(() => ClientTask(connStr_NoSSL));
                Thread.Sleep(100);
            }

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static void ClientTask(string connStr)
        {
            string taskNum = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();
            Log("entering", taskNum);
            MySqlConnection conn = new MySqlConnection(connStr);
            
            try
            {
                Log("Connecting to MySQL...", taskNum);
                conn.Open();
                Log("connected", taskNum);
                string sql = "SELECT * FROM dummy;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Log(rdr[0] + " -- " + rdr[1], taskNum);
                }
                rdr.Close();
            }
            catch (Exception ex)
            {
                Log("error", taskNum);
                Log(ex.ToString(), taskNum);
            }

            conn.Close();
            Log("Done.", taskNum);
            

            Log("finished", taskNum);
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
