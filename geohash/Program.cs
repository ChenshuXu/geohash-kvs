using System;
using System.Collections.Generic;
using NGeoHash;

namespace geohash
{
    class Program
    {
        static void Main(string[] args)
        {
            var location = new
            {
                latitude = 41.732733557,
                longitude = -87.551320357,
            };
            int level = 7;
            double radius = 2000;
            

            var watch = System.Diagnostics.Stopwatch.StartNew();
            string[] hashList = DataBase.Bcircle(location.latitude, location.longitude, radius, level);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            foreach (var h in hashList)
            {
                Console.WriteLine(h);
                //BoundingBox box2 = GeoHash.DecodeBbox(h);
                //if (DataBase.BoxInRange(box2, location.latitude, location.longitude, radius))
                //{
                //    Console.WriteLine("yes");
                //}
                //else
                //{
                //    Console.WriteLine("no");
                //}
            }

            var encoded = GeoHash.Encode(location.latitude, location.longitude, level);
            Console.WriteLine("encoded = " + encoded);

            var decoded = GeoHash.Decode(encoded);
            var latitude = decoded.Coordinates.Lat;
            Console.WriteLine("latitude = " + latitude);
            var longitude = decoded.Coordinates.Lon;
            Console.WriteLine("longitude = " + longitude);

            BoundingBox box = GeoHash.DecodeBbox(encoded);
            var maxLat = box.Maximum.Lat;
            var minLat = box.Minimum.Lat;
            var maxLon = box.Maximum.Lon;
            var minLon = box.Minimum.Lon;

            // Measure the box size in meters
            var oneSide = DataBase.Measure(maxLat, minLon, minLat, minLon);
            var anotherSide = DataBase.Measure(maxLat, maxLon, maxLat, minLon);
            Console.WriteLine("box size: " + oneSide + " * " + anotherSide);
            Console.WriteLine("radius " + radius + "m level " + level);
            Console.WriteLine("Time elapsed: " + elapsedMs+"ms | " + hashList.Length + " results");
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
