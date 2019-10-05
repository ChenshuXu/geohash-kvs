using System;
namespace server
{
    public interface ILog
    {
        void info(string str);
        string getInfo();
    }

    class MyConsoleLogger : ILog
    {
        public string infoStr { get; set; }
        public void info(string str)
        {
            Console.WriteLine(str);
            infoStr = str;
        }
        public string getInfo()
        {
            return infoStr;
        }
    }
}
