using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlServer;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace TestLoadData
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

            Console.Write("Server starting...");
            server.StartAsync();


            string connStr_SSL = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";database=dummy;allowLoadLocalInfile=true";

            string connStr_NoSSL = connStr_SSL + ";SslMode=None";

            //Console.WriteLine("Press ENTER to start select test without ssl");
            //Console.ReadLine();
            //TestSelect(connStr_NoSSL);

            //Console.WriteLine("Press ENTER to start select test with ssl");
            //Console.ReadLine();
            //TestSelect(connStr_SSL);

            //Console.WriteLine("Press ENTER to start load data test without ssl");
            //Console.ReadLine();
            TestLoadData(connStr_NoSSL);

            //Console.WriteLine("Press ENTER to start load data test with ssl");
            //Console.ReadLine();
            //TestLoadData(connStr_SSL);

            Console.WriteLine("Press ENTER to show the content in database");
            Console.ReadLine();

            using (MySqlConnection conn = new MySqlConnection(connStr_NoSSL))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    conn.Open();
                    Console.WriteLine("connected");

                    string sql = "SELECT Col1, Col2 FROM dummy;";
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
        }

        /// <summary>
        /// Test load data statement
        /// </summary>
        /// <param name="connStr"></param>
        static void TestLoadData(string connStr)
        {
            string filePath = "../../../Resources/imptest-5000.txt";
            string[] lines = System.IO.File.ReadAllLines(filePath);

            // connection string for real server
            string ConnectionString = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";database=test;allowLoadLocalInfile=true";

            Console.WriteLine("TestLoadData entering");
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    conn.Open();
                    Console.WriteLine("connected");

                    string sql = "LOAD DATA LOCAL INFILE '" + filePath + "' INTO TABLE dummy FIELDS TERMINATED BY ',' LINES TERMINATED BY '\n';";
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
