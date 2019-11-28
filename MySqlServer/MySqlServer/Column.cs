using System;
using TSQL.Tokens;

namespace MySqlServer
{
    public class Column
    {
        internal string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }

        internal string ColumnName
        {
            get { return _ColumnName; }
            set { _ColumnName = value; }
        }
        private string _TableName = null;
        private string _ColumnName = null;
        public TSQLTokenType _TokenType;
        public ClientSession.ColumnType _ColumnType = ClientSession.ColumnType.MYSQL_TYPE_VAR_STRING;

        public Column(string colName = null, ClientSession.ColumnType type = ClientSession.ColumnType.MYSQL_TYPE_VAR_STRING)
        {
            _ColumnName = colName;
            _ColumnType = type;
        }
    }
}
