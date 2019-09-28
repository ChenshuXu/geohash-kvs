using System;
using System.Collections.Generic;
using System.IO;
using geohash;
using NGeoHash;

namespace KML
{
    public static class KMLGenerator
    {
        /**
         * Draw bounding circle
         * Generate kml from a list of boxhash
         */
        public static void GenerateKMLBoundingCircle(string[] hashList, double latitude, double longitude, double radius, string fileName)
        {
            string kmlStr = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"" >
    <Document>
        <name>BoundingCircle</name>
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

            foreach (string hash in hashList)
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
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",20";
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
                var coor = DataBase.DistanceToPoint(latitude, longitude, radius, degree);


                kmlStr += coor.Lon.ToString();
                kmlStr += ",";
                kmlStr += coor.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                //Console.WriteLine(Measure(latitude, longitude, coor.Lat, coor.Lon));
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
         */
        public static void GenerateKMLBoundingBoxes(string[] hashList, Coordinates coorMin, Coordinates coorMax, string fileName)
        {
            string kmlStr = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"" >
    <Document>
        <name>BoundingBoxes</name>
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
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Minimum.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Minimum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",20";
                kmlStr += "\n";

                kmlStr += box.Maximum.Lon.ToString();
                kmlStr += ",";
                kmlStr += box.Maximum.Lat.ToString();
                kmlStr += ",20";
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
            kmlStr += ",20";
            kmlStr += "\n";

            kmlStr += coorMin.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMax.Lat.ToString();
            kmlStr += ",20";
            kmlStr += "\n";

            kmlStr += coorMax.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMax.Lat.ToString();
            kmlStr += ",20";
            kmlStr += "\n";

            kmlStr += coorMax.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMin.Lat.ToString();
            kmlStr += ",20";
            kmlStr += "\n";

            kmlStr += coorMin.Lon.ToString();
            kmlStr += ",";
            kmlStr += coorMin.Lat.ToString();
            kmlStr += ",20";
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

        /**
         * Draw all coordinates
         */
        public static void GenerateKMLcoordinates(Coordinates[] coordinates, string fileName)
        {
            string kmlStr = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"" >
    <Document>
        <name>Coordinates</name>
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

            foreach (Coordinates c in coordinates)
            {
                kmlStr += "\n";
                kmlStr += "<Point><coordinates>";
                kmlStr += c.Lon + "," + c.Lat + "," + "2700";
                kmlStr += "</coordinates></Point>";
            }

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

        public static void GenerateKMLallCoordinates(string fileName, Dictionary<string, List<Coordinates>> Dict)
        {
            string kmlStr = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"" >
    <Document>
        <name>All Coordinates</name>
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

            foreach (var v in Dict.Values)
            {
                foreach (Coordinates c in v)
                {
                    kmlStr += "\n";
                    kmlStr += "<Point><coordinates>";
                    kmlStr += c.Lon + "," + c.Lat + "," + "2700";
                    kmlStr += "</coordinates></Point>";
                }
            }

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
