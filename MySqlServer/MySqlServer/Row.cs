using System;
namespace MySqlServer
{
    public class Row
    {
        public object[] _Values;

        public Row(object[] values)
        {
            _Values = values;
        }
    }
}
