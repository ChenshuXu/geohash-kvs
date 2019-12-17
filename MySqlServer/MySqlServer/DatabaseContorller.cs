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

            dummyTable.InsertRows(new Row[]
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
            throw new Exception("Database name not exist");
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
            throw new Exception("user name "+ userName +" not exist");
        }

        public Table Set(List<TSQLToken> tokens)
        {
            Table virtualTable = new Table("");

            return virtualTable;
        }

        public void Log(string msg)
        {
            if (Debug) Console.WriteLine(msg);
        }
    }
}
