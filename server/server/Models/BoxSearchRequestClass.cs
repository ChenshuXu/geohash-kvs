using NGeoHash;
using System;
namespace server.Models
{
    public class BoxSearchRequestClass
    {
        public double Maxlat { get; set; }
        public double Maxlon { get; set; }
        public double Minlat { get; set; }
        public double Minlon { get; set; }
        public int Level { get; set; }
        public int Limit { get; set; }
    }

    public class BoxSearchDisplayRequestModelClass
    {
        public Coordinates Select { get; set; }
        public double SearchMaxLat { get; set; }
        public double SearchMaxLon { get; set; }
        public double SearchMinLat { get; set; }
        public double SearchMinLon { get; set; }
        public int Level { get; set; }
        public int Limit { get; set; }
    }
}
