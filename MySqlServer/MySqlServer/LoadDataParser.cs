using System;
using System.Collections.Generic;
using TSQL.Tokens;

namespace MySqlServer
{
    public class LoadDataParser : Parser
    {
        public string file_name = "";
        public string table_name = "";

        // fields
        public string fields_terminated_by = "\t";
        public string fields_enclosed_by = "";
        public bool fields_optionally_enclosed_by = false;
        public string fields_escaped_by = "\\";

        // lines
        public string lines_starting_by = "";
        public string lines_terminated_by = "\n";

        public LoadDataParser()
        {

        }

        public LoadDataParser(List<TSQLToken> tokens)
        {
            PopAndCheck(ref tokens, "load");
            PopAndCheck(ref tokens, "data");
            if (tokens[0].Text.ToLower() == "local")
            {
                PopAndCheck(ref tokens, "local");
            }
            PopAndCheck(ref tokens, "infile");
            string fileName = tokens[0].Text;
            file_name = fileName[1..^1];
            Log(file_name);
            tokens.RemoveAt(0);

            PopAndCheck(ref tokens, "into");
            PopAndCheck(ref tokens, "table");

            table_name = tokens[0].Text;
            Log(table_name);
            tokens.RemoveAt(0);

            // can handle different orders of the keywords
            string first = GetFirst(tokens).ToLower();
            while (first == "fields" || first == "columns" || first == "lines")
            {
                if (first == "fields" || first == "columns")
                {
                    tokens.RemoveAt(0);
                    string first_subclause = GetFirst(tokens).ToLower();
                    if (first_subclause != "terminated" && first_subclause != "enclosed" && first_subclause != "escaped" && first_subclause != "optionally")
                    {
                        throw new Exception("no subclauses after " + first);
                    }

                    while (first_subclause == "terminated" || first_subclause == "enclosed" || first_subclause == "escaped" || first_subclause == "optionally")
                    {
                        switch (first_subclause)
                        {
                            case "terminated":
                                PopAndCheck(ref tokens, "terminated");
                                PopAndCheck(ref tokens, "by");
                                fields_terminated_by = tokens[0].Text[1..^1];
                                break;
                            case "enclosed":
                                PopAndCheck(ref tokens, "enclosed");
                                PopAndCheck(ref tokens, "by");
                                fields_enclosed_by = tokens[0].Text[1..^1];
                                // the FIELDS[OPTIONALLY] ENCLOSED BY and FIELDS ESCAPED BY values must be a single character.
                                if (fields_escaped_by.Length > 1)
                                {
                                    throw new Exception("the FIELDS ENCLOSED BY {" + fields_enclosed_by + "} value must be a single character");
                                }
                                break;
                            case "optionally":
                                PopAndCheck(ref tokens, "optionally");
                                PopAndCheck(ref tokens, "enclosed");
                                PopAndCheck(ref tokens, "by");
                                fields_optionally_enclosed_by = true;
                                fields_enclosed_by = tokens[0].Text[1..^1];
                                break;
                            case "escaped":
                                PopAndCheck(ref tokens, "escaped");
                                PopAndCheck(ref tokens, "by");
                                fields_escaped_by = tokens[0].Text[1..^1];
                                // the FIELDS[OPTIONALLY] ENCLOSED BY and FIELDS ESCAPED BY values must be a single character.
                                if (fields_escaped_by.Length > 1)
                                {
                                    throw new Exception("the FIELDS ESCAPED BY {" + fields_escaped_by + "} value must be a single character");
                                }
                                break;
                        }
                        tokens.RemoveAt(0);
                        first_subclause = GetFirst(tokens).ToLower();
                    }
                }
                else if (first == "lines")
                {
                    PopAndCheck(ref tokens, "lines");
                    string first_subclause = GetFirst(tokens).ToLower();
                    if (first_subclause != "starting" && first_subclause != "terminated")
                    {
                        throw new Exception("no subclauses after " + first);
                    }

                    while (first_subclause == "starting" || first_subclause == "terminated")
                    {
                        switch (first_subclause)
                        {
                            case "starting":
                                PopAndCheck(ref tokens, "starting");
                                PopAndCheck(ref tokens, "by");

                                lines_starting_by = tokens[0].Text[1..^1];
                                tokens.RemoveAt(0);
                                break;
                            case "terminated":
                                PopAndCheck(ref tokens, "terminated");
                                PopAndCheck(ref tokens, "by");

                                lines_terminated_by = tokens[0].Text[1..^1];
                                tokens.RemoveAt(0);
                                break;
                        }
                        first_subclause = GetFirst(tokens).ToLower();
                    }
                }

                first = GetFirst(tokens).ToLower();
            }

            Log(string.Format("FIELDS TERMINATED BY '{0}' {1} ENCLOSED BY '{2}' ESCAPED BY '{3}' LINES TERMINATED BY '{4}' STARTING BY '{5}'",
                    fields_terminated_by,
                    fields_optionally_enclosed_by ? "optionally" : "",
                    fields_enclosed_by,
                    fields_escaped_by,
                    lines_terminated_by,
                    lines_starting_by));

            Log("remaining tokens:");
            foreach (var token in tokens)
            {
                Log("\ttype: " + token.Type.ToString() + ", value: " + token.Text);
            }
            if (tokens.Count > 0)
            {
                throw new Exception("remaining tokens");
            }
        }
    }
}
