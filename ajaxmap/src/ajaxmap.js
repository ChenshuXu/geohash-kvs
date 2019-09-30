function AjaxMap(sel) {
    console.log('map started');

    // update the value on the level input slider
    var slider = document.getElementById("input_level");
    var output = document.getElementById("input_level_value");
    output.innerHTML = slider.value;
    slider.oninput = function() {
        output.innerHTML = this.value;
    }

    document.getElementById("submit").onclick = function (event) {
        event.preventDefault();
        // get input
        var lat = document.getElementById("input_lat");
        var lon = document.getElementById("input_lon");
        var range = document.getElementById("input_range");
        var limit = document.getElementById("input_limit");
        var message = document.getElementById("message");

        // invalid input check
        if (lat.value === "" || Number(lat.value) === NaN)
        {
            console.log("error");
            return;
        }
    }


    // map
    var mymap = L.map('mapid').setView([41.87476071, -87.67198792], 12);

    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw', {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, ' +
            '<a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
            'Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
        id: 'mapbox.streets'
    }).addTo(mymap);

    L.marker([41.87476071, -87.67198792]).addTo(mymap)
        .bindPopup("<b>Center</b><br />I am the center.").openPopup();

    L.circle([41.87476071, -87.67198792], 5000, {
        color: 'red',
        fillColor: '#f03',
        fillOpacity: 0.5
    }).addTo(mymap).bindPopup("I am a circle.");

    var popup = L.popup();

    function onMapClick(e) {
        popup
            .setLatLng(e.latlng)
            .setContent("You clicked the map at " + e.latlng.toString())
            .openOn(mymap);
    }

    mymap.on('click', onMapClick);
}