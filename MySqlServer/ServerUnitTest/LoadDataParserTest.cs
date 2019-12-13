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
    public class LoadDataParserTest
    {
        [TestMethod]
        public void TestParserBasic()
        {
            string filePath = "../../../Resources/imptest-5000.txt";
            string query = "LOAD DATA LOCAL INFILE '" + filePath + "' INTO TABLE imptest FIELDS TERMINATED BY ',' LINES TERMINATED BY '\n';";
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);
            LoadDataParser parser = new LoadDataParser(tokens);

            Assert.AreEqual(filePath, parser.file_name);
            Assert.AreEqual("imptest", parser.table_name);
            Assert.IsFalse(parser.fields_optionally_enclosed_by);
            Assert.AreEqual(",", parser.fields_terminated_by);
            Assert.AreEqual("\n", parser.lines_terminated_by);
        }

        [TestMethod]
        public void TestParserComplex()
        {
            string query = "LOAD DATA LOCAL INFILE 'abc.txt' INTO TABLE imptest FIELDS TERMINATED BY 'aaa' OPTIONALLY ENCLOSED BY 'b' ESCAPED BY 'c' LINES STARTING BY 'eee' TERMINATED BY '\n';";
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);
            LoadDataParser parser = new LoadDataParser(tokens);

            Assert.AreEqual("abc.txt", parser.file_name);
            Assert.AreEqual("imptest", parser.table_name);
            Assert.AreEqual("aaa", parser.fields_terminated_by);
            Assert.AreEqual("b", parser.fields_enclosed_by);
            Assert.IsTrue(parser.fields_optionally_enclosed_by);
            Assert.AreEqual("c", parser.fields_escaped_by);
            Assert.AreEqual("eee", parser.lines_starting_by);
            Assert.AreEqual("\n", parser.lines_terminated_by);
        }

        [TestMethod]
        public void TestParserErrorHanding()
        {
            // No subclause after fields
            string query = "LOAD DATA LOCAL INFILE 'abc.txt' INTO TABLE imptest FIELDS LINES TERMINATED BY '\n';";

        }
    }
}
