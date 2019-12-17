using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlServer;
using TSQL;
using TSQL.Tokens;

namespace ServerUnitTest
{
    [TestClass]
    public class InsertTest
    {
        [TestMethod]
        public void TestParserSingleValue()
        {
            string query = "INSERT INTO tbl_name (a,b,c) VALUES(1,2,3)";
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);
            InsertParser parser = new InsertParser(tokens);

            Assert.AreEqual("tbl_name", parser.table_name);

            string[] column_name = { "a", "b", "c" };
            Assert.IsTrue(parser.column_names.SequenceEqual(column_name));

            string[][] values =
            {
                new string[] { "1", "2", "3" }
            };
            for (int i = 0; i < parser.row_values.Length; i++)
            {
                Assert.IsTrue(parser.row_values[i].SequenceEqual(values[i]));
            }
        }

        [TestMethod]
        public void TestParserMultiValues()
        {
            string query = "INSERT INTO tbl_name (a,b,c) VALUES(1,2,3),(4,5,6),(7,8,9)";
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);
            InsertParser parser = new InsertParser(tokens);

            Assert.AreEqual("tbl_name", parser.table_name);

            string[] column_name = { "a", "b", "c" };
            Assert.IsTrue(parser.column_names.SequenceEqual(column_name));

            string[][] values =
            {
                new string[] { "1", "2", "3" },
                new string[] { "4", "5", "6" },
                new string[] { "7", "8", "9" }
            };
            for ( int i=0; i<parser.row_values.Length; i++)
            {
                Assert.IsTrue(parser.row_values[i].SequenceEqual(values[i]));
            }
        }

        
    }
}
