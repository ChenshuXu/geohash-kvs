using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlServer;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace TestServer
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
            server.Debug = false;

            Task.Run(() => server.StartAsync());

            Thread.Sleep(1000);

            string connStr_NoSSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";SslMode=None";

            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password;

            ClientTask(connStr_SSL);
        }

        /// <summary>
        /// Test parameterized queries
        /// </summary>
        /// <param name="connStr"></param>
        private static void ClientTask(string connStr)
        {
            Console.WriteLine("ClientTask entering");
            MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection();
            MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand();
            conn.ConnectionString = connStr;

            try
            {
                conn.Open();
                cmd.Connection = conn;

                cmd.CommandText = "SELECT * FROM dummy WHERE Col2 = @value;";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@value", 0);

                MySqlDataReader rdr;
                for (int i = 1; i <= 10; i++)
                {
                    cmd.Parameters["@value"].Value = i;

                    //cmd.ExecuteNonQuery();

                    rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        Console.WriteLine(rdr[0] + " -- " + rdr[1]);
                    }
                    rdr.Close();
                }

                conn.Close();
                Console.WriteLine("Done");
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error " + ex.Number + " has occurred: " + ex.Message);
            }

            Console.WriteLine("ClientTask finished");
        }
    }
}
