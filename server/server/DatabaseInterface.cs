using System;
using System.IO;
using geohash;

namespace server
{
    public interface DatabaseInterface
    {
        DataBase GetDatabase();
        string GetDBinfo();
    }

    class MyDatabase : DatabaseInterface
    {
        public DataBase m_dataBase;
        public string ok;

        public MyDatabase()
        {
            DataBase database = new DataBase();
            m_dataBase = database;
            // TODO: takes 34 seconds for load dataset1
            var watch0 = System.Diagnostics.Stopwatch.StartNew();
            AddDataset1(database);
            watch0.Stop();
            Console.WriteLine("Process data takes " + watch0.ElapsedMilliseconds + " ms");
            ok = "Process data takes " + watch0.ElapsedMilliseconds + " ms";
            //m_dataBase.Display();
        }

        public string GetDBinfo()
        {
            return ok;
        }

        public DataBase GetDatabase()
        {
            return m_dataBase;
        }

        void AddDataset1(DataBase database)
        {
            string path = "/Users/chenshuxu/Projects/geohash-kvs/server/server/Resources/Crimes_-_2019.csv";
            //string path = "../../../../Resources/Crimes_-_2019.csv";
            string[] lines = System.IO.File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] columns = line.Split(',');
                string LatStr = columns[19];
                string LonStr = columns[20];
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
                    database.Add(lat, lon);
                }
            }
        }

        void AddDatasetSmall(DataBase database)
        {
            string path = "../../../Resources/Crimes_-_2019.csv";
            string[] lines = System.IO.File.ReadAllLines(path);
            //Console.WriteLine(lines[1]);
            int count = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] columns = line.Split(',');
                string LatStr = columns[19];
                string LonStr = columns[20];
                if (LatStr != "" && LonStr != "")
                {
                    double lat;
                    double lon;
                    //Console.WriteLine(LatStr + LonStr);
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
                    database.Add(lat, lon);
                    count++;
                }

                if (count > 5000)
                {
                    return;
                }
            }

        }
    }
}
