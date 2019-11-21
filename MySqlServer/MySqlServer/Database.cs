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
            return null;
        }

        public void AddTable(Table t)
        {
            _Tables.Add(t.TableName, t);
            t.DatabaseName = _DataBaseName;
        }
    }
}
