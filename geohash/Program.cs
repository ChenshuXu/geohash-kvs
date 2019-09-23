using System;
using NGeoHash;

namespace geohash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // Fort Worth
            var location = new
            {
                latitude = 32.768799,
                longitude = -97.309341,
            };
            var encoded = GeoHash.Encode(location.latitude, location.longitude); // "9vff3tms0"
            Console.WriteLine("encoded=" + encoded);

            var decoded = GeoHash.Decode("9vff3tms0");
            var latitude = decoded.Coordinates.Lat;     // 32.768805027008057
            Console.WriteLine("latitude=" + latitude);
            var longitude = decoded.Coordinates.Lon;	// -97.309319972991943
            Console.WriteLine("longitude=" + longitude);
        }
    }
}
