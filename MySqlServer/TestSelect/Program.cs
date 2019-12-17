using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlServer;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace TestSelect
{
    class Program
    {
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

            Console.Write("Server starting...");
            server.StartAsync();

            string root_password = "bG43JPmBrY92";

            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";database=dummy;allowLoadLocalInfile=true";

            string connStr_NoSSL = connStr_SSL + ";SslMode=None";

            Console.WriteLine("Press ENTER to start select test without ssl");
            Console.ReadLine();
            TestSelect(connStr_NoSSL);

            Console.WriteLine("Press ENTER to start select test with ssl");
            Console.ReadLine();
            TestSelect(connStr_SSL);
        }

        /// <summary>
        /// Test parameterized queries
        /// </summary>
        /// <param name="connStr"></param>
        static void TestSelect(string connStr)
        {
            Console.WriteLine("TestSelect entering");
            MySqlConnection conn = new MySqlConnection();
            MySqlCommand cmd = new MySqlCommand();
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

            Console.WriteLine("TestSelect finished");
        }
    }
}
