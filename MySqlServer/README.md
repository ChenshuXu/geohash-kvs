# MySqlServer
Simulates real MySQL server by responding the cilent with MySql client/server protocol.

Test server with command line in terminal:
`mysql -h 127.0.0.1 -u root -pbG43JPmBrY92` 
or 
`mysql --password=bG43JPmBrY92 --user=root --host=127.0.0.1`
By default it will use ssl connection.

Try `mysql> SELECT * FROM dummy;` will returns
`+------+------+`
`| Col1 | Col2 |`
`+------+------+`
`|    1 | ok   |`
`|    2 | A    |`
`+------+------+`
`2 rows in set, 28416 warnings (0.00 sec)`



### Certificate Instructions

Create certificate with OpenSSL: [https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html](https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html)

This is a self-signed certificate and should NOT be used in production.

Convert to p12 file: `openssl pkcs12 -export -out server-cert.p12 -in server-cert.pem -inkey server-key.pem`

Export password is `pswd`