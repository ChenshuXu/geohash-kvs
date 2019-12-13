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
    public class LoadDataTest
    {
        [TestMethod]
        public void TestParserBasic()
        {
            string filePath = "../../../Resources/imptest-5000.txt";
            string query = "LOAD DATA LOCAL INFILE '" + filePath + "' INTO TABLE imptest FIELDS TERMINATED BY ',' LINES TERMINATED BY '\n'";
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
            string query = "LOAD DATA LOCAL INFILE 'abc.txt' INTO TABLE imptest FIELDS TERMINATED BY 'aaa' OPTIONALLY ENCLOSED BY 'b' ESCAPED BY 'c' LINES STARTING BY 'eee' TERMINATED BY '\n'";
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
            // TODO: No subclause after fields
            string query = "LOAD DATA LOCAL INFILE 'abc.txt' INTO TABLE imptest FIELDS LINES TERMINATED BY '\n'";

        }

        [TestMethod]
        public void TestParseLinesBasic()
        {
            string file_string = @"10,NAV15BEO0Z
11,87NSZSSP4J
12,MZOTXUFT9T
13,6DFOKC74ZG
14,3KZ94GFGLZ
15,VQTMAXWCDK
";

            string[] expected = {
                "10,NAV15BEO0Z",
                "11,87NSZSSP4J",
                "12,MZOTXUFT9T",
                "13,6DFOKC74ZG",
                "14,3KZ94GFGLZ",
                "15,VQTMAXWCDK"
            };

            string query = "LOAD DATA LOCAL INFILE 'myfile.txt' INTO TABLE imptest FIELDS TERMINATED BY ',' LINES TERMINATED BY '\n'";
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);
            LoadDataParser parser = new LoadDataParser(tokens);
            Assert.AreEqual("\n", parser.lines_terminated_by);

            string[] actual = ClientSession.ParseLines(file_string, parser);
            foreach (var a in actual)
            {
                Console.WriteLine(a);
            }
            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [TestMethod]
        public void TestParseLines()
        {
            string file_string = @"zzz!10,NAV15BEO0Zll
before zzz!11,87NSZSSP4Jll
zzz!12,MZOTXUFT9Tll after
zzz!13,6DFOKC74ZGll
zzz!14,3KZ94GFGLZll
zzz!15,VQTMAXWCDKll";

            string[] expected = {
                "10,NAV15BEO0Z",
                "11,87NSZSSP4J",
                "12,MZOTXUFT9T",
                "13,6DFOKC74ZG",
                "14,3KZ94GFGLZ",
                "15,VQTMAXWCDK"
            };

            string query = "LOAD DATA LOCAL INFILE 'myfile.txt' INTO TABLE imptest FIELDS TERMINATED BY ',' LINES STARTING BY 'zzz!' TERMINATED BY 'll'";
            List<TSQLToken> tokens = TSQLTokenizer.ParseTokens(query);
            LoadDataParser parser = new LoadDataParser(tokens);
            Assert.AreEqual("zzz!", parser.lines_starting_by);
            Assert.AreEqual("ll", parser.lines_terminated_by);

            string[] actual = ClientSession.ParseLines(file_string, parser);
            foreach (var a in actual)
            {
                Console.WriteLine(a);
            }
            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [TestMethod]
        public void TestParseFields()
        {
            // settings
            LoadDataParser parser = new LoadDataParser();
            parser.fields_terminated_by = ",";
            parser.fields_enclosed_by = "\"";
            parser.fields_escaped_by = "\\";

            string line;
            string[] expected;
            string[] actual;

            // FIELDS [OPTIONALLY] ENCLOSED BY controls quoting of fields.
            // For output (SELECT ... INTO OUTFILE), if you omit the word OPTIONALLY,
            // all fields are enclosed by the ENCLOSED BY character
            parser.fields_optionally_enclosed_by = false;

            // "1","a string","100.20"
            line = "\"1\",\"a string\",\"100.20\"";
            Console.WriteLine(line);
            expected = new string[]
            {
                "1", // 1
                "a string", // a string
                "100.20" // 100.20
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));


            // "2","a string containing a , comma","102.20"
            line = "\"2\",\"a string containing a , comma\",\"102.20\"";
            Console.WriteLine(line);
            expected = new string[]
            {
                "2",
                "a string containing a , comma",
                "102.20"
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));

            // "3","a string containing a \" quote","102.20"
            line = "\"3\",\"a string containing a \\\" quote\",\"102.20\"";
            Console.WriteLine(line);
            expected = new string[]
            {
                "3",
                "a string containing a \" quote",
                "102.20"
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));

            // "4","a string containing a \", quote and comma","102.20"
            line = "\"4\",\"a string containing a \\\", quote and comma\",\"102.20\"";
            Console.WriteLine(line);
            expected = new string[]
            {
                "4",
                "a string containing a \", quote and comma",
                "102.20"
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));
            /*
             * TODO: not support optionally yet
            // If you specify OPTIONALLY, the ENCLOSED BY character is used
            // only to enclose values from columns that have a string data type
            parser.fields_optionally_enclosed_by = true;

            // 1,"a string",100.20
            line = "1,\"a string\",100.20";
            Console.WriteLine(line);
            expected = new string[]
            {
                "1", // 1
                "a string", // a string
                "100.20" // 100.20
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));

            // 2,"a string containing a , comma",102.20
            line = "2,\"a string containing a , comma\",102.20";
            Console.WriteLine(line);
            expected = new string[]
            {
                "2",
                "a string containing a , comma",
                "102.20"
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));

            // 3,"a string containing a \" quote",102.20
            line = "3,\"a string containing a \\\" quote\",102.20";
            Console.WriteLine(line);
            expected = new string[]
            {
                "3",
                "a string containing a \" quote",
                "102.20"
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));

            // 4,"a string containing a \", quote and comma",102.20
            line = "4,\"a string containing a \\\", quote and comma\",102.20";
            Console.WriteLine(line);
            expected = new string[]
            {
                "4",
                "a string containing a \", quote and comma",
                "102.20"
            };
            actual = ClientSession.ParseFields(line, parser);
            Assert.IsTrue(actual.SequenceEqual(expected));
            */
        }
    }
}
