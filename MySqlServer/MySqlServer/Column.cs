using System;
using TSQL.Tokens;

namespace MySqlServer
{
    public class Column
    {
        public string _TableName = null;
        public string _ColumnName = null;
        public TSQLTokenType _TokenType;
        public MySqlServer.ColumnType _ColumnType = MySqlServer.ColumnType.MYSQL_TYPE_VAR_STRING;

        public Column(string colName = null, MySqlServer.ColumnType type = MySqlServer.ColumnType.MYSQL_TYPE_VAR_STRING)
        {
            _ColumnName = colName;
            _ColumnType = type;
        }
    }
}
