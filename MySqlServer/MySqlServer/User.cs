using System;
namespace MySqlServer
{
    public class User
    {
        public string _Username;
        public string _Password;
        public User(string username, string password)
        {
            _Username = username;
            _Password = password;
        }
    }
}
