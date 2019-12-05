using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlServer;

namespace ServerUnitTest
{
    [TestClass]
    public class ClientSessionTest
    {
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

            string lines_starting_by = "";
            string lines_terminated_by = "\n";

            string[] actual = ClientSession.ParseLines(file_string, lines_starting_by, lines_terminated_by);
            foreach(var a in actual)
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

            string lines_starting_by = "zzz!";
            string lines_terminated_by = "ll";

            string[] actual = ClientSession.ParseLines(file_string, lines_starting_by, lines_terminated_by);
            foreach (var a in actual)
            {
                Console.WriteLine(a);
            }
            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [TestMethod]
        public void TestParseFields()
        {
            string fields_terminated_by = "\t";
            string fields_enclosed_by = "";
            string fields_escaped_by = "\\";

            string line1 = "\"1\",\"a string\",\"100.20\"";
            string[] expected1 = {

            };
            string line2 = "\"2\",\"a string containing a , comma\",\"102.20\"";
            string line3 = "\"3\",\"a string containing a \\\" quote\",\"102.20\"";
            string line4 = "\"4\",\"a string containing a \\\", quote and comma\",\"102.20\"";


        }
    }
}
