using System;
namespace MySqlServer
{
    public class Row
    {
        // row calues
        public Object[] _Values;

        public Row(Object[] values)
        {
            _Values = values;
            //_Values = new Column[values.Length];
            //for (var i=0; i<values.Length; i++)
            //{
            //    var value = values[i];
            //    Column c = new Column();
            //    c._Type = value.GetType();
            //    c._Value = value;
            //    _Values[i] = c;
            //}
        }
    }
}
