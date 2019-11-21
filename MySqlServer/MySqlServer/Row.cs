using System;
namespace MySqlServer
{
    public class Row
    {
        public Object[] _Values;

        public Row(Object[] values)
        {
            _Values = values;
        }
    }
}
