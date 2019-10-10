using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NGeoHash;
using server.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace server.Controllers
{
    [ApiController]
    
    public class CoordinatesController : Controller
    {
        DatabaseInterface _dbInterface;

        public CoordinatesController(DatabaseInterface dbInterface)
        {
            _dbInterface = dbInterface;
        }

        // GET: /coordinates
        [Route("coordinates")]
        [HttpGet]
        public IEnumerable<Coordinates> Get()
        {
            var db = _dbInterface.GetDatabase();
            return db.BcircleCoordinates(41.87476071, -87.67198792, 5000, 5, 100).ToArray();
            //return new string[] { "value1", "value2", _dbInterface.GetDBinfo() };
        }

        // POST /coordinates
        [Route("coordinates")]
        [HttpPost]
        public IEnumerable<Coordinates> GetCoordinates([FromBody]CircleSearchRequestModelClass data)
        {
            double lat = data.lat;
            double lon = data.lon;
            double range = data.range;
            int level = data.level;
            int limit = data.limit;

            var db = _dbInterface.GetDatabase();
            return db.BcircleCoordinates(lat, lon, range, level, limit).ToArray();
        }

        // POST /coordinatesBboxes
        [Route("coordinatesBboxes")]
        [HttpPost]
        public IEnumerable<BoundingBox> GetBboxes([FromBody]CircleSearchRequestModelClass data)
        {
            double lat = data.lat;
            double lon = data.lon;
            double range = data.range;
            int level = data.level;

            return geohash.DataBase.BcircleBoxes(lat, lon, range, level);
        }

        // POST /BboxCoordinates
        [Route("BboxCoordinates")]
        [HttpPost]
        public IEnumerable<Coordinates> BboxCoordinates([FromBody]BoxSearchRequestClass data)
        {
            double maxLat = data.maxlat;
            double maxLon = data.maxlon;
            double minLat = data.minlat;
            double minLon = data.minlon;
            int level = data.level;

            var db = _dbInterface.GetDatabase();
            return db.BboxCoordinates(minLat, minLon, maxLat, maxLon, level);
        }

        // POST /BboxBoxes
        [Route("BboxBoxes")]
        [HttpPost]
        public IEnumerable<BoundingBox> BboxBoxes([FromBody]BoxSearchRequestClass data)
        {
            double maxLat = data.maxlat;
            double maxLon = data.maxlon;
            double minLat = data.minlat;
            double minLon = data.minlon;
            int level = data.level;

            return geohash.DataBase.BboxBoxes(minLat, minLon, maxLat, maxLon, level);
        }
    }
}
