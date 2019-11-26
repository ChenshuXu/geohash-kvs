using System;

namespace MySqlServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(
                "127.0.0.1",
                3306,
                "../../../certs/server-cert.p12",
                "pswd"
                );
            server.Debug = true;

            server.StartAsync();

            while (true)
            {
                string userInput = InputString("Command [? for help]:", null, false);
                switch (userInput)
                {
                    case "?":
                        break;
                    case "q":
                    case "Q":
                        break;
                    case "c":
                    case "C":
                    case "cls":
                        break;
                    case "list":
                        break;
                    case "send":
                        break;
                    case "remove":
                        Console.Write("IP:Port: ");
                        string ipPort = Console.ReadLine();
                        break;
                    case "dispose":
                        break;
                }
            }
        }

        static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }
    }
}
