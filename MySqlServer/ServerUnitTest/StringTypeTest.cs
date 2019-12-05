using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlServer;

namespace ServerUnitTest
{
    [TestClass]
    public class StringTypeTest
    {
        public static void PrintAllBytes(byte[] data)
        {
            foreach (byte b in data)
            {
                Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
        }

        [TestMethod]
        public void TestLengthEncodedString()
        {

        }

        [TestMethod]
        public void TestFixedLengthString()
        {

        }

        [TestMethod]
        public void TestNulTerminatedString()
        {

        }
    }
}
