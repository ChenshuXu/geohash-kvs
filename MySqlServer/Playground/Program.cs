using System;
using System.Threading.Tasks;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Task.Run(() => PrintAsync());
            Console.WriteLine("Hello World finish");

            while (true)
            {
                Console.WriteLine("2");
            }
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
