using System;
using System.Text;
using System.Text.RegularExpressions;

namespace xSocks.Model
{
    [Serializable]
    public class Server
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public string Remarks { get; set; }

        public string FriendlyName()
        {
            if (string.IsNullOrEmpty(Host))
            {
                return "Untittled server";
            }
            if (string.IsNullOrEmpty(Remarks))
            {
                return Host + ":" + Port;
            }
            return Remarks + " (" + Host + ":" + Port + ")";
        }

        public Server()
        {
            Host = "";
            Port = 1073;
            Password = "";
            Remarks = "";
        }

    }
}
