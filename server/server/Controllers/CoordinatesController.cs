using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        // POST /CircleSearchCoordinates
        [Route("CircleSearchCoordinates")]
        [HttpPost]
        public IEnumerable<Coordinates> GetCircleSearchCoordinates([FromBody]CircleSearchRequestModelClass data)
        {
            double lat = data.Lat;
            double lon = data.Lon;
            double range = data.Range;
            int level = data.Level;
            int limit = data.Limit;

            var db = _dbInterface.GetDatabase();
            return db.BcircleCoordinates(lat, lon, range, level, limit).ToArray();
        }

        // POST /CircleSearchBboxes
        [Route("CircleSearchBboxes")]
        [HttpPost]
        public IEnumerable<BoundingBox> GetCircleSearchBboxes([FromBody]CircleSearchRequestModelClass data)
        {
            double lat = data.Lat;
            double lon = data.Lon;
            double range = data.Range;
            int level = data.Level;

            return geohash.DataBase.BcircleBoxes(lat, lon, range, level);
        }

        // POST /DisplayBoundingCircleSearchProcess
        [Route("DisplayBoundingCircleSearchProcess")]
        [HttpPost]
        public string GetBoundingCircleSearchProcess([FromBody]CircleSearchDisplayRequestModelClass data)
        {
            double selectLat = data.Select.Lat;
            double selectLon = data.Select.Lon;
            double lat = data.SearchLat;
            double lon = data.SearchLon;
            double range = data.Range;
            int level = data.Level;

            var db = _dbInterface.GetDatabase();
            return JsonConvert.SerializeObject(db.BoundingCircleSearchProcess(selectLat, selectLon, lat, lon, range, level));
        }

        // POST /BoxSearchCoordinates
        [Route("BoxSearchCoordinates")]
        [HttpPost]
        public IEnumerable<Coordinates> GetBoxSearchCoordinates([FromBody]BoxSearchRequestClass data)
        {
            double maxLat = data.Maxlat;
            double maxLon = data.Maxlon;
            double minLat = data.Minlat;
            double minLon = data.Minlon;
            int level = data.Level;

            var db = _dbInterface.GetDatabase();
            return db.BboxCoordinates(minLat, minLon, maxLat, maxLon, level);
        }

        // POST /BoxSearchBboxes
        [Route("BoxSearchBboxes")]
        [HttpPost]
        public IEnumerable<BoundingBox> GetBoxSearchBboxes([FromBody]BoxSearchRequestClass data)
        {
            double maxLat = data.Maxlat;
            double maxLon = data.Maxlon;
            double minLat = data.Minlat;
            double minLon = data.Minlon;
            int level = data.Level;

            return geohash.DataBase.BboxBoxes(minLat, minLon, maxLat, maxLon, level);
        }

        // POST /DisplayBoundingBoxSearchProcess
        [Route("DisplayBoundingBoxSearchProcess")]
        [HttpPost]
        public string GetBoundingBoxSearchProcess([FromBody]BoxSearchDisplayRequestModelClass data)
        {
            Coordinates select = data.Select;
            Coordinates max = new Coordinates { Lat = data.SearchMaxLat, Lon = data.SearchMaxLon };
            Coordinates min = new Coordinates { Lat = data.SearchMinLat, Lon = data.SearchMinLon };

            var db = _dbInterface.GetDatabase();
            return JsonConvert.SerializeObject(db.BoundingBoxSearchProcess(select, max, min, data.Level));
        }

        // POST /PolygonSearchCoordinates
        [Route("PolygonSearchCoordinates")]
        [HttpPost]
        public IEnumerable<Coordinates> GetPolygonSearchCoordinates([FromBody]PolygonSearchRequestModelClass data)
        {
            var db = _dbInterface.GetDatabase();
            return db.BpolygonCoordinates(data.Vertices, data.Level);
        }

        // POST /PolygonSearchBoxes
        [Route("PolygonSearchBoxes")]
        [HttpPost]
        public IEnumerable<BoundingBox> GetPolygonSearchBoxes([FromBody]PolygonSearchRequestModelClass data)
        {
            return geohash.DataBase.BpolygonBoxes(data.Vertices, data.Level);
        }

        // POST /PolygonSearchProcess
        [Route("PolygonSearchProcess")]
        [HttpPost]
        public string GetPolygonSearchProcess([FromBody]PolygonSearchProcessRequestModelClass data)
        {
            var db = _dbInterface.GetDatabase();
            return JsonConvert.SerializeObject(db.BpolygonSearchProcess(data.Select, data.Vertices, data.Level));
        }
    }
}
