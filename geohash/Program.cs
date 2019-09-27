using System;
using System.Collections.Generic;
using System.IO;
using NGeoHash;

namespace geohash
{
    class Program
    {
        static void Main(string[] args)
        {

            Coordinates c = new Coordinates { Lat = 86, Lon = -30.75179211 };
            int level = 3;
            double radius = 150000; // in meters

            Console.WriteLine("point latitude " + c.Lat + ", longitude " + c.Lon);
            var encoded = GeoHash.Encode(c.Lat, c.Lon, level);
            Console.WriteLine("encoded = " + encoded);

            var decoded = GeoHash.Decode(encoded);
            var latitude = decoded.Coordinates.Lat;
            var longitude = decoded.Coordinates.Lon;
            Console.WriteLine("decoded box latitude " + latitude + ", longitude " + longitude);

            BoundingBox box = GeoHash.DecodeBbox(encoded);
            var maxLat = box.Maximum.Lat;
            var minLat = box.Minimum.Lat;
            var maxLon = box.Maximum.Lon;
            var minLon = box.Minimum.Lon;

            // Measure the box size in meters
            var oneSide = DataBase.Measure(maxLat, minLon, minLat, minLon);
            var anotherSide = DataBase.Measure(maxLat, maxLon, maxLat, minLon);

            // Bounding circle
            var watch = System.Diagnostics.Stopwatch.StartNew();
            string[] hashList = DataBase.Bcircle(c.Lat, c.Lon, radius, level);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            foreach (var h in hashList)
            {
                // Console.WriteLine(h);
            }

            Console.WriteLine("box size: " + oneSide + " meters * " + anotherSide + " meters");
            Console.WriteLine("bounding circle radius " + radius + " meters, level " + level);
            Console.WriteLine("Time elapsed: " + elapsedMs+" ms | " + hashList.Length + " results get");
            string filename = "bounding circle" + c.Lat.ToString() + "-" + c.Lon.ToString() + "-" + radius.ToString() + "-" + level.ToString();
            DataBase.GenerateKMLBoundingCircle(hashList, c.Lat, c.Lon, radius, filename);
            Console.WriteLine("save as file name " + filename);

            // Bounding box
            Coordinates coor1 = new Coordinates { Lat = 83.97933143, Lon = -39.85722014 };
            Coordinates coor2 = new Coordinates { Lat= 84.44481747, Lon = -20.60826777 };
            string[] hashlist2 = GeoHash.Bboxes(coor1.Lat, coor1.Lon, coor2.Lat, coor2.Lon, level);
            DataBase.GenerateKMLBoundingBoxes(hashlist2, coor1, coor2, "bounding box");
        }

        

        public static void CrimeDataTest()
        {
            DataBase database = new DataBase();
            //AddDatasetSmall(database);
            //AddDataset1(database);
            //AddDataset2(database);
            //database.Display();
        }

        static void AddDatasetSmall(DataBase database)
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

                if(count > 5000)
                {
                    return;
                }
            }
            
        }

        static void AddDataset1(DataBase database)
        {
            string path = "../../../Resources/Crimes_-_2019.csv";
            string[] lines = System.IO.File.ReadAllLines(path);
            //Console.WriteLine(lines[1]);
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
                }
            }
        }

        static void AddDataset2(DataBase database)
        {
            string path = "../../../Resources/crime.csv";
            string[] lines2 = System.IO.File.ReadAllLines(path);
            //Console.WriteLine(lines2[1]);

            for (int i = 1; i < lines2.Length; i++)
            {
                string line = lines2[i];
                string[] columns = line.Split(',');
                string LatStr = columns[14];
                string LonStr = columns[15];
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
                }
            }
        }
    }
}
