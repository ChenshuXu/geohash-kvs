using System;
using System.Collections.Generic;
using TSQL.Tokens;

namespace MySqlServer
{
    public class InsertParser : Parser
    {
        public string table_name;
        public string[] column_names;
        public string[][] row_values;

        public InsertParser(List<TSQLToken> tokens)
        {
            PopAndCheck(ref tokens, "insert");
            PopAndCheck(ref tokens, "into");
            table_name = PopFirstToken(ref tokens);
            PopAndCheck(ref tokens, "(");

            List<string> columns = new List<string>();
            while (GetFirstToken(tokens) != ")")
            {
                columns.Add(PopFirstToken(ref tokens));
                PopIfExist(ref tokens, ",");
            }
            PopAndCheck(ref tokens, ")");
            column_names = columns.ToArray();
            PopAndCheck(ref tokens, "values");

            List<string[]> rows = new List<string[]>();
            while (GetFirstToken(tokens) == "(")
            {
                PopAndCheck(ref tokens, "(");
                List<string> values = new List<string>();
                while (GetFirstToken(tokens) != ")")
                {
                    values.Add(PopFirstToken(ref tokens));
                    PopIfExist(ref tokens, ",");
                }
                PopAndCheck(ref tokens, ")");
                rows.Add(values.ToArray());
            }
            row_values = rows.ToArray();
        }
    }
}
