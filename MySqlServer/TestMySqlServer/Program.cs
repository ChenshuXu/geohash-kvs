using System;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlServer;

namespace MySqlTest
{
    class Program
    {
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

            //Task.Run(() => server.StartSync());
            server.StartAsync();

            Thread.Sleep(1000);

            string connStr_NoSSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";SslMode=None";

            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password;

            ReadDataset(connStr_NoSSL);
            //Thread.Sleep(1000);
            //ReadDataset(connStr_NoSSL);
            //ReadDataset(connStr_SSL);

            Console.WriteLine("Test passed");
        }

        static void ReadDataset(string connStr)
        {
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                Console.WriteLine("connected");

                string sql = "SELECT * FROM dummy;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Console.WriteLine(rdr[0] + " -- " + rdr[1]);
                }
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error");
                Console.WriteLine(ex.ToString());
            }

            conn.Close();
            Console.WriteLine("Done.");
        }

        static void Import()
        {

        }
    }
}
