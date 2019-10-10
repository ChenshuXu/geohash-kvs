using System;
namespace server.Models
{
    public class BoxSearchRequestClass
    {
        public double maxlat { get; set; }
        public double maxlon { get; set; }
        public double minlat { get; set; }
        public double minlon { get; set; }
        public int level { get; set; }
        public int limit { get; set; }
    }
}
