using System;
using System.Collections.Generic;
using TSQL;
using TSQL.Statements;
using TSQL.Tokens;

namespace MySqlServer
{
    public class DatabaseController
    {
        internal Database InformationSchema
        {
            get { return _InformationSchema; }
        }
        private InformationSchemaDatabase _InformationSchema;
        private Dictionary<String, Database> _Databases = new Dictionary<string, Database>();
        private Dictionary<String, User> _Users = new Dictionary<string, User>();
        private string _DefaultDatabaseName = "dummy";

        public bool Debug = false;

        public DatabaseController()
        {
            // build root user
            _Users.Add("root", new User("root", "bG43JPmBrY92"));

            // information schema database
            _InformationSchema = new InformationSchemaDatabase();

            // TODO: dummy database with dummy table
            Database dummyDB = new Database("dummy");
            AddDatabase(dummyDB);
            Table dummyTable = new Table("dummy");
            dummyDB.AddTable(dummyTable);
            dummyTable.AddColumns(new Column[]
            {
                new Column("Col1", ClientSession.ColumnType.MYSQL_TYPE_LONGLONG),
                new Column("Col2")
            });

            dummyTable.AddRows(new Row[]
            {
                new Row( new Object[] { 1, "ok"} ),
                new Row( new Object[] { 2, "A"} )
            });
        }

        public Database GetDatabase(string databaseName)
        {
            if (_Databases.ContainsKey(databaseName))
            {
                return _Databases[databaseName];
            }
            else
            {
                throw new Exception("Database name not exist");
            }
        }

        public void AddDatabase(Database db)
        {
            _Databases.Add(db.DatabaseName, db);
        }

        public string GetUserPassword(string userName)
        {
            if (_Users.ContainsKey(userName))
            {
                return _Users[userName].Password;
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
            Log("SELECT:");

            // handle select TIMEDIFF
            if (tokens[0].Text == "TIMEDIFF")
            {
                Log("TIMEDIFF:");
                return HandleTimediff(tokens);
            }

            bool readingVariable = false;
            List<Column> outPutColumns = new List<Column>();

            while (true)
            {
                Column qualifiedColumnName = GetQualifiedColumnName(ref tokens);
                outPutColumns.Add(qualifiedColumnName);
                Log("\ttable name: {"+ qualifiedColumnName.TableName + "}, column name: {"+ qualifiedColumnName.ColumnName + "}");
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
            Table fromTable = _InformationSchema.GetTable("information schema"); // default is info schema
            // when reading system variable, don't have from table clause
            if (!readingVariable)
            {
                PopAndCheck(ref tokens, "from");
                Log("FROM:");
                Table tableNameObj = GetQualifiedTableName(ref tokens);
                // TODO: from database
                fromTable = GetDatabase(tableNameObj.DatabaseName).GetTable(tableNameObj.TableName); // TODO: throw error when table name not exist
                Log("\tdb name: {"+ fromTable.DatabaseName + "}, table name: {"+ fromTable.TableName + "}");
                // TODO: throw error when have join keyword,
                // only support from one table,
                // only support table from dummy database


            }

            // TODO: remaining clause
            Log("remaining tokens:");
            foreach (var token in tokens)
            {
                Log("\ttype: " + token.Type.ToString() + ", value: " + token.Text);
            }

            Log("QUERY after processed");
            foreach (var col in outPutColumns)
            {
                Log("\ttable name: {"+ col.TableName + "}, column name: {"+ col.ColumnName + "}");
            }
            Log("\tfrom table info, db name: {"+ fromTable.DatabaseName + "}, table name: {"+ fromTable.TableName + "}");


            // TODO: add column information to virtual table
            return fromTable.SelectRows(outPutColumns.ToArray());
        }

        /// <summary>
        /// Handle show query
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>table object that contains query result, ready to send to client</returns>
        public Table Show(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table("");
            PopAndCheck(ref tokens, "show");

            if (tokens[0].Text.ToLower() == "collation")
            {
                return _InformationSchema.GetTable("COLLATIONS");
            }
            return virtualTable;
        }

        public Table Set(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table("");

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
                return new Table(actualColName.Text, databaseName);
            }
            return new Table(possibleColName.Text, _DefaultDatabaseName);
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
                return new Column
                {
                    ColumnName = actualColName.Text,
                    TableName = tableName,
                    _TokenType = possibleColName.Type
                };
            }
            return new Column
            {
                ColumnName = possibleColName.Text,
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
            Table virtualTable = new Table("");
            virtualTable.AddColumn(
                new Column("TIMEDIFF(NOW(), UTC_TIMESTAMP())", ClientSession.ColumnType.MYSQL_TYPE_TIME)
            );
            virtualTable.AddRow(
                new Row(
                    new Object[] { "08:00:00" }
                )
            );

            return virtualTable;
        }

        public void Log(string msg)
        {
            if (Debug) Console.WriteLine(msg);
        }
    }
}
