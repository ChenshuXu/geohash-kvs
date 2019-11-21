using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace MySqlTest
{
    class Program
    {
        const string root_password = "bG43JPmBrY92";
        const string user1_name = "xcs";
        const string user1_password = "K7AA8XQS9wZq";

        static void Main(string[] args)
        {
            ReadDataset();
        }

        static void ReadDataset()
        {
            string connStr = "server=127.0.0.1;port=3306;uid=root;" +
                "pwd=" + root_password + ";SslMode=None";
            string connStr2 = "server=127.0.0.1;uid=xcs;" +
                "pwd=" + user1_password + ";database=world";
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

        static void AddDataset1()
        {
            string path = "/Users/chenshuxu/Projects/geohash-kvs/server/server/Resources/Crimes_-_2019.csv";
            //string path = "C:\\Users\\xinlian01\\Documents\\GitHub\\geohash-kvs\\server\\server\\Resources\\Crimes_-_2019.csv";
            //string path = "../../../../Resources/Crimes_-_2019.csv";

            
            StringBuilder sCommand = new StringBuilder("insert into world.crime(description,LocationDescription,lat,lon) values ");
            List<string> Rows = new List<string>();
            string[] lines = System.IO.File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] columns = line.Split(',');
                string LatStr = columns[19];
                string LonStr = columns[20];
                string LocationDescription = columns[7];
                string id = columns[0];
                string description = columns[6];
                if (LatStr != "" && LonStr != "")
                {
                    double lat;
                    double lon;
                    // Filter out errors
                    try
                    {
                        lat = Convert.ToDouble(LatStr);
                        lon = Convert.ToDouble(LonStr);
                    }
                    catch
                    {
                        continue;
                    }
                    string row = "(\"" + description + "\",";
                    row += "\"" + LocationDescription + "\",";
                    row += lat.ToString() + ",";
                    row += lon.ToString() + ")";
                    Console.WriteLine(row);
                    Rows.Add(row);
                }
            }
            sCommand.Append(string.Join(",", Rows));
            sCommand.Append(";");

            string connStr = "server=127.0.0.1;uid=root;" +
                "pwd=" + root_password + ";database=world;SslMode=None";
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                Console.WriteLine("connected");
                //Console.WriteLine(sCommand.ToString());
                using (MySqlCommand cmd = new MySqlCommand(sCommand.ToString(), conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error");
                Console.WriteLine(ex.ToString());
            }

            conn.Close();
            Console.WriteLine("Done.");
        }
    }
}
