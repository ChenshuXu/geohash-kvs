using System;
using System.Collections.Generic;

namespace MySqlServer
{
    public class Database
    {
        private Dictionary<String, Table> _Tables = new Dictionary<string, Table>();
        private Dictionary<String, User> _Users = new Dictionary<string, User>();

        public Database()
        {
            _Users.Add("root", new User("root", "bG43JPmBrY92"));
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
    }
}
