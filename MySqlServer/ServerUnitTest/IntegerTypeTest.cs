using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlServer;

namespace ServerUnitTest
{
    [TestClass]
    public class IntegerTypeTest
    {
        public static void PrintAllBytes(byte[] data)
        {
            foreach (byte b in data)
            {
                Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
        }

        [TestMethod]
        public void TestLengthEncodedInteger()
        {
            long value = 1;
            byte[] bytes_expected = { (byte)value };
            byte[] bytes_actual = ClientSession.LengthEncodedInteger(value);
            Assert.IsTrue(bytes_actual.SequenceEqual(bytes_expected));

            value = 300;
        }

        [TestMethod]
        public void TestFixedLengthInteger()
        {

        }
    }
}
