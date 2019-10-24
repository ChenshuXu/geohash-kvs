'use strict';
function AjaxMap2(mapid) {
    console.log('map2 started');

    this.lat = 41.87476071;
    this.lon = -87.67198792;
    this.level = 6;

    this.maxCoor = new Object();
    this.minCoor = new Object();

    // setup second map
    this.map = L.map(mapid).setView([this.lat, this.lon], 16);
    this.layerGroup = L.layerGroup().addTo(this.map);
    this.bboxLayerGroup = L.layerGroup().addTo(this.map); // layer group for bbox
    this.coordinatesLayerGroup = L.layerGroup().addTo(this.map); // layger group for coordinates
    this.tempLayerGroup = L.layerGroup().addTo(this.map);
    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw', {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, ' +
            '<a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
            'Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
        id: 'mapbox.streets'
    }).addTo(this.map);

    this.configMap();
}

AjaxMap2.prototype.configMap = function() {
    let that = this;

    // update the value on the level input slider
    let slider = document.getElementById("map2_input_level");
    let output = document.getElementById("map2_input_level_value");
    output.innerHTML = slider.value;
    this.level = Number(slider.value); // get initial value
    slider.oninput = function() {
        output.innerHTML = slider.value;
        that.level = Number(this.value);
        //that.updateMap();
    };

    // update map when move ends
    function onMapMove(e) {
        //that.updateMap();
    }
    this.map.on("moveend", onMapMove);

    // update map when click on search button
    document.getElementById("map2_search").onclick = function(event) {
        that.step0();
    }
}

AjaxMap2.prototype.updateMap = function () {
    let that = this;

    // clear previous map elements
    this.layerGroup.clearLayers();

    let bounds = this.map.getBounds();
    this.minCoor = bounds.getSouthWest();
    this.maxCoor = bounds.getNorthEast();

    // update numbers on webpage
    document.getElementById("map2_lat_max").innerHTML = this.maxCoor.lat;
    document.getElementById("map2_lon_max").innerHTML = this.maxCoor.lng;
    document.getElementById("map2_lat_min").innerHTML = this.minCoor.lat;
    document.getElementById("map2_lon_min").innerHTML = this.minCoor.lng;
    document.getElementById("map2_input_level").value = this.level;
    document.getElementById("map2_input_level_value").innerHTML = this.level;

    let request = new Object();
    request.Maxlat = this.maxCoor.lat;
    request.Maxlon = this.maxCoor.lng;
    request.Minlat = this.minCoor.lat;
    request.Minlon = this.minCoor.lng;
    request.Level = this.level;
    console.log('box request server: '+ JSON.stringify(request));
    $.ajax({
        url: "https://localhost:5001/BoxSearchCoordinates",
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
        url: "https://localhost:5001/BoxSearchBboxes",
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

// step0 draw search bounding box
AjaxMap2.prototype.step0 = function() {
    let that = this;
    let bounds = this.map.getBounds();
    this.minCoor = bounds.getSouthWest();
    this.maxCoor = bounds.getNorthEast();
    // update numbers on webpage
    document.getElementById("map2_lat_max").innerHTML = this.maxCoor.lat;
    document.getElementById("map2_lon_max").innerHTML = this.maxCoor.lng;
    document.getElementById("map2_lat_min").innerHTML = this.minCoor.lat;
    document.getElementById("map2_lon_min").innerHTML = this.minCoor.lng;
    document.getElementById("map2_input_level").value = this.level;
    document.getElementById("map2_input_level_value").innerHTML = this.level;
    // clear map elements
    this.layerGroup.clearLayers();
    this.bboxLayerGroup.clearLayers();
    this.coordinatesLayerGroup.clearLayers();
    this.tempLayerGroup.clearLayers();

    // draw rectangle
    L.rectangle(bounds, {
        color: "red",
        fillColor: "#f03",
        fillOpacity: 0.1
    }).addTo(this.layerGroup);

    let html = `<p>First, get bounding boxes covered by the view area</p>
<p>Calculate bounding boxes with {"Maxlat": ${this.maxCoor.lat},"Maxlon": ${this.maxCoor.lng},"Minlat": ${this.minCoor.lat},"Minlon": ${this.minCoor.lng},"Level": ${this.level}</p>
<input id="map2_next1" type="submit" value="Show bounding boxes">`;
    document.getElementById("map2_info").innerHTML = html;
    document.getElementById("map2_next1").onclick = function()
    {
        $(this).remove();
        that.step1();
    }
}

AjaxMap2.prototype.step1 = function() {
    let that = this;

    let request = {};
    request.Maxlat = this.maxCoor.lat;
    request.Maxlon = this.maxCoor.lng;
    request.Minlat = this.minCoor.lat;
    request.Minlon = this.minCoor.lng;
    request.Level = this.level;

    $.ajax({
        url: "https://localhost:5001/BoxSearchBboxes",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            that.drawBoundingBoxes(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });

    let html = `<div id="map2_bbox_info"></div>
<p>Second, get coordinates in each box</p>
<p>Query database with hash of each box</p>
<input id="map2_next2" type="submit" value="Show coordinates">`;
    $("#map2_info").append(html);

    document.getElementById("map2_next2").onclick = function()
    {
        $(this).remove();
        that.step2();
    }
}

AjaxMap2.prototype.step2 = function() {
    let that = this;
    let request = {};
    request.Maxlat = this.maxCoor.lat;
    request.Maxlon = this.maxCoor.lng;
    request.Minlat = this.minCoor.lat;
    request.Minlon = this.minCoor.lng;
    request.Level = this.level;

    // draw coordinates
    $.ajax({
        url: "https://localhost:5001/BoxSearchCoordinates",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            that.drawCoordinates(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });

    let html = `<div id="map2_coordinates_info"></div>`;
    $("#map2_info").append(html);
}

AjaxMap2.prototype.drawCoordinates = function(coordinates) {
    let that = this;
    console.log(coordinates.length + " coordinates returned");
    document.getElementById("map2_coordinates_count").innerHTML = coordinates.length + " coordinates in view";
    let table = document.createElement("table");
    let header = table.createTHead();
    let headerRow = header.insertRow();
    headerRow.insertCell().innerHTML = "Coordinates";
    headerRow.insertCell().innerHTML = "Location Description";
    headerRow.insertCell().innerHTML = "Description";
    let body = table.createTBody();
    let row = body.insertRow();

    for (let i=0; i<coordinates.length; i++)
    {
        let c = coordinates[i];
        let marker = L.marker([c.lat, c.lon]).addTo(this.layerGroup)
            .bindPopup(c.id+','+c.locationDescription+','+c.description)
            .on("click", function (ev) {
                that.onClickMarker(c);
            });

        row.insertCell().innerHTML = c.lat+","+c.lon;
        row.insertCell().innerHTML = c.locationDescription;
        row.insertCell().innerHTML = c.description;
        row.onclick = function(event) {
            //console.log(event);
            that.onClickMarker(c);
            marker.openPopup();
        };
        row = body.insertRow();
    }
    document.getElementById("map2_coordinates_info").appendChild(table);
}

AjaxMap2.prototype.onClickMarker = function(c) {
    this.tempLayerGroup.clearLayers();
    var that = this;
    //console.log(c);
    let request = {};
    request.SelectLat = c.lat;
    request.SelectLon = c.lon;
    request.SearchMaxLat = this.maxCoor.lat;
    request.SearchMaxLon = this.maxCoor.lng;
    request.SearchMinLat = this.minCoor.lat;
    request.SearchMinLon = this.minCoor.lng;
    request.Level = this.level;

    $.ajax({
        url: "https://localhost:5001/DisplayBoundingBoxSearchProcess",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(data) {
            let result = JSON.parse(data);
            //console.log(result.CoordinatesInRange.length + " in range");
            let coordinatesInRange = result.CoordinatesInRange;
            //console.log(result.CoordinatesOutOfRange.length + " out of range");
            let coordinatesOutOfRange = result.CoordinatesOutOfRange; //console.log("select hash " + result.Boxhash);
            let hash = result.Boxhash;
            coordinatesOutOfRange.forEach(element => {
                L.marker([element.Lat, element.Lon], {opacity:0.5}).addTo(that.tempLayerGroup);
            });
            document.getElementById("map2_message").innerHTML = `<p>
selected marker in box ${hash}, ${coordinatesInRange.length} coordinates are in side, ${coordinatesOutOfRange.length} coordinates are out side
</p>
`;
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });
    this.map.panTo([c.lat, c.lon]);
}

// takes in an array of bounding box json
AjaxMap2.prototype.drawBoundingBoxes = function(boxes) {
    console.log(boxes.length + " boxes returned");
    let that = this;
    let table = document.createElement("table");
    let header = table.createTHead();
    let headerRow = header.insertRow();
    headerRow.insertCell().innerHTML = "BoxHash";
    headerRow.insertCell().innerHTML = "Max";
    headerRow.insertCell().innerHTML = "Min";
    let body = table.createTBody();
    let row = body.insertRow();

    for (let i=0; i<boxes.length; i++)
    {
        let box = boxes[i];
        let max = box.maximum;
        let min = box.minimum;
        let hash = box.hash;
        let bounds = L.latLngBounds(L.latLng(min.lat, min.lon),L.latLng(max.lat, max.lon));

        L.rectangle(bounds, {color: "#ff8e2c", weight: 2})
            .bindTooltip(hash,{permanent:true, direction:"center", opacity:0.6})
            .openTooltip()
            .on('click', function(ev){
                that.onClickBox(bounds);
            })
            .addTo(this.bboxLayerGroup);

        row.insertCell().innerHTML = hash;
        row.insertCell().innerHTML = `${max.lat},${max.lon}`;
        row.insertCell().innerHTML = `${min.lat},${min.lon}`;
        row.onclick = function(event) {
            //console.log(event);
            that.onClickBox(bounds);
        };
        row = body.insertRow();
    }
    document.getElementById("map2_bbox_info").appendChild(table);
}

AjaxMap2.prototype.onClickBox = function(bounds) {
    this.map.fitBounds(bounds);
    this.map.panTo(bounds.getCenter());
}
