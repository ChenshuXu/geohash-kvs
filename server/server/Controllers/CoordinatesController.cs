using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using geohash;
using NGeoHash;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace server.Controllers
{
    [ApiController]
    [Route("coordinates")]
    public class CoordinatesController : Controller
    {
        DatabaseInterface _dbInterface;

        public CoordinatesController(DatabaseInterface dbInterface)
        {
            _dbInterface = dbInterface;
        }

        // GET: /coordinates
        [HttpGet]
        public IEnumerable<Coordinates> Get()
        {
            var db = _dbInterface.GetDatabase();
            return db.BcircleCoordinates(41.87476071, -87.67198792, 5000, 5, 0).ToArray();
            //return new string[] { "value1", "value2", _dbInterface.GetDBinfo() };
        }

        // GET /coordinates/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return id.ToString();
        }

        // POST /coordinates
        [HttpPost]
        public IEnumerable<Coordinates> Post([FromBody]JObject data)
        {
            double lat = (double)data["lat"];
            double lon = (double)data["lon"];
            double range = (double)data["range"];
            int level = (int)data["level"];
            int limit = (int)data["limit"];

            var db = _dbInterface.GetDatabase();
            return db.BcircleCoordinates(lat, lon, range, level, limit).ToArray();
        }
    }
}
