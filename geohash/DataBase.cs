using System;
using System.Collections.Generic;
using NGeoHash;
using Newtonsoft;
using System.IO;
using System.Linq;

namespace geohash
{
    public class DataBase
    {
        // Key is encoded code
        // Value is an array of coordinates
        private IDictionary<string, List<Coordinates>> dict = new Dictionary<string, List<Coordinates>>();
        private int numberOfCharsStart = 5;
        private int maxCoordinatesInValue = 100;

        public object JsonConvert { get; private set; }
        public object Newtonsoft { get; private set; }


        public void Add(double latitude, double longitude)
        {
            int numberOfChars = numberOfCharsStart;
            // Get the key
            string key = GeoHash.Encode(latitude, longitude, 5);
            // Get the value
            Coordinates coordinate = new Coordinates { Lat = latitude, Lon = longitude };

            // Check how many coordinates under this key
            // If more than maxCoordinatesInValue, encode one more level
            while (dict.ContainsKey(key) && dict[key].Count >= maxCoordinatesInValue)
            {
                numberOfChars++;
                key = GeoHash.Encode(latitude, longitude, numberOfChars);
            }

            // If less than maxCoordinatesInValue or not exist add to value
            if (dict.ContainsKey(key))
            {
                dict[key].Add(coordinate);
                Console.WriteLine(key);
            }
            else
            {
                List<Coordinates> value = new List<Coordinates> { coordinate };
                dict.Add(key, value);
                Console.WriteLine(key);
            }
        }


        public void Display()
        {
            using (StreamWriter sw = new StreamWriter("dict.csv"))
            {
                foreach (var item in dict)
                {
                    //sw.WriteLine(item.Key + "," + item.Value.Count);

                    foreach (var i in item.Value)
                    {
                        string str = item.Key + "," + item.Value.Count + "," + i.Lat.ToString() + "," + i.Lon.ToString();
                        sw.WriteLine(str);
                    }

                }
            }
            
        }
    }
}
