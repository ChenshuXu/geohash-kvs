# MySqlServer
This project simulates real MySQL server by responding the cilent with MySql client/server protocol. Reference MySQL server version 5.7.28.

这个项目模拟了真实的MySql服务器，版本5.7.28

It can handle listed statements and connections from different MySql clients.

MySql connector能和这个服务端连接，运行一些[查询语句](#Supported-SQL-Statements)，具体看下面。

## What's this project for?

User can use MySql connectors and MySql statements to get data from our server

用户能用各种MySql连接器和这个服务器连接。这个服务器能给MySql连接器正确的返回数据。

比如，用户的代码中，原本是用MySql数据库储存坐标信息数据的，他一般会用如下的代码连接数据库和查询数据：

Example code in user side:

```c#
using MySql.Data;
using MySql.Data.MySqlClient;

using (MySqlConnection conn = new MySqlConnection(connStr))
{
	try
  {
    // Connecting to MySQL
    conn.Open();
    // connected
    string sql = "SELECT * FROM dummy;";
    MySqlCommand cmd = new MySqlCommand(sql, conn);
    MySqlDataReader rdr = cmd.ExecuteReader();

    while (rdr.Read())
    {
      Console.WriteLine(rdr[0] + " -- " + rdr[1]);
    }
    rdr.Close();
  }
  catch (Exception ex)
  {
    // handle error
  }
  conn.Close();
}
```

This project can also handle this connections and return data to the user.

## Supported connectors

- Support [The MySQL Command-Line Client](https://dev.mysql.com/doc/refman/5.7/en/mysql.html). Example is [here](#mysql--the-mysql-command-line-client)
- Support [MySQL Connector/NET](https://dev.mysql.com/doc/connector-net/en/) used in C#. Example code is in test projects in the solution.
- Haven't test with other [connectors](https://dev.mysql.com/doc/refman/5.7/en/connectors-apis.html).


## Certificate Instructions

If want to use SSL connections, must have a certificate. I have included the certificates in the solution.

To create certificate with OpenSSL: [https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html](https://dev.mysql.com/doc/refman/8.0/en/creating-ssl-files-using-openssl.html)

This is a self-signed certificate and should NOT be used in production.

After creating, convert to p12 file: 

```shell
shell> openssl pkcs12 -export -out server-cert.p12 -in server-cert.pem -inkey server-key.pem
```

Export password as `pswd`

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
shell> mysql --user=root --password=bG43JPmBrY92 --host=127.0.0.1 --database=test --ssl-mode=DISABLED < batch-file.sql
```

If you are already running [**mysql**](https://dev.mysql.com/doc/refman/5.7/en/mysql.html), you can execute an SQL script file using the `source` command or `\.` command:

```mysql
mysql> source file_name
mysql> \. file_name
```



## Supported [Client Programs](https://dev.mysql.com/doc/refman/5.7/en/programs-client.html)

#### [mysql — The MySQL Command-Line Client](https://dev.mysql.com/doc/refman/5.7/en/mysql.html)

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

#### [mysqlimport — A Data Import Program](https://dev.mysql.com/doc/refman/5.7/en/mysqlimport.html)

TODO

#### [mysqldump — A Database Backup Program](https://dev.mysql.com/doc/refman/5.7/en/mysqldump.html)

TODO



## Basic logic

TODO

## Unsolved

Handle: 

```mysql
select @@version_comment limit 1
```

- 定义查询geohash数据库的query statement
- 和本项目连接到geohash数据库