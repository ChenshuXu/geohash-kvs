using System;
namespace MySqlServer
{
    public class User
    {
        internal string Password
        {
            get { return _Password; }
            set { _Password = value; }
        }
        private string _Username;
        private string _Password;
        public User(string username, string password)
        {
            _Username = username;
            _Password = password;
        }
    }
}
