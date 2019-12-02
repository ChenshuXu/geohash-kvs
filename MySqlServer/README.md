# MySqlServer
Simulates real MySQL server by responding the cilent with MySql client/server protocol. Reference MySQL server version 5.7.28

### Basic connection

Support multi client connections at the same time.

Connect to server with default root user in terminal:

```shell
shell> mysql -h 127.0.0.1 -u root -pbG43JPmBrY92
```
or
```shell
shell> mysql --user=root --password=bG43JPmBrY92 --host=127.0.0.1
```
Without SSL: 

```shell
shell> mysql --user=root --password=bG43JPmBrY92 --host=127.0.0.1 --ssl-mode=DISABLED
```

### SELECT statement

There is a default table.

```mysql
mysql> SELECT * FROM dummy;
```
will return
```
+------+------+
| Col1 | Col2 |
+------+------+
|    1 | ok   |
|    2 | A    |
+------+------+
2 rows in set, 28416 warnings (0.00 sec)
```

### LOAD DATA statement

Test data import program, mysqlimport function

Invoke [**mysqlimport**](https://dev.mysql.com/doc/refman/8.0/en/mysqlimport.html) like this: 

```shell
shell> mysqlimport --password=bG43JPmBrY92 --user=root --host=127.0.0.1 --ssl-mode=DISABLED dummy Collations.csv
```



```mysql
mysql> LOAD DATA LOCAL INFILE 'imptest.txt' INTO TABLE imptest FIELDS TERMINATED BY ','  LINES STARTING BY '\n';
```

## Certificate Instructions

Create certificate with OpenSSL: [https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html](https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html)

This is a self-signed certificate and should NOT be used in production.

Convert to p12 file: 

```shell
shell> openssl pkcs12 -export -out server-cert.p12 -in server-cert.pem -inkey server-key.pem
```

Export password is `pswd`
