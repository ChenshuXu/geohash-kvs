import socketserver
import sys
from mysql_wire_framework.server import FrameworkServer
from commonconf.backends import use_configparser_backend


use_configparser_backend("./demo/server.cfg", "server")

host = "localhost"
port = 3306

server = socketserver.TCPServer((host, port), FrameworkServer)
server.serve_forever()
