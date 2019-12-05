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
            TSQLToken first = tokens[0];
            tokens.RemoveAt(0);
            if (first.Text.ToLower() != keyword)
            {
                throw new Exception(string.Format("{0}!={1}", first.Text, keyword));
            }
        }

        /// <summary>
        /// Get the first token in token list
        /// </summary>
        /// <param name="tokens">token list</param>
        /// <returns>first token, null if nothing</returns>
        public static string GetFirst(List<TSQLToken> tokens)
        {
            if (tokens.Count == 0)
            {
                return "";
            }
            return tokens[0].Text;
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
