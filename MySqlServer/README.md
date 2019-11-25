# MySqlServer
Simulates real MySQL server by responding the cilent with MySql client/server protocol.

Test server with command line in terminal:
`mysql -h 127.0.0.1 -u root -pbG43JPmBrY92` or `mysql --password=bG43JPmBrY92 --user=root --host=127.0.0.1`
By default it will use ssl connection.

Try `mysql> SELECT * FROM dummy;`will returns
`+------+------+\n
| Col1 | Col2 |\n
+------+------+\n
|    1 | ok   |\n
|    2 | A    |\n
+------+------+\n
2 rows in set, 28416 warnings (0.00 sec)`
