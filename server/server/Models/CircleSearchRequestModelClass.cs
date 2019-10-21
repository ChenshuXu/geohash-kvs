using System;
namespace server.Models
{
    public class CircleSearchRequestModelClass
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Range { get; set; }
        public int Level { get; set; }
        public int Limit { get; set; }
    }

    public class CircleSearchDisplayRequestModelClass
    {
        public double SelectLat { get; set; }
        public double SelectLon { get; set; }
        public double SearchLat { get; set; }
        public double SearchLon { get; set; }
        public double Range { get; set; }
        public int Level { get; set; }
    }
}
