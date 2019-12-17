using System;
using System.Collections.Generic;
using TSQL.Tokens;

namespace MySqlServer
{
    public class Parser
    {
        public bool Debug = true;

        /// <summary>
        /// Remove first item and check if it is same as keyword
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="keyword"></param>
        public static void PopAndCheck(ref List<TSQLToken> tokens, string keyword)
        {
            if (tokens.Count == 0)
            {
                throw new Exception("tokens size is 0");
            }

            TSQLToken first = tokens[0];
            tokens.RemoveAt(0);
            if (first.Text.ToLower() != keyword)
            {
                throw new Exception(string.Format("{0} != {1}", first.Text, keyword));
            }
        }

        /// <summary>
        /// Remove first item if it is same as keyword
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static bool PopIfExist(ref List<TSQLToken> tokens, string keyword)
        {
            if (tokens.Count == 0)
            {
                return false;
            }

            if (tokens[0].Text == keyword)
            {
                tokens.RemoveAt(0);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the first token in token list
        /// empty string if nothing
        /// </summary>
        /// <param name="tokens">token list</param>
        /// <returns>first token</returns>
        public static string GetFirstToken(List<TSQLToken> tokens)
        {
            if (tokens.Count == 0)
            {
                return "";
            }
            return tokens[0].Text;
        }

        public static string PopFirstToken(ref List<TSQLToken> tokens)
        {
            if (tokens.Count == 0)
            {
                return "";
            }
            string tmp = tokens[0].Text;
            tokens.RemoveAt(0);
            return tmp;
        }

        /// <summary>
        /// Read tokens, get the table name, handle . character
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>table object that contains table name and database name</returns>
        public static Table GetQualifiedTableName(ref List<TSQLToken> tokens, string connectedDB)
        {
            TSQLToken possibleColName = tokens[0];
            tokens.RemoveAt(0);

            if (tokens.Count != 0 && tokens[0].Text == ".")
            {
                tokens.RemoveAt(0);
                TSQLToken actualColName = tokens[0];
                tokens.RemoveAt(0);
                string databaseName = possibleColName.Text;
                return new Table(actualColName.Text, databaseName);
            }
            return new Table(possibleColName.Text, connectedDB);
        }

        /// <summary>
        /// Read tokens, get the first column name, handle . character
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>column object that contains table name and column name</returns>
        public static Column GetQualifiedColumnName(ref List<TSQLToken> tokens)
        {
            TSQLToken possibleColName = tokens[0];
            tokens.RemoveAt(0);

            if (tokens.Count != 0 && tokens[0].Text == ".")
            {
                tokens.RemoveAt(0);
                TSQLToken actualColName = tokens[0];
                tokens.RemoveAt(0);
                string tableName = possibleColName.Text;
                return new Column
                {
                    ColumnName = actualColName.Text,
                    TableName = tableName,
                    _TokenType = possibleColName.Type
                };
            }
            return new Column
            {
                ColumnName = possibleColName.Text,
                _TokenType = possibleColName.Type
            };
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        public static void LogBasic(string msg)
        {
            string timeStr = DateTime.Now.Minute.ToString() + '.' + DateTime.Now.Second.ToString() + '.' + DateTime.Now.Millisecond.ToString();
            //Console.WriteLine("[client session][" + timeStr + "]" + "[" + _IpPort + "] " + msg);
            Console.WriteLine("[parser][" + timeStr + "] " + msg);
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg)
        {
            if (Debug)
            {
                LogBasic(msg);
            }
        }

        public void Log(object[] objs)
        {
            if (Debug)
            {
                foreach (var obj in objs)
                {
                    LogBasic(Convert.ToString(obj));
                }
            }
        }
    }
}
