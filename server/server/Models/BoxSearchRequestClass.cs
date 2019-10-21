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
}
