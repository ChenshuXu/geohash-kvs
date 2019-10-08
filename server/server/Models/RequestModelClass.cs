using System;
namespace server.Models
{
    public class RequestModelClass
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public double range { get; set; }
        public int level { get; set; }
        public int limit { get; set; }
    }
}
