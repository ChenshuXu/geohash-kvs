'use strict';
const TYPE = {
    polygon : 1,
    rectangle : 2,
    circle : 3
};
function AjaxMap3(mapid) {
    console.log("map3 started");

    this.lat = 41.87476071;
    this.lon = -87.67198792;
    this.level = 6;
    this.searchType;
    
    // setup map
    this.map = L.map(mapid).setView([this.lat, this.lon], 15);
	// layer group for bbox
	this.bboxLayerGroup = L.layerGroup().addTo(this.map);
	// layger group for coordinates
	this.coordinatesLayerGroup = L.layerGroup().addTo(this.map);
	// layer group for out of range coordinates
	this.tempLayerGroup = L.layerGroup().addTo(this.map);

	// Initialise the FeatureGroup to store editable layers
	let editableLayers = new L.FeatureGroup();
	this.editableLayers = editableLayers;
	this.map.addLayer(editableLayers);

    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw', {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, ' +
            '<a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
            'Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
        id: 'mapbox.streets'
    }).addTo(this.map);

	this.configMap();

}

AjaxMap3.prototype.configMap = function() {
	let that = this;
	document.getElementById("map_info").innerHTML = `<h2>Click icon to draw on map</h2>`;
	let editableLayers = this.editableLayers;
    var drawPluginOptions = {
		position: 'topright',
		draw: {
			polygon: {
				allowIntersection: false, // Restricts shapes to simple polygons
				drawError: {
					color: '#e1e100', // Color the shape will turn when intersects
					message: '<strong>Oh snap!<strong> you can\'t draw that!' // Message that will show when intersect
				},
				shapeOptions: {
					color: '#97009c'
				}
			},
			// disable toolbar item by setting it to false
			polyline: false,
			circle: true, // Turns off this drawing tool
			rectangle: true,
			marker: false,
			circlemarker: false
		},
		edit: {
			featureGroup: editableLayers, //REQUIRED!!
			remove: true
		}
	};

	// Initialise the draw control and pass it the FeatureGroup of editable layers
	var drawControl = new L.Control.Draw(drawPluginOptions);
	this.map.addControl(drawControl);
	this.map.on('draw:created', function(e) {
		let layer = e.layer;
		let type = e.layerType;
		that.editableLayers.addLayer(layer);
		console.log(e);
		if (type === "polygon") {
			that.starPolygonSearch(e.layer._latlngs[0]);
		} else if (type === "rectangle") {
			that.startRectangleSearch(e.layer._bounds);
		} else if (type === "circle") {
			that.startCircleSearch(e.layer._latlng, e.layer._mRadius)
		}
	});

	this.map.on('draw:drawstart', function(e) {
		that.drawstart();
	});
}

AjaxMap3.prototype.drawstart = function() {
    let that = this;
    console.log("draw start");
    // clear map elements
    this.bboxLayerGroup.clearLayers();
    this.coordinatesLayerGroup.clearLayers();
	this.tempLayerGroup.clearLayers();
	this.editableLayers.clearLayers();
	document.getElementById("map_info").innerHTML = `<h2>Drawing ...</h2>`;
}

AjaxMap3.prototype.starPolygonSearch = function(vertices) {
    this.searchType = TYPE.polygon;
	let that = this;
	document.getElementById("map_info").innerHTML =`<h2>
	Polygon search
</h2>
<p>
	search level: <input type="range" id="input_level" min="1" max="9" value="6">
	Value: <span id="input_level_value"></span>
</p>
<p>
    Polygon vertices: ${vertices}
</p>
<input id="search" type="submit" value="Start search">
<div id="message"></div>
<div id="bbox_info"></div>`;

	// update the value on the level input slider
	let slider = document.getElementById("input_level");
    let output = document.getElementById("input_level_value");
    output.innerHTML = slider.value;
    slider.oninput = function() {
        output.innerHTML = this.value;
        that.level = Number(this.value);
	};
	
	document.getElementById("search").onclick = function(event) {
		//$(this).remove();
		// clear map elements
		that.bboxLayerGroup.clearLayers();
		that.coordinatesLayerGroup.clearLayers();
		that.tempLayerGroup.clearLayers();
		document.getElementById("bbox_info").innerHTML = "";
        that.PolygonSearchBboxes(vertices);
    }
}

AjaxMap3.prototype.PolygonSearchBboxes = function(vertices) {
	console.log("search bboxes");
	let that = this;
	// setup request
	let polygon = [];
	for (let i=0; i<vertices.length; i++)
	{
		let v = vertices[i];
		let p = { Lat: v.lat, Lon: v.lng };
		polygon[i] = p;
	}
	this.polygon = polygon;
	let request = {};
	request.Level = this.level;
	request.Vertices = polygon;
	console.log(request);
	// draw bounding boxes
	$.ajax({
        url: "https://localhost:5001/PolygonSearchBoxes",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            that.drawBboxes(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
	});
	
	let html = `<div id="bbox_info_table"></div>
<p>Second, get coordinates in each box</p>
<p>Query database with hash of each box</p>
<input id="next2" type="submit" value="Show coordinates">`;
    document.getElementById("bbox_info").innerHTML = html;

    document.getElementById("next2").onclick = function()
    {
        $(this).remove();
        that.PolygonSearchCoordinates(vertices);
    }
}

AjaxMap3.prototype.PolygonSearchCoordinates = function(vertices) {
	console.log("search coordinates");
	let that = this;
	// setup request
	let polygon = [];
	for (let i=0; i<vertices.length; i++)
	{
		let v = vertices[i];
		let p = { Lat: v.lat, Lon: v.lng };
		polygon[i] = p;
	}
	let request = {};
	request.Level = this.level;
	request.Vertices = polygon;
	console.log(request);
	// draw coordinates
    $.ajax({
        url: "https://localhost:5001/PolygonSearchCoordinates",
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

	let html = `<div id="coordinates_info">
<div id="coordinates_info_table"></div>
</div>`;
    $("#map_info").append(html);
}

AjaxMap3.prototype.startRectangleSearch = function(bounds) {
    this.searchType = TYPE.rectangle;
	console.log("rectangle search");
	console.log(bounds);
	let that = this;
	document.getElementById("map_info").innerHTML = `<h2>
	Bounding box search
</h2>
<p>
	search level: <input type="range" id="input_level" min="1" max="9" value="6">
	Value: <span id="input_level_value"></span>
</p>
<p>
	NorthEast: ${bounds._northEast}
</p>
<p>
	SouthWest: ${bounds._southWest}
</p>
<input id="search" type="submit" value="Start search">
<div id="message"></div>
<div id="bbox_info"></div>`;
	
	// update the value on the level input slider
	let slider = document.getElementById("input_level");
    let output = document.getElementById("input_level_value");
    output.innerHTML = slider.value;
    slider.oninput = function() {
        output.innerHTML = this.value;
        that.level = Number(this.value);
    };
    
    document.getElementById("search").onclick = function(event) {
		//$(this).remove();
		// clear map elements
		that.bboxLayerGroup.clearLayers();
		that.coordinatesLayerGroup.clearLayers();
		that.tempLayerGroup.clearLayers();
		document.getElementById("bbox_info").innerHTML = "";
        that.RectangleSearchBboxes(bounds);
    }

}

AjaxMap3.prototype.RectangleSearchBboxes = function(bounds) {
    console.log("search bboxes");
    let that = this;
    let request = {};
    request.Maxlat = bounds.getNorthEast().lat;
    request.Maxlon = bounds.getNorthEast().lng;
    request.Minlat = bounds.getSouthWest().lat;
    request.Minlon = bounds.getSouthWest().lng;
    request.Level = this.level;

    $.ajax({
        url: "https://localhost:5001/BoxSearchBboxes",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            that.drawBboxes(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });

    let html = `<div id="bbox_info_table"></div>
<p>Second, get coordinates in each box</p>
<p>Query database with hash of each box</p>
<input id="next2" type="submit" value="Show coordinates">`;
document.getElementById("bbox_info").innerHTML = html;

    document.getElementById("next2").onclick = function()
    {
        $(this).remove();
        that.RectangleSearchCoordinates(bounds);
    }
}

AjaxMap3.prototype.RectangleSearchCoordinates = function(bounds) {
    console.log("search coordinates");
    let that = this;
    let request = {};
    request.Maxlat = bounds.getNorthEast().lat;
    request.Maxlon = bounds.getNorthEast().lng;
    request.Minlat = bounds.getSouthWest().lat;
    request.Minlon = bounds.getSouthWest().lng;
    request.Level = this.level;
    console.log(request);

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

    let html = `<div id="coordinates_info">
<div id="coordinates_info_table"></div>
</div>`;
    $("#map_info").append(html);
}

AjaxMap3.prototype.startCircleSearch = function(center, radius) {
    this.searchType = TYPE.circle;
	let that = this;
	document.getElementById("map_info").innerHTML = `<h2>
	Lat Lon Range search
</h2>
<p>
	search level: <input type="range" id="input_level" min="1" max="8" value="">
	Value: <span id="input_level_value"></span>
</p>
<p>
	Lat: <input type='text' id='input_lat' value="${center.lat}">
	Lon: <input type='text' id="input_lon" value="${center.lng}">
</p>
<p>
	range(meter): <input type="number" id="input_range" value="${radius}">
</p>
<p>
	limit: <input type="number" id="input_limit" value="0">
</p>
<input id="search" type="submit" value="Start search">
<div id="message"></div>
<div id="bbox_info"></div>`;

	// update the value on the level input slider
	let slider = document.getElementById("input_level");
    let output = document.getElementById("input_level_value");
    output.innerHTML = slider.value;
    slider.oninput = function() {
        output.innerHTML = this.value;
        that.level = Number(this.value);
    };

    let limitObj = document.getElementById("input_limit");
    document.getElementById("search").onclick = function(event) {
		//$(this).remove();
		// clear map elements
		that.bboxLayerGroup.clearLayers();
		that.coordinatesLayerGroup.clearLayers();
		that.tempLayerGroup.clearLayers();
        document.getElementById("bbox_info").innerHTML = "";
        if (limitObj.value === "" || !Number.isInteger(Number(limitObj.value))) {
            console.log("error limit");
            limitObj.value = "0";
        } else { 
            let limit = Number(limitObj.value);
            that.CircleSearchBboxes(center, radius, limit);
        }
    }
}

AjaxMap3.prototype.CircleSearchBboxes = function(center, radius, limit) {
    console.log("search bboxes");
    let that = this;
    
    let request = {};
    request.Lat = center.lat;
    request.Lon = center.lng;
    request.Range = radius;
    request.Level = this.level;
    request.Limit = limit;

    // draw bounding boxes
    $.ajax({
        url: "https://localhost:5001/CircleSearchBboxes",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(result) {
            that.drawBboxes(result);
        },
        error: function(jqxhr, status, exception) {
            console.log("error get "+ jqxhr + status + exception);
        }
    });

    let html = `<div id="bbox_info_table"></div>
<p>Second, get coordinates in each box</p>
<p>Query database with hash of each box</p>
<input id="next2" type="submit" value="Show coordinates">`;
    document.getElementById("bbox_info").innerHTML = html;

    document.getElementById("next2").onclick = function()
    {
        $(this).remove();
        that.CircleSearchCoordinates(center, radius, limit);
    }
}

AjaxMap3.prototype.CircleSearchCoordinates = function(center, radius, limit) {
    console.log("search coordinates");
    let that = this;
    
    let request = {};
    request.Lat = center.lat;
    request.Lon = center.lng;
    request.Range = radius;
    request.Level = this.level;
    request.Limit = limit;

    // draw coordinates
    $.ajax({
        url: "https://localhost:5001/CircleSearchCoordinates",
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

	let html = `<div id="coordinates_info">
<div id="coordinates_info_table"></div>
</div>`;
    $("#map_info").append(html);
}

AjaxMap3.prototype.drawBboxes = function(boxes) {
	console.log("draw bbox")
	console.log(boxes);
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
    document.getElementById("bbox_info_table").appendChild(table);
}

AjaxMap3.prototype.onClickBox = function(bounds) {
    this.map.fitBounds(bounds);
    this.map.panTo(bounds.getCenter());
}

// takes in an array of coordinates json
AjaxMap3.prototype.drawCoordinates = function(coordinates) {
    let that = this;
    console.log(coordinates.length + " coordinates returned");
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
        let marker = L.marker([c.lat, c.lon]).addTo(that.coordinatesLayerGroup)
            .bindPopup(c.id+','+c.locationDescription+','+c.description)
            .on("click", function(ev) {
                //that.onClickMarker(c);
            });

        row.insertCell().innerHTML = c.lat+","+c.lon;
        row.insertCell().innerHTML = c.locationDescription;
        row.insertCell().innerHTML = c.description;
        row.onclick = function(event) {
            //console.log(event);
            //that.onClickMarker(c);
            marker.openPopup();
        };
        row = body.insertRow();
    }
    document.getElementById("coordinates_info_table").appendChild(table);
}

AjaxMap3.prototype.onClickMarker = function(c) {
    this.tempLayerGroup.clearLayers();
    var that = this;
    //console.log(c);
    let requestURL = "https://localhost:5001/";
	let request = {};
	request.Select = { Lat: c.lat, Lon: c.lon };
    request.Level = this.level;
    if (this.searchType === TYPE.polygon) {
        request.Vertices = this.polygon;
        requestURL += "PolygonSearchProcess";
    } else if (this.searchType === TYPE.rectangle) {
        request.SearchMaxLat = this.maxCoor.lat;
        request.SearchMaxLon = this.maxCoor.lng;
        request.SearchMinLat = this.minCoor.lat;
        request.SearchMinLon = this.minCoor.lng;
        requestURL += "DisplayBoundingBoxSearchProcess";
    } else if (this.searchType === TYPE.circle) {
        request.SearchLat = this.lat;
        request.SearchLon = this.lon;
        request.Range = this.range;
        requestURL += "DisplayBoundingCircleSearchProcess";
    }
	console.log(request);

    $.ajax({
        url: requestURL,
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(request),
        success: function(data) {
            let result = JSON.parse(data);
            console.log(result.CoordinatesInRange.length + " in range");
            let coordinatesInRange = result.CoordinatesInRange;
            console.log(result.CoordinatesOutOfRange.length + " out of range");
            let coordinatesOutOfRange = result.CoordinatesOutOfRange; //console.log("select hash " + result.Boxhash);
            let hash = result.Boxhash;
            coordinatesOutOfRange.forEach(element => {
                L.marker([element.Lat, element.Lon], {opacity:0.5}).addTo(that.tempLayerGroup);
            });
            document.getElementById("message").innerHTML = `<p>
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