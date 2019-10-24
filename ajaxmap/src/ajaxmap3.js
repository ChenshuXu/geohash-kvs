'use strict';
function AjaxMap3(mapid) {
    console.log("map3 started");

    this.lat = 41.87476071;
    this.lon = -87.67198792;
    this.level = 6;
    this.vertices;

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

    // Initialise the FeatureGroup to store editable layers
    var editableLayers = new L.FeatureGroup();
    this.map.addLayer(editableLayers);

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
        var type = e.layerType,
          layer = e.layer;
      
        if (type === 'marker') {
          layer.bindPopup('A popup!');
        }
      
        editableLayers.addLayer(layer);
        console.log(e);
      });

      this.configMap();

}

AjaxMap3.prototype.configMap = function() {
    let that = this;

    // update the value on the level input slider
    let slider = document.getElementById("map3_input_level");
    let output = document.getElementById("map3_input_level_value");
    output.innerHTML = slider.value;
    this.level = Number(slider.value); // get initial value
    slider.oninput = function() {
        output.innerHTML = slider.value;
        that.level = Number(this.value);
        //that.updateMap();
    };

    // update map when click on search button
    document.getElementById("map3_search").onclick = function(event) {
        that.step0();
    }
}

AjaxMap3.prototype.step0 = function() {
    let that = this;

    // clear map elements
    this.layerGroup.clearLayers();
    this.bboxLayerGroup.clearLayers();
    this.coordinatesLayerGroup.clearLayers();
    this.tempLayerGroup.clearLayers();
}