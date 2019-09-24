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
                latitude = 32.768799,
                longitude = -97.309341,
            };
            var encoded = GeoHash.Encode(location.latitude, location.longitude, 5);
            Console.WriteLine("encoded = " + encoded);

            var decoded = GeoHash.Decode("9v");
            var latitude = decoded.Coordinates.Lat;
            Console.WriteLine("latitude = " + latitude);
            var longitude = decoded.Coordinates.Lon;
            Console.WriteLine("longitude = " + longitude);

            BoundingBox box = GeoHash.DecodeBbox(encoded);
            var maxLat = box.Maximum.Lat;
            var minLat = box.Minimum.Lat;
            var maxLon = box.Maximum.Lon;
            var minLon = box.Minimum.Lon;

            Console.WriteLine("latitude range: " + minLat + ", " + maxLat);
            Console.WriteLine("longitude range: " + minLon + ", " + maxLon);

            // Measure the box size in meters
            var oneSide = Measure(maxLat, minLon, minLat, minLon);
            var anotherSide = Measure(maxLat, maxLon, maxLat, minLon);
            Console.WriteLine("box size: " + oneSide + " * " + anotherSide);

            CrimeDataTest();
        }

        public static double Measure(double lat1, double lon1, double lat2, double lon2)
        {  // generally used geo measurement function
            var R = 6378.137; // Radius of earth in KM
            var dLat = lat2 * Math.PI / 180 - lat1 * Math.PI / 180;
            var dLon = lon2 * Math.PI / 180 - lon1 * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d * 1000.0; // meters
        }

        public static void CrimeDataTest()
        {
            string path = "/Users/chenshuxu/Projects/geohash-kvs/geohash/Crimes_-_2019.csv";
            DataBase database = new DataBase();

            string[] lines = System.IO.File.ReadAllLines(path);
            //Console.WriteLine(lines[1]);
            for (int i=1; i<lines.Length; i++)
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
            

            path = "/Users/chenshuxu/Projects/geohash-kvs/geohash/crime.csv";
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
            database.Display();
        }

    }
}
