using System;
using System.Collections.Generic;
using NGeoHash;
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

        public void Add2( double latitude, double longitude, int numberOfChars = 1 )
        {
            // Get the key
            string key = GeoHash.Encode(latitude, longitude, numberOfChars);
            // Get the value
            Coordinates coordinate = new Coordinates { Lat = latitude, Lon = longitude };

            if (!Dict.ContainsKey(key))
            {
                List<Coordinates> value = new List<Coordinates> { coordinate };
                Dict.Add(key, value);
                //Console.WriteLine("Add new key " + key);
                return;
            }

            if (Dict.ContainsKey(key) && Dict[key].Count < maxCoordinatesInValue)
            {
                Dict[key].Add(coordinate);
                //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                return;
            }

            if (numberOfChars >= maxNumberOfChar)
            {
                Dict[key].Add(coordinate);
                //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
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
                        Add2(coor.Lat, coor.Lon, numberOfChars + 1);
                    }
                }

                // Calculate one more level
                Add2(latitude, longitude, numberOfChars + 1);
            }
        }

        public void Add(double latitude, double longitude)
        {
            for (int numberOfChars = 1; numberOfChars <= maxNumberOfChar; numberOfChars++)
            {
                // Get the key
                string key = GeoHash.Encode(latitude, longitude, numberOfChars);
                // Get the value
                Coordinates coordinate = new Coordinates { Lat = latitude, Lon = longitude };

                // First entry
                if (!Dict.ContainsKey(key))
                {
                    List<Coordinates> value = new List<Coordinates> { coordinate };
                    Dict.Add(key, value);
                    //Console.WriteLine("Add new key " + key);
                    continue;
                }

                if (Dict.ContainsKey(key) && Dict[key].Count < maxCoordinatesInValue)
                {
                    Dict[key].Add(coordinate);
                    //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                    continue;
                }

                if (numberOfChars >= maxNumberOfChar)
                {
                    Dict[key].Add(coordinate);
                    //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                }
            }
        }


        /**
         * Store dictionary in a file
         */
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

        /**
        * Bounding Circle
        *
        * Return all the hashString covered by the circle in numberOfChars
        * @param {double} lat
        * @param {double} lon
        * @param {double} radius in meters
        * @param {int} numberOfChars
        * @returns {string[]}
        */

        public static string[] Bcircle(double latitude, double longitude, double radius, int numberOfChars = 9)
        {
            var hashList = new List<string>();
            string hashCenter = GeoHash.Encode(latitude, longitude, numberOfChars);
            hashList.Add(hashCenter);

            GeohashDecodeResult latLon = GeoHash.Decode(hashCenter);

            // Find left and right end
            // Find west(left) end
            Coordinates leftCoor = DistanceToPoint(latitude, longitude, radius, 270);
            string hashLeft = GeoHash.Encode(leftCoor.Lat, leftCoor.Lon, numberOfChars);
            NGeoHash.BoundingBox boxLeft = GeoHash.DecodeBbox(hashLeft);

            // Find east(right) end
            Coordinates rightCoor = DistanceToPoint(latitude, longitude, radius, 90);
            string hashRight = GeoHash.Encode(rightCoor.Lat, rightCoor.Lon, numberOfChars);
            NGeoHash.BoundingBox boxRight = GeoHash.DecodeBbox(hashRight);

            // Find steps(from left to right)
            double perLon = latLon.Error.Lon * 2; // box size(in degree) on west-east direction
            var lonStep = Math.Round((boxRight.Minimum.Lon - boxLeft.Minimum.Lon) / perLon);

            double perLat = latLon.Error.Lat * 2; // box size(in dgree) on north–south direction

            for ( var lon = 0; lon <= lonStep; lon++ )
            {
                // Find current box
                string currentBoxHash = GeoHash.Neighbor(hashLeft, new[] { 0, lon });
                NGeoHash.BoundingBox currentBox = GeoHash.DecodeBbox(currentBoxHash);

                // Find north(upper) end
                // Find up neighbor
                // Check if in range
                int i = 0;
                NGeoHash.BoundingBox upBox = currentBox;
                string upBoxHash = currentBoxHash;
                while (BoxInCircleRange(upBox, latitude, longitude, radius))
                {
                    if (!hashList.Contains(upBoxHash))
                    {
                        hashList.Add(upBoxHash);
                    }
                    //Console.WriteLine("Add+ " + upBoxHash);
                    i++;
                    upBoxHash = GeoHash.Neighbor(currentBoxHash, new[] { i, 0 });
                    upBox = GeoHash.DecodeBbox(upBoxHash);
                }

                // Find south(down) end
                // Find steps(north to south)
                int j = 0;
                NGeoHash.BoundingBox downBox = currentBox;
                string downBoxHash = currentBoxHash;
                while (BoxInCircleRange(downBox, latitude, longitude, radius))
                {
                    if (!hashList.Contains(downBoxHash))
                    {
                        hashList.Add(downBoxHash);
                    }
                    //Console.WriteLine("Add- " + downBoxHash);
                    j--;
                    downBoxHash = GeoHash.Neighbor(currentBoxHash, new[] { j, 0 });
                    downBox = GeoHash.DecodeBbox(downBoxHash);
                }
            }

            // Check each point on the circle, see if covers more box
            // Find step length of radius
            double stepOfRadius = FindMinSideLength(hashCenter) * 0.9;
            // Find step of cricle, devide 360 degree to how many parts
            double stepOfCircle = 360 / (Math.PI * 2 * radius / stepOfRadius);

            for (double degree = 0; degree <= 360; degree += stepOfCircle)
            {
                Coordinates coor = DistanceToPoint(latitude, longitude, radius, degree);
                string hash = GeoHash.Encode(coor.Lat, coor.Lon, numberOfChars);
                if (!hashList.Contains(hash))
                {
                    hashList.Add(hash);
                }
            }
            

            return hashList.ToArray();
        }

        /**
        * Bounding Circle Coordinates
        *
        * Return all coordinates covered by the circle in numberOfChars
        * @param {double} lat
        * @param {double} lon
        * @param {double} radius in meters
        * @param {int} numberOfChars
        * @param {int} maximum number of coordinates returns
        * @returns {Coordinates[]}
        */
        public Coordinates[] BcircleCoordinates(double latitude, double longitude, double radius, int numberOfChars = 9, int limit = 0)
        {
            var coorList = new List<Coordinates>();
            string[] hashList = Bcircle(latitude, longitude, radius, numberOfChars);
            foreach (string hash in hashList)
            {
                // TODO: search all level or search current level only?
                Coordinates[] coors = GetCoordinates(hash);
                BoundingBox box = GeoHash.DecodeBbox(hash);

                if (BoxAllInCircleRange(box, latitude, longitude, radius))
                {
                    // All covered box
                    foreach (Coordinates c in coors)
                    {
                        coorList.Add(c);
                    }
                }
                else
                {
                    // Not all covered box
                    foreach (Coordinates c in coors)
                    {
                        if (Measure(c.Lat, c.Lon, latitude, longitude) <= radius)
                        {
                            coorList.Add(c);
                        }
                    }
                }
            }

            if (limit == 0)
            {
                return coorList.ToArray();
            }

            coorList.Sort((x, y) => Measure(x.Lat, x.Lon, latitude, longitude).CompareTo(Measure(y.Lat, y.Lon, latitude, longitude)));

            if (coorList.Count >= limit)
            {
                return coorList.GetRange(0, limit).ToArray();
            }

            return coorList.ToArray();
        }

        /**
        * Bounding Box Coordinates
        *
        * Return all coordinates covered by the box in numberOfChars
        * @param {double} minLat
        * @param {double} minLon
        * @param {double} maxLat
        * @param {double} maxLon
        * @param {int} numberOfChars
        * @returns {Coordinates[]}
        */
        public Coordinates[] BboxCoordinates(double minLat, double minLon, double maxLat, double maxLon, int numberOfChars = 9)
        {
            var coorList = new List<Coordinates>();
            string[] hashList = GeoHash.Bboxes(minLat, minLon, maxLat, maxLat, numberOfChars);

            foreach (string hash in hashList)
            {
                // TODO: search all level or search current level only?
                Coordinates[] coors = GetCoordinates(hash);
                BoundingBox box = GeoHash.DecodeBbox(hash);

                if (BoxInBoxRange(box, minLat, minLon, maxLat, maxLon))
                {
                    // All covered box
                    foreach (Coordinates c in coors)
                    {
                        coorList.Add(c);
                    }
                }
                else
                {
                    // Not all covered box
                    foreach (Coordinates c in coors)
                    {
                        if (CoordinateInBoxRange(c, minLat, minLon, maxLat, maxLon))
                        {
                            coorList.Add(c);
                        }
                    }
                }
            }

            return coorList.ToArray();
        }

        /**
         * Get all coordinates in all levels
         * 
         */
        private Coordinates[] GetCoordinatesAll(string hash)
        {
            var coorList = new List<Coordinates>();
            if (Dict.ContainsKey(hash))
            {
                coorList.AddRange(Dict[hash]);
                // if has deeper level
                if (FullList.Contains(hash))
                {
                    // find all hash start with hash

                    int level = hash.Length;
                    foreach(string key in Dict.Keys)
                    {
                        if(key.Length>=level && key.Substring(0,level) == hash)
                        {
                            coorList.AddRange(Dict[key]);
                        }
                    }
                }
            }

            //TODO: distince is not good if there are some real duplicates in database
            return coorList.Distinct().ToArray();
        }

        /**
         * Get all coordinates in this level only
         * 
         */
        private Coordinates[] GetCoordinates(string hash)
        {
            if (Dict.ContainsKey(hash))
            {
                return Dict[hash].ToArray();
            }
            return new Coordinates[] { };
        }

        /**
         * Convert meters to coordinate in direction
         *
         * Return coordinates
         * @param {double} lat
         * @param {double} lon
         * @param {double} d (distance in meter)
         * @param {double} bearing (clockwise from north in degree)
         */
        public static Coordinates DistanceToPoint(double lat, double lon, double distance, double brng)
        {
            double radius = 6371000;

            double δ = distance / radius; // angular distance in radians
            double θ = brng * Math.PI / 180;

            double φ1 = lat * Math.PI / 180, λ1 = lon * Math.PI / 180;

            double sinφ2 = Math.Sin(φ1) * Math.Cos(δ) + Math.Cos(φ1) * Math.Sin(δ) * Math.Cos(θ);
            double φ2 = Math.Asin(sinφ2);
            double y = Math.Sin(θ) * Math.Sin(δ) * Math.Cos(φ1);
            double x = Math.Cos(δ) - Math.Sin(φ1) * sinφ2;
            double λ2 = λ1 + Math.Atan2(y, x);

            double lat3 = φ2 * 180 / Math.PI;
            double lon3 = λ2 * 180 / Math.PI;

            return new NGeoHash.Coordinates { Lat = lat3, Lon = lon3 };
        }

        /**
         * Measure the distance between two points
         * Return distance in meters
         */
        public static double Measure( double lat1, double lon1, double lat2, double lon2 )
        {
            var R = 6371; // Radius of earth in KM
            var dLat = lat2 * Math.PI / 180 - lat1 * Math.PI / 180;
            var dLon = lon2 * Math.PI / 180 - lon1 * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return Math.Abs(d * 1000.0); // meters
        }

        /**
         * Check box overlaps with circle
         * Check the four corners of the box
         */
        public static Boolean BoxInCircleRange(BoundingBox box, double lat1, double lon1, double distance )
        {
            // 4 rectangle vertex in circle
            double d1 = Measure( box.Maximum.Lat, box.Maximum.Lon, lat1, lon1 );
            double d2 = Measure( box.Maximum.Lat, box.Minimum.Lon, lat1, lon1 );
            double d3 = Measure( box.Minimum.Lat, box.Maximum.Lon, lat1, lon1 );
            double d4 = Measure( box.Minimum.Lat, box.Minimum.Lon, lat1, lon1 );
            return d1 <= distance || d2 <= distance || d3 <= distance || d4 <= distance;
        }

        /**
         * Check box all covered by circle
         * Check the four corners of the box
         */
        public static Boolean BoxAllInCircleRange(BoundingBox box, double lat1, double lon1, double distance)
        {
            // 4 rectangle vertex in circle
            double d1 = Measure(box.Maximum.Lat, box.Maximum.Lon, lat1, lon1);
            double d2 = Measure(box.Maximum.Lat, box.Minimum.Lon, lat1, lon1);
            double d3 = Measure(box.Minimum.Lat, box.Maximum.Lon, lat1, lon1);
            double d4 = Measure(box.Minimum.Lat, box.Minimum.Lon, lat1, lon1);
            return d1 <= distance && d2 <= distance && d3 <= distance && d4 <= distance;
        }

        /**
         * Check box overlaps with box
         * Check the four corners of the box
         */
        public static Boolean BoxInBoxRange(BoundingBox box, double minLat, double minLon, double maxLat, double maxLon)
        {
            return box.Minimum.Lat >= minLat && box.Minimum.Lon >= minLon && box.Maximum.Lat <= maxLat && box.Maximum.Lon <= maxLon;
        }

        /**
         * Check coordinate inside box
         */
        public static Boolean CoordinateInBoxRange(Coordinates c, double minLat, double minLon, double maxLat, double maxLon)
        {
            return c.Lat >= minLat && c.Lon >= minLon && c.Lat <= maxLat && c.Lon <= maxLon;
        }

        /**
        * Find the length of the shortest side of a box
        */
        public static double FindMinSideLength(string hash)
        {
            BoundingBox box = GeoHash.DecodeBbox(hash);
            double west = Measure(box.Minimum.Lat, box.Minimum.Lon, box.Maximum.Lat, box.Minimum.Lon);
            double east = Measure(box.Minimum.Lat, box.Maximum.Lon, box.Maximum.Lat, box.Maximum.Lon);
            double south = Measure(box.Minimum.Lat, box.Minimum.Lon, box.Minimum.Lat, box.Maximum.Lon);
            double north = Measure(box.Maximum.Lat, box.Maximum.Lon, box.Maximum.Lat, box.Minimum.Lon);

            return Math.Min(Math.Min(west, east), Math.Min(north, south));
        }
    }
}
