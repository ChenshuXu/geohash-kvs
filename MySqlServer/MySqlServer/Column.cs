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
        public ClientMetadata.ColumnType _ColumnType = ClientMetadata.ColumnType.MYSQL_TYPE_VAR_STRING;

        public Column(string colName = null, ClientMetadata.ColumnType type = ClientMetadata.ColumnType.MYSQL_TYPE_VAR_STRING)
        {
            _ColumnName = colName;
            _ColumnType = type;
        }
    }
}
