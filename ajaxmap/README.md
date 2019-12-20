# Ajaxmap

A webpage that use for demo. The [server project](../server) is a web api project. Run as the backend of the webpage. The webpage send requests the [server project](../server), then the server respond with the search results. The webpage display the search process interactively.

一个网页项目用于演示geohash搜索坐标点的原理。 [Server project](../server) 作为网页的后端。网页向 [Server project](../server) 发送搜索请求，服务端返回搜索过程和坐标。

### How to start

1. Setup the server backend

   Use Visual Studio to open the [server project](../server). It's a web api project. Run as the backend of the webpage. open [DatabaseInterface.cs](../server/DatabaseInterface.cs),  check function AddDataset1, the path variable is correct. It should be the example data file called [Crimes_-_2019.csv](../server/Resources/Crimes_-_2019.csv) in Resources folder.

   Install the dependencies, NuGet Packages.

   Then run the server backend. It will take about 30 seconds for loading dataset1 depends on the computer speed.

   After the server backend started, successfully run will look like this:

   ![screen1](screen1.jpg)

2. To open the demo page, need to allow [Cross-Origin Resource Sharing (CORS)](https://en.wikipedia.org/wiki/Cross-origin_resource_sharing). One solution is to install an extension in your browser. For Firefox, you can install: [CORS Everywhere](https://addons.mozilla.org/en-US/firefox/addon/cors-everywhere/). For Chrome, you can install: [Allow CORS](https://chrome.google.com/webstore/detail/allow-cors-access-control/lhobafahddgcelffkeicbaginigeejlf?hl=en). Enable it.

3. Then start the webpage.

   Use [VS code](https://code.visualstudio.com/), install [Live Server](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer) extension to run the webpage locally. Other web development tools such as [WebStorm](https://www.jetbrains.com/webstorm/) also works. Currently it using CDN version of dependencies. If want to install locally, can use npm to install them. The dependencies information is included in the project folder.

   Successfully run will look like this:
   
   ![screen2](screen2.jpg)

### Lat Lon Range Search

Search coordinates in a certain circle.

Click 'Start' button can start search with default values. The circle is defined with latitude, longitude and range(radius). Information are shown on the right side. You can click the table to change the view of the map. It will zoom in to the bounding box that you clicked.

Then scroll down. There are more buttons to click.

![screen3](screen3.jpg)

### Bounding box search

Search coordinates in a certain bounding box.

The bounding box is defined with current viewing area.

![screen4](screen4.jpg)

### Polygon Search (Combined Version)

It combines all three type of search.

Polygon search is searching coordinates in a certain polygon shape.

Start a search by click on the upper right icons then click on the map to define vertices.

![screen5](screen5.jpg)

### Reference

- [Leaflet](https://github.com/Leaflet/Leaflet)
- [Leaflet.draw](https://github.com/Leaflet/Leaflet.draw)
- [Mapbox](https://www.mapbox.com/)
- [jQuery](https://jquery.com/)

