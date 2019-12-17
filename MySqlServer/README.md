# MySqlServer
Simulates real MySQL server by responding the cilent with MySql client/server protocol. Reference MySQL server version 5.7.28

## Basic connection

Support multi client connections at the same time

Support [The MySQL Command-Line Client](https://dev.mysql.com/doc/refman/5.7/en/mysql.html)

Support some MySQL Connectors [Connector/NET](https://dev.mysql.com/doc/connector-net/en/)

Connect to server with default root user in terminal 

with SSL:

```shell
shell> mysql -h 127.0.0.1 -u root -pbG43JPmBrY92
```
or
```shell
shell> mysql --user=root --password=bG43JPmBrY92 --host=127.0.0.1
```
without SSL: 

```shell
shell> mysql --user=root --password=bG43JPmBrY92 --host=127.0.0.1 --ssl-mode=DISABLED
```

## Certificate Instructions

Create certificate with OpenSSL: [https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html](https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html)

This is a self-signed certificate and should NOT be used in production.

Convert to p12 file: 

```shell
shell> openssl pkcs12 -export -out server-cert.p12 -in server-cert.pem -inkey server-key.pem
```

Export password is `pswd`

## Supported SQL Statements

#### SELECT statement

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

#### INSERT statement

```mysql
mysql> INSERT INTO dummy (Col1, Col2) VALUES(3, ccc)(4, ddd);
```

#### LOAD DATA statement

Test data import program, mysqlimport function

Invoke [**mysqlimport**](https://dev.mysql.com/doc/refman/5.7/en/mysqlimport.html) like this: 

```shell
shell> mysqlimport --password=bG43JPmBrY92 --user=root --host=127.0.0.1 --ssl-mode=DISABLED dummy Collations.csv
```

```mysql
LOAD DATA
    [LOCAL]
    INFILE 'file_name'
    INTO TABLE tbl_name
    [{FIELDS | COLUMNS}
        [TERMINATED BY 'string']
        [[OPTIONALLY] ENCLOSED BY 'char']
        [ESCAPED BY 'char']
    ]
    [LINES
        [STARTING BY 'string']
        [TERMINATED BY 'string']
    ]
```

If you specify no `FIELDS` or `LINES` clause, the defaults are the same as if you had written this:

```mysql
FIELDS TERMINATED BY '\t' ENCLOSED BY '' ESCAPED BY '\\'
LINES TERMINATED BY '\n' STARTING BY ''
```

Example:

```mysql
mysql> LOAD DATA LOCAL INFILE 'imptest.txt' INTO TABLE imptest FIELDS TERMINATED BY ','  LINES TERMINATED BY '\n';
```

#### Using mysql in Batch Mode

Support batch query, executing SQL Statements from a Text File

Reference: [batch-mode](https://dev.mysql.com/doc/refman/5.7/en/batch-mode.html), [mysql-batch-commands](https://dev.mysql.com/doc/refman/5.7/en/mysql-batch-commands.html)

```shell
shell> mysql mysql -h 127.0.0.1 -u root -pbG43JPmBrY92 < batch-file.sql
```



#### DUMP, mysqldump — A Database Backup Program



