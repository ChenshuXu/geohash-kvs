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
        that.level = this.value;
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

    this.getCoordinates(this.lat, this.lon, this.range, this.level, this.limit);
    // draw coordinates

    // draw bounding boxes

}

// call server, returns all coordinates
AjaxMap.prototype.getCoordinates = function(lat, lon, range, level, limit)
{
    console.log('request server with value: lat '+ lat + ' lon ' + lon + ' range ' + range + ' search level ' + level + ' limit ' + limit);
    $.ajax({
        url: "https://localhost:5001/coordinates",
        method: "POST",
        contentType: "application/json",
        dataType: 'application/json',
        data: {"lat":lat, "lon":lon, "range":range, "level":level, "limit":limit},
        success: function(data) {
            console.log(data[0].lat);
            var i;
            for (i=0; i<data.length; i++)
            {
                L.marker([data[i].lat, data[i].lon]).addTo(this.layerGroup)
            }
        },
        error: function() {
            console.log("error get");
        }
    });
    // var data = parse_json(coordinatesJson());
    // console.log(data[0].lat);
    // var i;
    // for (i=0; i<data.length; i++)
    // {
    //     L.marker([data[i].lat, data[i].lon]).addTo(this.layerGroup)
    // }
}

// call server, returns all bounding box coordinates
AjaxMap.prototype.getBoundingBoxes = function(lat, lon, range, level)
{

}

function coordinatesJson() {
    var json = "[{\"lat\":41.874112944,\"lon\":-87.670865933},{\"lat\":41.874112944,\"lon\":-87.670865933},{\"lat\":41.874133594,\"lon\":-87.670164416},{\"lat\":41.874133594,\"lon\":-87.670164416},{\"lat\":41.874133594,\"lon\":-87.670164416},{\"lat\":41.874077392,\"lon\":-87.674225948},{\"lat\":41.874077392,\"lon\":-87.674225948},{\"lat\":41.875046407,\"lon\":-87.674755747},{\"lat\":41.8763156,\"lon\":-87.669271624},{\"lat\":41.8763156,\"lon\":-87.669271624},{\"lat\":41.877534837,\"lon\":-87.672262652},{\"lat\":41.877532811,\"lon\":-87.672387519},{\"lat\":41.877532811,\"lon\":-87.672387519},{\"lat\":41.874173691,\"lon\":-87.668082118},{\"lat\":41.874173691,\"lon\":-87.668082118},{\"lat\":41.87417635,\"lon\":-87.668067404},{\"lat\":41.875155116,\"lon\":-87.667906709},{\"lat\":41.875155116,\"lon\":-87.667906709},{\"lat\":41.875155116,\"lon\":-87.667906709},{\"lat\":41.875155116,\"lon\":-87.667906709},{\"lat\":41.872552039,\"lon\":-87.66906458},{\"lat\":41.874180626,\"lon\":-87.66785807},{\"lat\":41.877267351,\"lon\":-87.66918466},{\"lat\":41.873219626,\"lon\":-87.675868666},{\"lat\":41.875286899,\"lon\":-87.67649739},{\"lat\":41.875286899,\"lon\":-87.67649739},{\"lat\":41.876351148,\"lon\":-87.666880865},{\"lat\":41.877161446,\"lon\":-87.676536946},{\"lat\":41.877605262,\"lon\":-87.667826213},{\"lat\":41.877609409,\"lon\":-87.667594837},{\"lat\":41.877609409,\"lon\":-87.667594837},{\"lat\":41.872268386,\"lon\":-87.677005609},{\"lat\":41.878053463,\"lon\":-87.67656086},{\"lat\":41.877039418,\"lon\":-87.6777536},{\"lat\":41.871617946,\"lon\":-87.66698878},{\"lat\":41.87102983,\"lon\":-87.676368411},{\"lat\":41.87102983,\"lon\":-87.676368411},{\"lat\":41.879180745,\"lon\":-87.668823348},{\"lat\":41.879390552,\"lon\":-87.668564131},{\"lat\":41.879390552,\"lon\":-87.668564131},{\"lat\":41.880081938,\"lon\":-87.672339218},{\"lat\":41.879693672,\"lon\":-87.66925501},{\"lat\":41.877956153,\"lon\":-87.67778095},{\"lat\":41.877470199,\"lon\":-87.678222877},{\"lat\":41.877470199,\"lon\":-87.678222877},{\"lat\":41.877470199,\"lon\":-87.678222877},{\"lat\":41.876799235,\"lon\":-87.678945751},{\"lat\":41.869201889,\"lon\":-87.670589881},{\"lat\":41.869201889,\"lon\":-87.670589881},{\"lat\":41.873670974,\"lon\":-87.664213664},{\"lat\":41.878726718,\"lon\":-87.678162308},{\"lat\":41.871649797,\"lon\":-87.664913992},{\"lat\":41.878724477,\"lon\":-87.678250459},{\"lat\":41.872811862,\"lon\":-87.664185908},{\"lat\":41.872332627,\"lon\":-87.679608149},{\"lat\":41.878278759,\"lon\":-87.67901144},{\"lat\":41.872243709,\"lon\":-87.664169804},{\"lat\":41.872197038,\"lon\":-87.664166619},{\"lat\":41.878415984,\"lon\":-87.679013713},{\"lat\":41.878925296,\"lon\":-87.664999747},{\"lat\":41.871370856,\"lon\":-87.664138521},{\"lat\":41.881348623,\"lon\":-87.674525788},{\"lat\":41.881348623,\"lon\":-87.674525788},{\"lat\":41.881348623,\"lon\":-87.674525788},{\"lat\":41.868655689,\"lon\":-87.676304606},{\"lat\":41.873333547,\"lon\":-87.662821937},{\"lat\":41.873333547,\"lon\":-87.662821937},{\"lat\":41.873333547,\"lon\":-87.662821937},{\"lat\":41.879987448,\"lon\":-87.678358738},{\"lat\":41.872508522,\"lon\":-87.662992112},{\"lat\":41.872512852,\"lon\":-87.662793798},{\"lat\":41.880604741,\"lon\":-87.677849354},{\"lat\":41.874938665,\"lon\":-87.681847091},{\"lat\":41.881441785,\"lon\":-87.667837814},{\"lat\":41.881441785,\"lon\":-87.667837814},{\"lat\":41.881441785,\"lon\":-87.667837814},{\"lat\":41.876991717,\"lon\":-87.681426001},{\"lat\":41.877244215,\"lon\":-87.681430778},{\"lat\":41.877244215,\"lon\":-87.681430778},{\"lat\":41.870157672,\"lon\":-87.664099773},{\"lat\":41.870097302,\"lon\":-87.664100403},{\"lat\":41.881307619,\"lon\":-87.676949835},{\"lat\":41.881307619,\"lon\":-87.676949835},{\"lat\":41.880195761,\"lon\":-87.664979164},{\"lat\":41.86928729,\"lon\":-87.664964304},{\"lat\":41.869289458,\"lon\":-87.664865152},{\"lat\":41.869070769,\"lon\":-87.678829991},{\"lat\":41.869291069,\"lon\":-87.664670547},{\"lat\":41.869066892,\"lon\":-87.679112733},{\"lat\":41.871057701,\"lon\":-87.681258662},{\"lat\":41.873948802,\"lon\":-87.682473996},{\"lat\":41.868071816,\"lon\":-87.666419826},{\"lat\":41.869054315,\"lon\":-87.664070896},{\"lat\":41.882146688,\"lon\":-87.666827987},{\"lat\":41.869313358,\"lon\":-87.663315542},{\"lat\":41.883213802,\"lon\":-87.672457502},{\"lat\":41.883209751,\"lon\":-87.672707258},{\"lat\":41.881476477,\"lon\":-87.66483362},{\"lat\":41.881224307,\"lon\":-87.66441395},{\"lat\":41.882215582,\"lon\":-87.677843907}]";
    return json;
}

function parse_json(json) {
    try {
        var data = $.parseJSON(json);
    } catch(err) {
        throw "JSON parse error: " + json;
    }

    return data;
}