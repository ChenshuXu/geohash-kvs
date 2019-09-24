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
        private IDictionary<string, List<Coordinates>> Dict = new Dictionary<string, List<Coordinates>>();
        private List<string> FullList = new List<string>();
        private int numberOfCharsStart = 1;
        private int maxCoordinatesInValue = 1000;
        private int maxNumberOfChar = 9;

        public void Add(double latitude, double longitude, int numberOfChars = 1)
        {
            // Get the key
            string key = GeoHash.Encode(latitude, longitude, numberOfChars);
            // Get the value
            Coordinates coordinate = new Coordinates { Lat = latitude, Lon = longitude };

            if (!Dict.ContainsKey(key))
            {
                List<Coordinates> value = new List<Coordinates> { coordinate };
                Dict.Add(key, value);
                Console.WriteLine("Add new key " + key);
                return;
            }

            if (Dict.ContainsKey(key) && Dict[key].Count < maxCoordinatesInValue)
            {
                Dict[key].Add(coordinate);
                Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                return;
            }

            if (numberOfChars >= maxNumberOfChar)
            {
                Dict[key].Add(coordinate);
                Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                return;
            }

            // Check how many coordinates under this key
            // If more than maxCoordinatesInValue, encode one more level
            // Also calculate all upper level again, move to a deeper level
            if (Dict.ContainsKey(key) && Dict[key].Count >= maxCoordinatesInValue)
            {
                if (!FullList.Contains(key))
                {
                    // Mark current level as full
                    FullList.Add(key);
                    //Console.WriteLine("Add " + key + " to full list");

                    // Move current level to the deeper level
                    foreach (var coor in Dict[key])
                    {
                        Add(coor.Lat, coor.Lon, numberOfChars + 1);
                    }
                }

                // Calculate one more level
                Add(latitude, longitude, numberOfChars + 1);
            }
        }

        public void Display()
        {
            using (StreamWriter sw = new StreamWriter("dict.csv"))
            {
                foreach (var item in Dict)
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
