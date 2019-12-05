using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Playground
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Task task = Task.Run(() => PrintAsync());
            
            
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("2");
                await Task.Delay(30);
            }

            Console.WriteLine("Hello World finish");

            string a = "\n";

            string b = @"
1,eweeeee
2,wweqwe
3,otoyrty
4,88988uu
";
            Console.WriteLine("a length " + a.Length);
            int current = 0;
            while (current < b.Length)
            {
                string c = b.Substring(current, 1);
                Console.WriteLine(c);
                if (c == a)
                {
                    Console.WriteLine("hit");
                }
                current += 1;
            }

            GenerateFile();
        }

        static void GenerateFile()
        {
            string docPath = "../../../";

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "imptest.txt"), true))
            {
                for (int i = 0; i < 5000000; i++)
                {
                    string line = "lbegin" + "[" + i.ToString() + "] [" + RandomString(10) + "] lend";
                    outputFile.WriteLine(line);
                }
            }
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static async Task PrintAsync()
        {
            while (true)
            {
                Console.WriteLine("1");
                await Task.Delay(30);
            }
        }
    }
}
