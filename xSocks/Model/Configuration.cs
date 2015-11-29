using System;
using System.Collections.Generic;
using System.IO;
using SimpleJson;
using xSocks.Util;

namespace xSocks.Model
{
    [Serializable]
    public class Configuration
    {
        public List<Server> Servers { get; set; }
        public int Index { get; set; }
        public bool Global { get; set; }
        public bool Enabled { get; set; }
        public bool ShareOverLan { get; set; }
        public bool IsDefault { get; set; }
        public int PACServerPort { get; set; }
        public int HttpProxyPort { get; set; }
        public int Socks5ProxyPort { get; set; }
        public string PacUrl { get; set; }
        public bool UseOnlinePac { get; set; }

        private static string CONFIG_FILE = Path.Combine(Common.UserDataFolder, "xSocks-config.json");

        public Server GetCurrentServer()
        {
            if (Index >= 0 && Index < Servers.Count)
            {
                return Servers[Index];
            }
            else
            {
                return GetDefaultServer();
            }
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.Port);
            CheckPassword(server.Password);
            CheckServer(server.Host);
        }

        public static Configuration Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = SimpleJson.SimpleJson.DeserializeObject<Configuration>(configContent, new JsonSerializerStrategy());
                config.IsDefault = false;
                if (config.PACServerPort == 0)
                {
                    config.PACServerPort = 8118;
                    config.HttpProxyPort = 8123;
                    config.Socks5ProxyPort = 1080;
                }
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                {
                    Console.WriteLine(e);
                }
                return new Configuration
                {
                    Index = 0,
                    IsDefault = true,
                    PACServerPort = 8118,
                    HttpProxyPort = 8123,
                    Socks5ProxyPort = 1080,
                    Servers = new List<Server>()
                    {
                        GetDefaultServer()
                    }
                };
            }
        }

        public static void Save(Configuration config)
        {
            if (config.Index >= config.Servers.Count)
            {
                config.Index = config.Servers.Count - 1;
            }
            if (config.Index < 0)
            {
                config.Index = 0;
            }
            config.IsDefault = false;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = SimpleJson.SimpleJson.SerializeObject(config);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        private static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new Exception("assertion failure");
            }
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentException("Port out of range");
            }
        }

        private static void CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password can not be blank");
            }
        }

        private static void CheckServer(string server)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("Server IP can not be blank");
            }
        }

        private class JsonSerializerStrategy : PocoJsonSerializerStrategy
        {
            // convert string to int
            public override object DeserializeObject(object value, Type type)
            {
                if (type == typeof(Int32) && value is string)
                {
                    return Int32.Parse(value.ToString());
                }
                return base.DeserializeObject(value, type);
            }
        }
    }
}
