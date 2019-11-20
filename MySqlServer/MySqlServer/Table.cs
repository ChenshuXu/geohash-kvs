using System;
using System.Collections.Generic;

namespace MySqlServer
{
    public class Table
    {
        public string _TableName;
        public string _DatabaseName;
        public List<Column> _Columns = new List<Column>();
        public List<Row> _Rows = new List<Row>();

        internal Column[] Columns
        {
            get { return _Columns.ToArray(); }
        }

        internal Row[] Rows
        {
            get { return _Rows.ToArray(); }
        }

        public Table(string name = "")
        {
            _TableName = name;
        }

        public void AddColumn(Column col)
        {
            _Columns.Add(col);
            col._TableName = _TableName;
        }

        public void AddColumns(Column[] cols)
        {
            foreach (var col in cols)
            {
                AddColumn(col);
            }
        }

        public void AddRow(Row row)
        {
            _Rows.Add(row);
        }

        public void AddRows(Row[] rows)
        {
            foreach (var row in rows)
            {
                AddRow(row);
            }
        }

        public Table SelectRows(Column[] output_columns)
        {
            Column[] expanded_output_columns = ExpandStarColumn(output_columns);
            CheckColumnsExist(expanded_output_columns);
            EnsureFullyQualified(ref expanded_output_columns);
            int[] real_column_index = GetRealColumnIndex(expanded_output_columns);

            // build up virtual table as result table
            Table virtualTable = new Table("virtual");
            virtualTable.AddColumns(GenerateColumns(real_column_index));
            virtualTable.AddRows(GenerateRows(real_column_index));

            return virtualTable;
        }

        private Column[] ExpandStarColumn(Column[] output_columns)
        {
            List<Column> new_output_columns = new List<Column>();
            foreach (Column col in output_columns)
            {
                if (col._ColumnName == "*")
                {
                    new_output_columns.AddRange(_Columns);
                }
                else
                {
                    new_output_columns.Add(col);
                }
            }
            return new_output_columns.ToArray();
        }

        private void CheckColumnsExist(Column[] columns)
        {
            List<string> existColNames = new List<string>();
            foreach (var col in _Columns)
            {
                existColNames.Add(col._ColumnName);
            }

            foreach (var col in columns)
            {
                if (!existColNames.Contains(col._ColumnName))
                {
                    throw new Exception("column name {" + col._ColumnName + "}not exist");
                }
            }
        }

        private void EnsureFullyQualified(ref Column[] columns)
        {
            foreach (Column col in columns)
            {
                if (col._TableName == null)
                {
                    col._TableName = _TableName;
                }
            }
        }

        private int[] GetRealColumnIndex(Column[] outputColumns)
        {
            List<int> realColumnIndex = new List<int>();
            foreach (Column col in outputColumns)
            {
                for (int i=0; i<_Columns.Count; i++)
                {
                    if (_Columns[i]._ColumnName == col._ColumnName)
                    {
                        realColumnIndex.Add(i);
                    }
                }
            }
            return realColumnIndex.ToArray();
        }

        private Column[] GenerateColumns(int[] columnIndex)
        {
            List<Column> newCols = new List<Column>();
            for (int i = 0; i < columnIndex.Length; i++)
            {
                int index = columnIndex[i];
                newCols.Add(_Columns[index]);
            }
            
            return newCols.ToArray();
        }

        private Row[] GenerateRows(int[] columnIndex)
        {
            List<Row> newRows = new List<Row>();
            foreach (Row r in _Rows)
            {
                Object[] rowValue = new Object[columnIndex.Length];
                for(int i = 0; i < columnIndex.Length; i++)
                {
                    int index = columnIndex[i];
                    Object value = r._Values[index];
                    rowValue[i] = value;
                }

                Row newRow = new Row(rowValue);
                newRows.Add(newRow);
            }
            return newRows.ToArray();
        }
    }
}
