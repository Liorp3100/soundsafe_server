using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class User
    {
        private string username;
        private string password;

        public User(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
        public string Getusername()
        {
            return this.username;
        }
        public string Getpassword()
        {
            return this.password;
        }
    }
}
