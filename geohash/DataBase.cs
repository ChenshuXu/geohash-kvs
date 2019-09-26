using System;
using System.Collections.Generic;
using NGeoHash;
using System.IO;
using SharpKml.Dom;
using SharpKml.Base;

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

        public void Add( double latitude, double longitude, int numberOfChars = 1 )
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
                        Add(coor.Lat, coor.Lon, numberOfChars + 1);
                    }
                }

                // Calculate one more level
                Add(latitude, longitude, numberOfChars + 1);
            }
        }

        /**
         * Store dictionary in a file
         *
         *
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

        public static string[] Bcircle( double latitude, double longitude, double radius, int numberOfChars = 9 )
        {
            var hashList = new List<string>();
            string hashCenter = GeoHash.Encode(latitude, longitude, numberOfChars);
            hashList.Add(hashCenter);

            GeohashDecodeResult latLon = GeoHash.Decode(hashCenter);

            // Find left and right end
            // Find west(left) end
            Coordinates leftCoor = MetersToCoordinate(latitude, longitude, radius, 270);
            string hashLeft = GeoHash.Encode(leftCoor.Lat, leftCoor.Lon, numberOfChars);
            NGeoHash.BoundingBox boxLeft = GeoHash.DecodeBbox(hashLeft);

            // Find east(right) end
            Coordinates rightCoor = MetersToCoordinate(latitude, longitude, radius, 90);
            string hashRight = GeoHash.Encode(rightCoor.Lat, rightCoor.Lon, numberOfChars);
            NGeoHash.BoundingBox boxRight = GeoHash.DecodeBbox(hashRight);

            // Find steps(left to right)
            double perLon = latLon.Error.Lon * 2; // box size in west-east direction
            var lonStep = Math.Round((boxRight.Minimum.Lon - boxLeft.Minimum.Lon) / perLon);

            double perLat = latLon.Error.Lat * 2; // box size in north–south direction

            

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
                while (BoxInRange(upBox, latitude, longitude, radius))
                {
                    hashList.Add(upBoxHash);
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
                while (BoxInRange(downBox, latitude, longitude, radius))
                {
                    hashList.Add(downBoxHash);
                    //Console.WriteLine("Add- " + downBoxHash);
                    j--;
                    downBoxHash = GeoHash.Neighbor(currentBoxHash, new[] { j, 0 });
                    downBox = GeoHash.DecodeBbox(downBoxHash);
                }
                //Console.WriteLine("one loop");
            }

            return hashList.ToArray();
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
        public static Coordinates MetersToCoordinate( double lat, double lon, double d, double brng )
        {
            double R = 6378137.0;

            // convert to radian
            double lat2 = lat * Math.PI / 180;
            double lon2 = lon * Math.PI / 180;
            double brng2 = brng * Math.PI / 180;

            // Calculate and convert to degree
            var lat3 = Math.Asin(Math.Sin(lat2) * Math.Cos(d / R) +
                    Math.Cos(lat2) * Math.Sin(d / R) * Math.Cos(brng2)) * 180.0/Math.PI;
            var lon3 = (lon2 + Math.Atan2(Math.Sin(brng2) * Math.Sin(d / R) * Math.Cos(lat2),
                        Math.Cos(d / R) - Math.Sin(lat2) * Math.Sin(lat2))) * 180/Math.PI;

            // Normalise to −180 +180
            double lat4 = (lat3 + 540.0) % 360 - 180.0;
            double lon4 = (lon3 + 540.0) % 360 - 180.0;

            return new NGeoHash.Coordinates { Lat = lat4, Lon = lon4 };
        }

        /**
         * Measure the distance between two points
         *
         * Return distance in meters
         */
        public static double Measure( double lat1, double lon1, double lat2, double lon2 )
        {
            // generally used geo measurement function
            var R = 6378.137; // Radius of earth in KM
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
         * ***Need improvement***
         * Check whether the box overlaps with circle
         *
         *
         */
        public static Boolean BoxInRange(NGeoHash.BoundingBox box, double lat1, double lon1, double distance )
        {
            // 4 rectangle vertex in circle
            double d1 = Measure( box.Maximum.Lat, box.Maximum.Lon, lat1, lon1 );
            double d2 = Measure( box.Maximum.Lat, box.Minimum.Lon, lat1, lon1 );
            double d3 = Measure( box.Minimum.Lat, box.Maximum.Lon, lat1, lon1 );
            double d4 = Measure( box.Minimum.Lat, box.Minimum.Lon, lat1, lon1 );
            if ( d1 <= distance || d2 <= distance || d3 <= distance || d4 <= distance )
            {
                return true;
            }

            // distance between centers
            double middleLat = ( box.Maximum.Lat + box.Minimum.Lat ) / 2;
            double middleLon = ( box.Maximum.Lon + box.Minimum.Lat ) / 2;
            double dR = Measure( middleLat, middleLon, lat1, lon1 );

            double west = Measure(box.Minimum.Lat, box.Minimum.Lon, box.Maximum.Lat, box.Minimum.Lon);
            double east = Measure(box.Minimum.Lat, box.Maximum.Lon, box.Maximum.Lat, box.Maximum.Lon);
            double south = Measure(box.Minimum.Lat, box.Minimum.Lon, box.Minimum.Lat, box.Maximum.Lon);
            double north = Measure(box.Maximum.Lat, box.Maximum.Lon, box.Maximum.Lat, box.Minimum.Lon);

            double l1 = Math.Sqrt(Math.Pow(south / 2, 2) + Math.Pow(west / 2, 2)) + distance;
            double l2 = Math.Sqrt(Math.Pow(south / 2, 2) + Math.Pow(east / 2, 2)) + distance;
            double l3 = Math.Sqrt(Math.Pow(north / 2, 2) + Math.Pow(west / 2, 2)) + distance;
            double l4 = Math.Sqrt(Math.Pow(north / 2, 2) + Math.Pow(east / 2, 2)) + distance;

            if ( l1 <= dR || l2 <= dR || l3 <= dR || l4 <= dR)
            {
                //return true;
            }

            return false;
        }

        /**
         * Draw bounding circle
         * Generate kml from a list of boxhash
         *
         */
        public static void GenerateKMLBoundingCircle( string[] hashList, double latitude, double longitude, double radius, string fileName )
        {
            string kmlStr = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"" >
    <Document>
        <name> Paths </name>
        <Style id=""yellowLineGreenPoly"">
            <LineStyle>
                <color> 7f00ffff </color>
                <width> 4 </width>
            </LineStyle>
            <PolyStyle>
                <color> 7f00ff00 </color>
            </PolyStyle>
        </Style>
        <Placemark>
            <styleUrl>#yellowLineGreenPoly</styleUrl>
            <MultiGeometry>";

            foreach(string hash in hashList)
            {
                kmlStr += "\n";
                kmlStr += @"            <LineString>
                    <extrude> 1 </extrude >
                    <tessellate> 1 </tessellate>
                    <altitudeMode> absolute </altitudeMode>
                        <coordinates> ";
                var box = GeoHash.DecodeBbox(hash);

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += @"                        </coordinates>
            </LineString>";
            }

            kmlStr += "\n";
            kmlStr += @"            <LineString>
                    <extrude> 1 </extrude >
                    <tessellate> 1 </tessellate>
                    <altitudeMode> absolute </altitudeMode>
                        <coordinates> ";

            // Draw the circle
            for (double degree = 0; degree < 360; degree += 0.5)
            {
                var coor = MetersToCoordinate(latitude, longitude, radius, degree);


                kmlStr += coor.Lon.ToString();
                kmlStr += ",";
                kmlStr += coor.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";


            }
            kmlStr += @"                        </coordinates>
            </LineString>";

            // Draw center
            kmlStr += "<Point><coordinates>" +
                longitude.ToString() + "," + latitude.ToString()
                + "</coordinates></Point>";

            kmlStr += @"
            </MultiGeometry>
        </Placemark>
    </Document>
</kml>";
            using (StreamWriter sw = new StreamWriter(fileName + ".kml"))
            {
                sw.WriteLine(kmlStr);
            }
        }


        /**
         * Draw bounding boxes
         * Generate kml from a list of boxhash
         *
         */
        public static void GenerateKMLBoundingBoxes(string[] hashList, Coordinates coorMin, Coordinates coorMax, string fileName)
        {
            string kmlStr = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"" >
    <Document>
        <name> Paths </name>
        <Style id=""yellowLineGreenPoly"">
            <LineStyle>
                <color> 7f00ffff </color>
                <width> 4 </width>
            </LineStyle>
            <PolyStyle>
                <color> 7f00ff00 </color>
            </PolyStyle>
        </Style>
        <Placemark>
            <styleUrl>#redLineGreenPoly</styleUrl>
            <MultiGeometry>";

            foreach (string hash in hashList)
            {
                //Console.WriteLine(hash+",");
                kmlStr += "\n";
                kmlStr += @"
                <LineString>
                    <extrude> 1 </extrude >
                    <tessellate> 1 </tessellate>
                    <altitudeMode> absolute </altitudeMode>
                        <coordinates> ";
                var box = GeoHash.DecodeBbox(hash);

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",2357";
                kmlStr += "\n";

                kmlStr += @"                        </coordinates>
            </LineString>";
            }

            // Draw bounding box
            kmlStr += "\n";
            kmlStr += @"            <LineString>
                    <extrude> 1 </extrude >
                    <tessellate> 1 </tessellate>
                    <altitudeMode> absolute </altitudeMode>
                        <coordinates> ";

            kmlStr += coorMin.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMin.Lat.ToString();
            kmlStr += ",2357";
            kmlStr += "\n";

            kmlStr += coorMin.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMax.Lat.ToString();
            kmlStr += ",2357";
            kmlStr += "\n";

            kmlStr += coorMax.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMax.Lat.ToString();
            kmlStr += ",2357";
            kmlStr += "\n";

            kmlStr += coorMax.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMin.Lat.ToString();
            kmlStr += ",2357";
            kmlStr += "\n";

            kmlStr += coorMin.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMin.Lat.ToString();
            kmlStr += ",2357";
            kmlStr += "\n";

            kmlStr += @"                        </coordinates>
            </LineString>";

            kmlStr += @"
            </MultiGeometry>
        </Placemark>
    </Document>
</kml>";
            using (StreamWriter sw = new StreamWriter(fileName + ".kml"))
            {
                sw.WriteLine(kmlStr);
            }
        }
    }
}
