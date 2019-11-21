using System;
using System.Collections.Generic;

namespace MySqlServer
{
    public class Table
    {
        internal string TableName
        {
            get { return _TableName; }
        }
        
        internal string DatabaseName
        {
            get { return _DatabaseName; }
            set { _DatabaseName = value; }
        }

        private string _TableName;
        private string _DatabaseName;
        private List<Column> _Columns = new List<Column>();
        private List<Row> _Rows = new List<Row>();

        internal Column[] Columns
        {
            get { return _Columns.ToArray(); }
        }

        internal Row[] Rows
        {
            get { return _Rows.ToArray(); }
        }

        public Table(string tableName, string dbName=null)
        {
            _TableName = tableName;
            _DatabaseName = dbName;
        }

        public void AddColumn(Column col)
        {
            _Columns.Add(col);
            col.TableName = _TableName;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output_columns"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Convert * to full column name
        /// </summary>
        /// <param name="output_columns"></param>
        /// <returns>column objects</returns>
        private Column[] ExpandStarColumn(Column[] output_columns)
        {
            List<Column> new_output_columns = new List<Column>();
            foreach (Column col in output_columns)
            {
                if (col.ColumnName == "*")
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

        /// <summary>
        /// Check the existance of column names in this table, theow exception when not exist
        /// </summary>
        /// <param name="columns"></param>
        private void CheckColumnsExist(Column[] columns)
        {
            // collect all column names in this table
            List<string> existColNames = new List<string>();
            foreach (var col in _Columns)
            {
                existColNames.Add(col.ColumnName);
            }

            foreach (var col in columns)
            {
                if (!existColNames.Contains(col.ColumnName))
                {
                    throw new Exception("column name {" + col.ColumnName + "}not exist");
                }
            }
        }

        /// <summary>
        /// Add table name to columns if it's empty
        /// </summary>
        /// <param name="columns"></param>
        private void EnsureFullyQualified(ref Column[] columns)
        {
            foreach (Column col in columns)
            {
                if (col.TableName == null)
                {
                    col.TableName = _TableName;
                }
            }
        }

        /// <summary>
        /// Convert column names to index
        /// </summary>
        /// <param name="outputColumns"></param>
        /// <returns>Index of corresponding column</returns>
        private int[] GetRealColumnIndex(Column[] outputColumns)
        {
            List<int> realColumnIndex = new List<int>();
            foreach (Column col in outputColumns)
            {
                for (int i=0; i<_Columns.Count; i++)
                {
                    if (_Columns[i].ColumnName == col.ColumnName)
                    {
                        realColumnIndex.Add(i);
                    }
                }
            }
            return realColumnIndex.ToArray();
        }

        /// <summary>
        /// Convert index to real columns in this table
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get values in corresponding index in each row
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
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
