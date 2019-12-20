using System;
using System.Collections.Generic;
using NGeoHash;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace geohash
{
    public class DataBase
    {
        // Key is encoded code
        // Value is an array of coordinates
        // TODO: will store in a key-value database such as Redis, DynamoDB
        private IDictionary<string, List<Coordinates>> _Dict = new Dictionary<string, List<Coordinates>>();
        private List<string> _FullList = new List<string>();
        private int _NumberOfCharsStart = 1;
        private int _MaxCoordinatesInValue = 500;
        private int _MaxNumberOfChar = 9;

        /// <summary>
        /// Add coordinates to database(not in use)
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="numberOfChars"></param>
        public void Add2( double latitude, double longitude, int numberOfChars = 1 )
        {
            // Get the key
            string key = GeoHash.Encode(latitude, longitude, numberOfChars);
            // Get the value
            Coordinates coordinate = new Coordinates { Lat = latitude, Lon = longitude };

            if (!_Dict.ContainsKey(key))
            {
                List<Coordinates> value = new List<Coordinates> { coordinate };
                _Dict.Add(key, value);
                //Console.WriteLine("Add new key " + key);
                return;
            }

            if (_Dict.ContainsKey(key) && _Dict[key].Count < _MaxCoordinatesInValue)
            {
                _Dict[key].Add(coordinate);
                //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                return;
            }

            if (numberOfChars >= _MaxNumberOfChar)
            {
                _Dict[key].Add(coordinate);
                //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                return;
            }

            // Check how many coordinates under this key
            // If more than maxCoordinatesInValue, encode one more level
            // Also calculate all upper level again, move to a deeper level
            if (_Dict.ContainsKey(key) && _Dict[key].Count >= _MaxCoordinatesInValue)
            {
                if (!_FullList.Contains(key))
                {
                    // Mark current level as full
                    _FullList.Add(key);
                    //Console.WriteLine("Add " + key + " to full list");

                    // Move current level to the deeper level
                    foreach (var coor in _Dict[key])
                    {
                        Add2(coor.Lat, coor.Lon, numberOfChars + 1);
                    }
                }

                // Calculate one more level
                Add2(latitude, longitude, numberOfChars + 1);
            }
        }

        /// <summary>
        /// Add coordinates to database(in use)
        /// </summary>
        /// <param name="coordinate"></param>
        public void Add(Coordinates coordinate)
        {
            double latitude = coordinate.Lat;
            double longitude = coordinate.Lon;
            for (int numberOfChars = 1; numberOfChars <= _MaxNumberOfChar; numberOfChars++)
            {
                // Get the key
                string key = GeoHash.Encode(latitude, longitude, numberOfChars);
                // The value is coordinate

                // First entry
                if (!_Dict.ContainsKey(key))
                {
                    List<Coordinates> value = new List<Coordinates> { coordinate };
                    _Dict.Add(key, value);
                    //Console.WriteLine("Add new key " + key);
                }
                // Second entry
                else if (_Dict.ContainsKey(key) && _Dict[key].Count < _MaxCoordinatesInValue)
                {
                    _Dict[key].Add(coordinate);
                    //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                }
                // The deepest level
                else if (numberOfChars == _MaxCoordinatesInValue)
                {
                    _Dict[key].Add(coordinate);
                    //Console.WriteLine("Add existent key " + key + ", " + Dict[key].Count);
                }
            }
        }

        /// <summary>
        /// Store dictionary in a file
        /// </summary>
        public void Display()
        {
            using (StreamWriter sw = new StreamWriter("dict.csv"))
            {
                foreach (var item in _Dict)
                {
                    //sw.WriteLine(item.Key + "," + item.Value.Count);

                    foreach (var i in item.Value)
                    {
                        string str = item.Key + "," + item.Value.Count + ","
                            + i.Lat.ToString() + "," + i.Lon.ToString() + ","
                            + i.Id + "," + i.LocationDescription;
                        sw.WriteLine(str);
                    }
                }
            }
        }

        public JObject BoundingBoxSearchProcess(Coordinates select, Coordinates searchMax, Coordinates searchMin, int level = 9)
        {
            string hash = GeoHash.Encode(select.Lat, select.Lon, level);

            // get all other points(out of range) in box
            Coordinates[] allCoordinates = GetCoordinates(hash);
            List<Coordinates> coordinatesInRange = new List<Coordinates>();
            List<Coordinates> coordinatesOutOfRange = new List<Coordinates>();
            foreach (Coordinates c in allCoordinates)
            {
                if (CoordinateInBoxRange(c, searchMin.Lat, searchMin.Lon, searchMax.Lat, searchMax.Lon))
                {
                    coordinatesInRange.Add(c);
                }
                else
                {
                    coordinatesOutOfRange.Add(c);
                }
            }

            JObject json = JObject.FromObject(
                new
                {
                    Boxhash = hash,
                    CoordinatesInRange = coordinatesInRange,
                    CoordinatesOutOfRange = coordinatesOutOfRange
                }
            );

            return json;
        }

        public JObject BoundingCircleSearchProcess(double selectLatitude, double selectLongitude, double searchLatitude, double searchLongitude, double radius, int level = 9)
        {
            string hash = GeoHash.Encode(selectLatitude, selectLongitude, level);

            // get all other points(out of range) in box
            Coordinates[] allCoordinates = GetCoordinates(hash);
            List<Coordinates> coordinatesInRange = new List<Coordinates>();
            List<Coordinates> coordinatesOutOfRange = new List<Coordinates>();
            foreach(Coordinates c in allCoordinates)
            {
                if (Measure(c.Lat, c.Lon, searchLatitude, searchLongitude) <= radius)
                {
                    coordinatesInRange.Add(c);
                }
                else
                {
                    coordinatesOutOfRange.Add(c);
                }
            }

            JObject json = JObject.FromObject(
                new
                {
                    Boxhash = hash,
                    CoordinatesInRange = coordinatesInRange,
                    CoordinatesOutOfRange = coordinatesOutOfRange
                }
            );

            return json;
        }

        /// <summary>
        /// Bounding Circle
        /// Get all the hashString covered by the circle in numberOfChars
        /// </summary>
        /// <param name="latitude">latitude of center point</param>
        /// <param name="longitude">longitude of center point</param>
        /// <param name="radius">radius in meters</param>
        /// <param name="numberOfChars">number of characters of hash string</param>
        /// <returns>hash string array</returns>
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

        /// <summary>
        /// Get all bounding boxes that covers the circle
        /// </summary>
        /// <param name="latitude">latitude of center point</param>
        /// <param name="longitude">longitude of center point</param>
        /// <param name="radius">radius in meters</param>
        /// <param name="numberOfChars">number of characters of hash string</param>
        /// <returns>bounding box object array</returns>
        public static BoundingBox[] BcircleBoxes(double latitude, double longitude, double radius, int numberOfChars = 9)
        {
            var boxList = new List<BoundingBox>();
            string[] hashList = Bcircle(latitude, longitude, radius, numberOfChars);
            foreach(string hash in hashList)
            {
                boxList.Add(GeoHash.DecodeBbox(hash));
            }

            return boxList.ToArray();
        }

        /// <summary>
        /// Bounding Circle Coordinates
        /// Get all coordinates covered by the circle in numberOfChars
        /// </summary>
        /// <param name="latitude">latitude of center point</param>
        /// <param name="longitude">longitude of center point</param>
        /// <param name="radius">radius in meters</param>
        /// <param name="numberOfChars">number of characters of hash string</param>
        /// <param name="limit">max number of coordinates return</param>
        /// <returns>array of coordinate object</returns>
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
                    // All covered by circle
                    coorList.AddRange(coors);
                }
                else
                {
                    // Not all covered by circle
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

        /// <summary>
        /// Bounding Box Coordinates
        /// Get all coordinates covered by the box in numberOfChars
        /// </summary>
        /// <param name="minLat"></param>
        /// <param name="minLon"></param>
        /// <param name="maxLat"></param>
        /// <param name="maxLon"></param>
        /// <param name="numberOfChars"></param>
        /// <returns>all coordinates covered by the box in numberOfChars</returns>
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
                    // All covered by box
                    coorList.AddRange(coors);
                }
                else
                {
                    // Not all covered by box
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

        /// <summary>
        /// Get all bounding boxes covered by box
        /// </summary>
        /// <param name="minLat"></param>
        /// <param name="minLon"></param>
        /// <param name="maxLat"></param>
        /// <param name="maxLon"></param>
        /// <param name="numberOfChars"></param>
        /// <returns></returns>
        public static BoundingBox[] BboxBoxes(double minLat, double minLon, double maxLat, double maxLon, int numberOfChars = 9)
        {
            
            var boxList = new List<BoundingBox>();
            string[] hashList = GeoHash.Bboxes(minLat, minLon, maxLat, maxLon, numberOfChars);
            foreach (string hash in hashList)
            {
                boxList.Add(GeoHash.DecodeBbox(hash));
            }

            return boxList.ToArray();
        }

        /// <summary>
        /// Get all coordinates in all levels
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private Coordinates[] GetCoordinatesAll(string hash)
        {
            var coorList = new List<Coordinates>();
            if (_Dict.ContainsKey(hash))
            {
                coorList.AddRange(_Dict[hash]);
                // if has deeper level
                if (_FullList.Contains(hash))
                {
                    // find all hash start with hash

                    int level = hash.Length;
                    foreach(string key in _Dict.Keys)
                    {
                        if(key.Length>=level && key.Substring(0,level) == hash)
                        {
                            coorList.AddRange(_Dict[key]);
                        }
                    }
                }
            }

            //TODO: distinct is not good if there are some real duplicates in database
            //return coorList.Distinct().ToArray();
            return coorList.ToArray();
        }

        /// <summary>
        /// Get all coordinates in this level only
        /// </summary>
        /// <param name="hash">hash of the box</param>
        /// <returns>coordinates array</returns>
        private Coordinates[] GetCoordinates(string hash)
        {
            if (_Dict.ContainsKey(hash))
            {
                return _Dict[hash].ToArray();
            }
            return new Coordinates[] { };
        }

        /// <summary>
        /// Convert meters to coordinate in direction
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="distance">distance in meter</param>
        /// <param name="brng">bearing (clockwise from north in degree)</param>
        /// <returns></returns>
        public static Coordinates DistanceToPoint(double lat, double lon, double distance, double brng)
        {
            double radius = 6371000; // radius of earth

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

        /// <summary>
        /// Measure the distance between two points
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns>distance in meters</returns>
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

        /// <summary>
        /// Check box overlaps with circle
        /// It's not perfect, only check the four corners of the box
        /// </summary>
        /// <param name="box"></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Boolean BoxInCircleRange(BoundingBox box, double lat, double lon, double distance )
        {
            // 4 rectangle vertex in circle
            double d1 = Measure( box.Maximum.Lat, box.Maximum.Lon, lat, lon );
            double d2 = Measure( box.Maximum.Lat, box.Minimum.Lon, lat, lon );
            double d3 = Measure( box.Minimum.Lat, box.Maximum.Lon, lat, lon );
            double d4 = Measure( box.Minimum.Lat, box.Minimum.Lon, lat, lon );
            return d1 <= distance || d2 <= distance || d3 <= distance || d4 <= distance;
        }

        /// <summary>
        /// Check box all covered by circle
        /// </summary>
        /// <param name="box"></param>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Boolean BoxAllInCircleRange(BoundingBox box, double lat1, double lon1, double distance)
        {
            // 4 rectangle vertex in circle
            double d1 = Measure(box.Maximum.Lat, box.Maximum.Lon, lat1, lon1);
            double d2 = Measure(box.Maximum.Lat, box.Minimum.Lon, lat1, lon1);
            double d3 = Measure(box.Minimum.Lat, box.Maximum.Lon, lat1, lon1);
            double d4 = Measure(box.Minimum.Lat, box.Minimum.Lon, lat1, lon1);
            return d1 <= distance && d2 <= distance && d3 <= distance && d4 <= distance;
        }

        /// <summary>
        /// Check box overlaps with box
        /// </summary>
        /// <param name="box"></param>
        /// <param name="minLat"></param>
        /// <param name="minLon"></param>
        /// <param name="maxLat"></param>
        /// <param name="maxLon"></param>
        /// <returns></returns>
        public static Boolean BoxInBoxRange(BoundingBox box, double minLat, double minLon, double maxLat, double maxLon)
        {
            return box.Minimum.Lat >= minLat && box.Minimum.Lon >= minLon && box.Maximum.Lat <= maxLat && box.Maximum.Lon <= maxLon;
        }

        /// <summary>
        /// Check coordinate inside box
        /// </summary>
        /// <param name="c">coordinate</param>
        /// <param name="minLat"></param>
        /// <param name="minLon"></param>
        /// <param name="maxLat"></param>
        /// <param name="maxLon"></param>
        /// <returns></returns>
        public static Boolean CoordinateInBoxRange(Coordinates c, double minLat, double minLon, double maxLat, double maxLon)
        {
            return c.Lat >= minLat && c.Lon >= minLon && c.Lat <= maxLat && c.Lon <= maxLon;
        }

        /// <summary>
        /// Find the length of the shortest side of a box
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>length in meter</returns>
        public static double FindMinSideLength(string hash)
        {
            BoundingBox box = GeoHash.DecodeBbox(hash);
            double west = Measure(box.Minimum.Lat, box.Minimum.Lon, box.Maximum.Lat, box.Minimum.Lon);
            double east = Measure(box.Minimum.Lat, box.Maximum.Lon, box.Maximum.Lat, box.Maximum.Lon);
            double south = Measure(box.Minimum.Lat, box.Minimum.Lon, box.Minimum.Lat, box.Maximum.Lon);
            double north = Measure(box.Maximum.Lat, box.Maximum.Lon, box.Maximum.Lat, box.Minimum.Lon);

            return Math.Min(Math.Min(west, east), Math.Min(north, south));
        }

        /// <summary>
        /// Bounding Polygon
        /// Get all the hashString covered by the polygon in numberOfChars
        /// </summary>
        /// <param name="polygon">array of coordinates describes the polygon</param>
        /// <param name="numberOfChars"></param>
        /// <returns>array of hash string</returns>
        public static string[] Bpolygon(Coordinates[] polygon, int numberOfChars = 9)
        {
            var hashList = new List<string>();
            // Get all bounding boxes that are possible be covered by polygon
            Coordinates max = new Coordinates { Lat = -90, Lon = -180 };
            Coordinates min = new Coordinates { Lat = 90, Lon = 180 };
            foreach (Coordinates c in polygon)
            {
                max.Lat = Math.Max(max.Lat, c.Lat);
                max.Lon = Math.Max(max.Lon, c.Lon);
                min.Lat = Math.Min(min.Lat, c.Lat);
                min.Lon = Math.Min(min.Lon, c.Lon);
            }
            string[] bboxHash = GeoHash.Bboxes(min.Lat, min.Lon, max.Lat, max.Lon, numberOfChars);
            foreach (string hash in bboxHash)
            {
                BoundingBox box = GeoHash.DecodeBbox(hash);
                if (BoxOverlapPolygon(box, polygon))
                {
                    hashList.Add(hash);
                }
            }
            return hashList.ToArray();
        }

        /// <summary>
        /// Get all bounding boxes covered by polygon in numberOfChars
        /// </summary>
        /// <param name="polygon">array of coordinates describes the polygon</param>
        /// <param name="numberOfChars"></param>
        /// <returns>array of hash string</returns>
        public static BoundingBox[] BpolygonBoxes(Coordinates[] polygon, int numberOfChars = 9)
        {
            var boxList = new List<BoundingBox>();
            string[] hashList = Bpolygon(polygon, numberOfChars);
            foreach(string hash in hashList)
            {
                boxList.Add(GeoHash.DecodeBbox(hash));
            }
            return boxList.ToArray();
        }
        
        /// <summary>
        /// Bounding polygon coordinates
        /// Get all the coordinates covered by the polygon
        /// </summary>
        /// <param name="polygon">list of vertex coordinates of the polygon</param>
        /// <param name="numberOfChars"></param>
        /// <returns></returns>
        public Coordinates[] BpolygonCoordinates(Coordinates[] polygon, int numberOfChars = 9)
        {
            List<Coordinates> coorList = new List<Coordinates>();

            string[] bbox = Bpolygon(polygon, numberOfChars);
            foreach(var hash in bbox)
            {
                Coordinates[] coors = GetCoordinates(hash);
                foreach(Coordinates c in coors)
                {
                    if (PointInPolygon(c, polygon))
                    {
                        coorList.Add(c);
                    }
                }
            }

            return coorList.ToArray();
        }

        public JObject BpolygonSearchProcess(Coordinates select, Coordinates[] polygon, int level)
        {
            string hash = GeoHash.Encode(select.Lat, select.Lon, level);
            // get all other points(out of range) in box
            Coordinates[] allCoordinates = GetCoordinates(hash);
            List<Coordinates> coordinatesInRange = new List<Coordinates>();
            List<Coordinates> coordinatesOutOfRange = new List<Coordinates>();
            foreach (Coordinates c in allCoordinates)
            {
                if (PointInPolygon(c, polygon))
                {
                    coordinatesInRange.Add(c);
                }
                else
                {
                    coordinatesOutOfRange.Add(c);
                }
            }
            JObject json = JObject.FromObject(
                new
                {
                    Boxhash = hash,
                    CoordinatesInRange = coordinatesInRange,
                    CoordinatesOutOfRange = coordinatesOutOfRange
                }
            );

            return json;
        }

        /// <summary>
        /// Check if box overlap with polygon
        /// </summary>
        /// <param name="box"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static Boolean BoxOverlapPolygon(BoundingBox box, Coordinates[] polygon)
        {
            Coordinates[] boxVertices = {
                box.Maximum,
                new Coordinates{ Lat=box.Maximum.Lat, Lon=box.Minimum.Lon },
                box.Minimum,
                new Coordinates{ Lat=box.Minimum.Lat, Lon=box.Maximum.Lon }
            };
            // Check polygon vertices in box
            foreach (Coordinates c in boxVertices)
            {
                if (PointInPolygon(c, polygon))
                {
                    return true;
                }
            }
            // Check box vertices in polygon
            foreach (Coordinates c in polygon)
            {
                if (PointInPolygon(c, boxVertices))
                {
                    return true;
                }
            }
            // Check intersection between box and polygon edges
            int i = 0, n = polygon.Length, j = 0;
            do
            {
                int next = (i + 1) % n;
                do
                {
                    int next2 = (j + 1) % 4;
                    if (doIntersect(polygon[i], polygon[next], boxVertices[j], boxVertices[next2]))
                    {
                        return true;
                    }
                    j = next2;
                } while (j != 0);
                i = next;
            } while (i != 0);
            return false;
        }

        /// <summary>
        /// Check if a point is inside the polygon
        /// </summary>
        /// <param name="point"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static Boolean PointInPolygon(Coordinates point, Coordinates[] polygon)
        {
            // There must be at least 3 vertices in polygon
            if (polygon.Length < 3)
            {
                return false;
            }
            int n = polygon.Length;
            // Create a point for line segment from p to infinite
            Coordinates extreme = new Coordinates { Lat = 180, Lon = point.Lon};
            // Count intersections of the above line with sides of polygon
            int count = 0, i = 0;
            do
            {
                int next = (i + 1) % n;

                // Check if the line segment from 'p' to 'extreme' intersects 
                // with the line segment from 'polygon[i]' to 'polygon[next]' 
                if (doIntersect(polygon[i], polygon[next], point, extreme))
                {
                    // If the point 'p' is colinear with line segment 'i-next', 
                    // then check if it lies on segment. If it lies, return true, 
                    // otherwise false 
                    if (orientation(polygon[i], point, polygon[next]) == 0)
                        return onSegment(polygon[i], point, polygon[next]);

                    count++;
                }
                i = next;
            } while (i != 0);
            // Return true if count is odd, false otherwise 
            return count % 2 == 1;
        }

        // The function that returns true if line segment 'p1q1' 
        // and 'p2q2' intersect. 
        private static bool doIntersect(Coordinates p1, Coordinates q1, Coordinates p2, Coordinates q2)
        {
            // Find the four orientations needed for general and 
            // special cases 
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case 
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases 
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and p2 are colinear and q2 lies on segment p1q1 
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases 
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are colinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        private static int orientation(Coordinates p, Coordinates q, Coordinates r)
        {
            double val = (q.Lat - p.Lat) * (r.Lon - q.Lon) -
              (q.Lon - p.Lon) * (r.Lat - q.Lat);

            if (Math.Abs(val) < 0.000000001f) return 0;  // colinear 
            return (val > 0) ? 1 : 2; // clock or counterclock wise 
        }

        // Given three colinear points p, q, r, the function checks if 
        // point q lies on line segment 'pr' 
        private static bool onSegment(Coordinates p, Coordinates q, Coordinates r)
        {
            if (q.Lon <= Math.Max(p.Lon, r.Lon) && q.Lon >= Math.Min(p.Lon, r.Lon) &&
                    q.Lat <= Math.Max(p.Lat, r.Lat) && q.Lat >= Math.Min(p.Lat, r.Lat))
            {
                return true;
            }
            return false;
        }
    }
}
