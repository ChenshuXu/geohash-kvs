function AjaxMap(sel) {
    console.log('map started');

    this.form = $(sel);
    this.lat = 41.87476071;
    this.lon = -87.67198792;
    this.range = 5000;
    this.level = 5;
    this.limit = 100;

    // setup basic map
    this.map = L.map('mapid').setView([this.lat, this.lon], 12);
    this.layerGroup = L.layerGroup().addTo(this.map);
    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw', {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, ' +
            '<a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
            'Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
        id: 'mapbox.streets'
    }).addTo(this.map);

    this.configButton();
    this.updateMap();
}

AjaxMap.prototype.configButton = function()
{
    var that = this;

    // popup
    var popup = L.popup();
    function onMapClick(e) {
        popup
            .setLatLng(e.latlng)
            .setContent("You clicked the map at " + e.latlng.toString() + "<input type=\"button\" id=\"popup-submit\" value=\"center here\">")
            .openOn(that.map);

        document.getElementById("popup-submit").onclick = function (event) {
            that.lat = e.latlng.lat;
            that.lon = e.latlng.lng;
            that.updateMap();
            that.updateInput();
        }
    }
    this.map.on('click', onMapClick);

    // get input
    var lat = document.getElementById("input_lat");
    var lon = document.getElementById("input_lon");
    var range = document.getElementById("input_range");
    var level = document.getElementById("input_level");
    var limit = document.getElementById("input_limit");
    var message = document.getElementById("message");

    lat.oninput = function(event) {
        if (lat.value === "" || isNaN(Number(lat.value)))
        {
            console.log("error lat");
            message.innerHTML = "error lat";
            return;
        }
        that.lat = Number(lat.value);
        that.updateMap();
    };

    lon.oninput = function(event) {
        if (lon.value === "" || isNaN(Number(lon.value)))
        {
            console.log("error lon");
            message.innerHTML = "error lon";
            return;
        }
        that.lon = Number(lon.value);
        that.updateMap();
    };

    range.oninput = function(event) {
        if (range.value === "" || isNaN(Number(range.value)))
        {
            console.log("error range");
            message.innerHTML = "error range";
            return;
        }
        that.range = Number(range.value);
        that.updateMap();
    };

    limit.oninput = function(event) {
        if (limit.value === "" || !Number.isInteger(Number(limit.value)))
        {
            console.log("error limit");
            message.innerHTML = "error limit";
            return;
        }
        that.limit = Number(limit.value);
        that.updateMap();
    };

    // update the value on the level input slider
    var slider = document.getElementById("input_level");
    var output = document.getElementById("input_level_value");
    output.innerHTML = slider.value;
    slider.oninput = function() {
        output.innerHTML = this.value;
        that.level = Number(this.value);
        that.updateMap();
    };

    //console.log(this);
}

AjaxMap.prototype.updateInput = function()
{
    var lat = document.getElementById("input_lat");
    lat.value = this.lat;
    var lon = document.getElementById("input_lon");
    lon.value = this.lon;
}

AjaxMap.prototype.updateMap = function()
{
    //console.log('update with value: lat '+ this.lat + ' lon ' + this.lon + ' range ' + this.range + ' search level ' + this.level + ' limit ' + this.limit);

    // clear map elements
    this.layerGroup.clearLayers();
    
    // draw new elements
    // draw center
    var marker = L.marker([this.lat, this.lon]).addTo(this.layerGroup)
        .bindPopup("<b>Center</b><br />I am the center.").openPopup();

    // draw range
    var circle = L.circle([this.lat, this.lon], this.range, {
        color: 'red',
        fillColor: '#f03',
        fillOpacity: 0.4
    }).addTo(this.layerGroup);

    // draw coordinates
    this.getCoordinates(this.lat, this.lon, this.range, this.level, this.limit);
    
    // draw bounding boxes
    this.getBoundingBoxes(this.lat, this.lon, this.range, this.level);
}

// call server, returns all coordinates
AjaxMap.prototype.getCoordinates = function(lat, lon, range, level, limit)
{
    var that = this;
    console.log('request server with value: lat '+ lat + ' lon ' + lon + ' range ' + range + ' search level ' + level + ' limit ' + limit);
    var request = new Object();
    request.lat = lat;
    request.lon = lon;
    request.range = range;
    request.level = level;
    request.limit = limit;
    //console.log(request);
    $.ajax({
        url: "https://localhost:5001/coordinates",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            console.log(result.length + " results find");
            var i;
            for (i=0; i<result.length; i++)
            {
                L.marker([result[i].lat, result[i].lon]).addTo(that.layerGroup)
            }
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });
}

// call server, returns all bounding box coordinates
AjaxMap.prototype.getBoundingBoxes = function(lat, lon, range, level)
{
    var that = this;
    console.log('request server with value: lat '+ lat + ' lon ' + lon + ' range ' + range + ' search level ' + level);
    var request = new Object();
    request.lat = lat;
    request.lon = lon;
    request.range = range;
    request.level = level;
}

function parse_json(json) {
    try {
        var data = $.parseJSON(json);
    } catch(err) {
        throw "JSON parse error: " + json;
    }

    return data;
}