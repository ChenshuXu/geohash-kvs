# geohash-kvs
Multilevel GeoHashing in Key-Value Stores

### [Ajaxmap](./ajaxmap)

A web front end that use for demo. It request data from server and display the search process on web page interactively

一个网页项目用于演示geohash数据库搜索坐标点的原理

### [Server](./server)

A simple web api project. The server side of Ajaxmap

一个简易的web api，用于Ajaxmap网页的后端

### [geohash](./geohash)

Geohash library

Geohash数据库本身

### [MySqlServer](./MySqlServer)

Custom build server for simulating real MySQL server. Can handle all connections from all kinds of MySQL connector. It uses MySQL Client/Server Protocol to make connections and handles MySQL querys to respond data from geohash database.

这个项目模拟了一个真实的MySql数据库服务端，使用MySQL Client/Server Protocol完成大部分MySql服务器的功能。

使用场景：在用户的代码中，原本是用MySql数据库储存坐标信息数据的，一般会用 [MySql connector](https://dev.mysql.com/doc/refman/5.7/en/connectors-apis.html) 连接数据库和查询数据。如果用户想使用geohash数据库，就可以在不改变代码的情况下继续使用原本查询MySql数据库的方法查询geohash数据库，因为这个项目使用MySQL Client/Server Protocol，能模拟MySql服务器与连接器连接。用户只需要改变query statement就行了。