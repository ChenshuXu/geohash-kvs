using System;
using System.Collections.Generic;

namespace MySqlServer
{
    public class Database
    {
        internal string DatabaseName
        {
            get { return _DataBaseName; }
        }

        protected Dictionary<String, Table> _Tables = new Dictionary<string, Table>();
        private string _DataBaseName;

        public Database(string databaseName)
        {
            _DataBaseName = databaseName;
        }

        public Table GetTable(string tableName)
        {
            if (_Tables.ContainsKey(tableName))
            {
                return _Tables[tableName];
            }
            throw new Exception("table " + tableName + " not exist");
        }

        public void AddTable(Table t)
        {
            if (_Tables.ContainsKey(t.TableName))
            {
                throw new Exception("already have table " + t.TableName + " exist");
            }
            _Tables.Add(t.TableName, t);
            t.DatabaseName = _DataBaseName;
        }
    }
}
