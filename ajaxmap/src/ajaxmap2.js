function AjaxMap2(mapid)
{
    console.log('map2 started');

    this.lat = 41.87476071;
    this.lon = -87.67198792;
    this.level = 6;

    this.maxCoor = new Object();
    this.minCoor = new Object();

    // setup second map
    this.map = L.map(mapid).setView([this.lat, this.lon], 16);
    this.layerGroup = L.layerGroup().addTo(this.map);
    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw', {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, ' +
            '<a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
            'Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
        id: 'mapbox.streets'
    }).addTo(this.map);

    this.configMap();
    this.updateMap();
}

AjaxMap2.prototype.configMap = function()
{
    var that = this;
    
    // popup
    var popup = L.popup();
    function onMapClick(e) {
        popup
            .setLatLng(e.latlng)
            .setContent("You clicked the map at " + e.latlng.toString())
            .openOn(that.map);
    }
    this.map.on('click', onMapClick);

    // update the value on the level input slider
    var slider = document.getElementById("map2_input_level");
    var output = document.getElementById("map2_input_level_value");
    output.innerHTML = slider.value;
    this.level = Number(slider.value); // get initial value
    slider.oninput = function() {
        output.innerHTML = slider.value;
        that.level = Number(this.value);
        that.updateMap();
    };

    // update map when move ends
    function onMapMove(e) {
        that.updateMap();
    }
    this.map.on("moveend", onMapMove);
}

AjaxMap2.prototype.updateMap = function ()
{
    var that = this;

    // clear previous map elements
    this.layerGroup.clearLayers();

    var bounds = this.map.getBounds();
    this.minCoor = bounds.getSouthWest();
    this.maxCoor = bounds.getNorthEast();

    // update numbers on webpage
    document.getElementById("map2_lat_max").innerHTML = this.maxCoor.lat;
    document.getElementById("map2_lon_max").innerHTML = this.maxCoor.lng;
    document.getElementById("map2_lat_min").innerHTML = this.minCoor.lat;
    document.getElementById("map2_lon_min").innerHTML = this.minCoor.lng;

    var request = new Object();
    request.maxlat = this.maxCoor.lat;
    request.maxlon = this.maxCoor.lng;
    request.minlat = this.minCoor.lat;
    request.minlon = this.minCoor.lng;
    request.level = this.level;
    console.log('box request server: '+ JSON.stringify(request));
    $.ajax({
        url: "https://localhost:5001/BboxCoordinates",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            console.log(result.length + " coordinates returned");
            document.getElementById("map2_coordinates_count").innerHTML = result.length + " coordinates in view";
            that.drawCoordinates(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });

    $.ajax({
        url: "https://localhost:5001/BboxBoxes",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            console.log(result.length + " boxes returned");
            that.drawBoundingBoxes(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });
}

AjaxMap2.prototype.drawCoordinates = function(coordinates)
{
    var i;
    for (i=0; i<coordinates.length; i++)
    {
        L.marker([coordinates[i].lat, coordinates[i].lon]).addTo(this.layerGroup)
    }
}

// takes in an array of bounding box json
AjaxMap2.prototype.drawBoundingBoxes = function(boxes)
{
    var i;
    for (i=0; i<boxes.length; i++)
    {
        var max = boxes[i].maximum;
        var min = boxes[i].minimum;
        var bounds = [[min.lat, min.lon],[max.lat, max.lon]];
        L.rectangle(bounds, {color: "#ff7800", weight: 1}).addTo(this.layerGroup);
    }
}

function parse_json(json) {
    try {
        var data = $.parseJSON(json);
    } catch(err) {
        throw "JSON parse error: " + json;
    }

    return data;
}