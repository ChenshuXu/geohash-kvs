using System;
using System.Collections.Generic;
using TSQL;
using TSQL.Statements;
using TSQL.Tokens;

namespace MySqlServer
{
    public class Database
    {
        public Table _InformationSchema;
        public Table _Collation;
        private Dictionary<String, Table> _Tables = new Dictionary<string, Table>();
        private Dictionary<String, User> _Users = new Dictionary<string, User>();
        public Table _VirtualTable;
        public string _DataBaseName = "default database";

        public Database()
        {
            // build root user
            _Users.Add("root", new User("root", "bG43JPmBrY92"));

            // build information schema
            _InformationSchema = new Table("information schema");
            _InformationSchema.AddColumns(new Column[]
            {
                new Column("@@max_allowed_packet", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG),
                new Column("@@character_set_client"),
                new Column("@@character_set_connection"),
                new Column("@@license"),
                new Column("@@sql_mode"),
                new Column("@@lower_case_table_names"),
                new Column("@@version_comment")
            });

            _InformationSchema.AddRows(new Row[]
            {
                new Row( new Object[]
                {
                    4194304,
                    "utf8",
                    "utf8",
                    "GPL",
                    "ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION",
                    "2",
                    "MySQL Community Server (GPL)"
                })
            });

            // build collation table
            _Collation = new Table("COLLATIONS");
            _Collation._DatabaseName = "information_schema";
            _Collation.AddColumns(new Column[]
            {
                new Column("Collation"),
                new Column("Charset"),
                new Column("Id", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG),
                new Column("Default"),
                new Column("Compiled"),
                new Column("Sortlen", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG)
            });
            _Collation.AddRows(new Row[]
            {
                new Row( new Object[]
                {
                    "latin1_bin",
                    "latin1",
                    47,
                    "",
                    "Yes",
                    1
                })
            });

            // dummy table
            Table dummyTable = new Table("dummy");
            dummyTable._Columns = new List<Column>
            {
                new Column("Col1", MySqlServer.ColumnType.MYSQL_TYPE_LONGLONG),
                new Column("Col2")
            };

            dummyTable._Rows = new List<Row>
            {
                new Row( new Object[] { 1, "ok"} ),
                new Row( new Object[] { 2, "A"} )
            };

            _Tables.Add("dummy", dummyTable);

        }

        public Table GetTable(string tableName)
        {
            if (_Tables.ContainsKey(tableName))
            {
                return _Tables[tableName];
            }
            return null;
        }

        public User GetUser(string userName)
        {
            if (_Users.ContainsKey(userName))
            {
                return _Users[userName];
            }
            return null;
        }

        /// <summary>
        /// Handle select query
        /// </summary>
        /// <param name="tokens">query tokens</param>
        /// <returns>a table object that contains result column information and row information</returns>
        public Table Select(List<TSQLToken> tokens)
        {
            // select clause
            // get output columns
            PopAndCheck(ref tokens, "select");
            Console.WriteLine("SELECT:");

            // handle select TIMEDIFF
            if (tokens[0].Text == "TIMEDIFF")
            {
                Console.WriteLine("TIMEDIFF:");
                return HandleTimediff(tokens);
            }

            bool readingVariable = false;
            List<Column> outPutColumns = new List<Column>();

            while (true)
            {
                Column qualifiedColumnName = GetQualifiedColumnName(ref tokens);
                outPutColumns.Add(qualifiedColumnName);

                Console.WriteLine("\ttable name: {0}, column name: {1}", qualifiedColumnName._TableName, qualifiedColumnName._ColumnName);
                
                // Handle veriable tokens
                if (qualifiedColumnName._TokenType == TSQLTokenType.Variable)
                {
                    readingVariable = true;
                }

                if (tokens.Count == 0)
                {
                    break;
                }

                TSQLToken nextToken = tokens[0];
                if (nextToken.Text.ToLower() == "from" || nextToken.Text.ToLower() == "limit")
                {
                    break;
                }

                if (!readingVariable && nextToken.Text != ",")
                {
                    throw new Exception("should be ,");
                }
                // check ,
                if (nextToken.Text == ",")
                {
                    tokens.RemoveAt(0);
                    continue;
                }
            }

            // from clause
            // get from table name
            Table fromTable = _InformationSchema; // default is info schema
            // when reading system variable, don't have from table clause
            if (!readingVariable)
            {
                PopAndCheck(ref tokens, "from");
                Console.WriteLine("FROM:");
                Table tableNameObj = GetQualifiedTableName(ref tokens);
                fromTable = GetTable(tableNameObj._TableName); // TODO: throw error when table name not exist

                // TODO: throw error when have join keyword,
                // only support from one table,
                // only support table from this database

                Console.WriteLine("\tdb name: {0}, table name: {1}", tableNameObj._DatabaseName, tableNameObj._TableName);
            }

            // TODO: remaining clause
            Console.WriteLine("remaining tokens:");
            foreach (var token in tokens)
            {
                Console.WriteLine("\ttype: " + token.Type.ToString() + ", value: " + token.Text);
            }

            
            // TODO: add column information to virtual table
            return fromTable.SelectRows(outPutColumns.ToArray());
        }

        public Table Show(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table();
            PopAndCheck(ref tokens, "show");

            if (tokens[0].Text.ToLower() == "collation")
            {
                return _Collation;
            }
            return virtualTable;
        }

        public Table Set(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table();

            return virtualTable;
        }

        /// <summary>
        /// Read tokens, get the table name, handle . character
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>table object that contains table name and database name</returns>
        private Table GetQualifiedTableName(ref List<TSQLToken> tokens)
        {
            TSQLToken possibleColName = tokens[0];
            tokens.RemoveAt(0);

            if (tokens.Count != 0 && tokens[0].Text == ".")
            {
                tokens.RemoveAt(0);
                TSQLToken actualColName = tokens[0];
                tokens.RemoveAt(0);
                string databaseName = possibleColName.Text;
                return new Table
                {
                    _TableName = actualColName.Text,
                    _DatabaseName = databaseName
                };
            }
            return new Table
            {
                _TableName = possibleColName.Text,
                _DatabaseName = _DataBaseName
            };
        }

        /// <summary>
        /// Read tokens, get the first column name, handle . character
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>column object that contains table name and column name</returns>
        private Column GetQualifiedColumnName(ref List<TSQLToken> tokens)
        {
            TSQLToken possibleColName = tokens[0];
            tokens.RemoveAt(0);

            if (tokens.Count != 0 && tokens[0].Text == ".")
            {
                tokens.RemoveAt(0);
                TSQLToken actualColName = tokens[0];
                tokens.RemoveAt(0);
                string tableName = possibleColName.Text;
                return new Column {
                    _ColumnName = actualColName.Text,
                    _TableName = tableName,
                    _TokenType = possibleColName.Type
                };
            }
            return new Column {
                _ColumnName = possibleColName.Text,
                _TokenType = possibleColName.Type
            };
        }

        /// <summary>
        /// Remove first item and check if it is same as keyword
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="keyword"></param>
        private void PopAndCheck(ref List<TSQLToken> tokens, string keyword)
        {
            TSQLToken first = tokens[0];
            tokens.RemoveAt(0);
            if (first.Text.ToLower() != keyword)
            {
                throw new Exception(string.Format("{0}!={1}", first.Text, keyword));
            }
        }

        /// <summary>
        /// Handle query with TIMEDIFF funtion
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>a table object that contains diff information</returns>
        private Table HandleTimediff(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table();
            virtualTable.AddColumn(
                new Column("TIMEDIFF(NOW(), UTC_TIMESTAMP())", MySqlServer.ColumnType.MYSQL_TYPE_TIME)
            );
            virtualTable.AddRow(
                new Row (
                    new Object[] { "08:00:00" }
                )
            );

            return virtualTable;
        }
    }
}
