using System;
using MySqlServer;

namespace TestDatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DatabaseController DatabaseController = new DatabaseController();
            DatabaseController.Debug = true;
            Database database = DatabaseController.GetDatabase("dummy");

        }
    }
}
