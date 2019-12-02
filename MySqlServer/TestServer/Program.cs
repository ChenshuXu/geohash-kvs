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
            server.Debug = true;

            server.StartAsync();

            Thread.Sleep(1000);

            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";database=dummy;allowLoadLocalInfile=true";

            string connStr_NoSSL = connStr_SSL + ";SslMode=None";


            //TestSelect(connStr_SSL);
            TestLoadData(connStr_NoSSL);
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

        /// <summary>
        /// Test load data statement
        /// </summary>
        /// <param name="connStr"></param>
        static void TestLoadData(string connStr)
        {
            string filePath = "../../../Resources/imptest.txt";
            //string filePath = "../../../Resources/Crimes-2019.csv";
            string[] lines = System.IO.File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                //Console.WriteLine(line);
            }

            // connection string for real server
            string ConnectionString = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";SslMode=None;database=test;allowLoadLocalInfile=true";

            Console.WriteLine("TestLoadData entering");
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    conn.Open();
                    Console.WriteLine("connected");

                    string sql = "LOAD DATA LOCAL INFILE '" + filePath + "' INTO TABLE imptest FIELDS TERMINATED BY 'aaa' ENCLOSED BY 'bbb' ESCAPED BY 'ccc' LINES TERMINATED BY 'ddd' STARTING BY 'eee';";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        Console.WriteLine(rdr[0] + " -- " + rdr[1]);
                    }
                    rdr.Close();
                    conn.Close();
                    Console.WriteLine("Done");
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error " + ex.Number + " has occurred: " + ex.Message);
                }
            }

            Console.WriteLine("TestLoadData finished");
        }
    }
}
