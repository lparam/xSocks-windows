using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using xSocks.Properties;
using xSocks.Util;

namespace xSocks.Controller
{
    class PACServer : Listener.IService
    {
        private static string _pacFile = Path.Combine(Common.UserDataFolder, "pac.txt");
        private static string _userRuleFile = Path.Combine(Common.UserDataFolder, "user-rule.txt");

        FileSystemWatcher _watcher;
        int _httpProxyPort = 8188;

        public event EventHandler PACFileChanged;

        public PACServer()
        {
            WatchPacFile();
        }

        public static string PacFile
        {
            get { return _pacFile; }
            set { _pacFile = value; }
        }

        public static string UserRuleFile
        {
            get { return _userRuleFile; }
            set { _userRuleFile = value; }
        }

        public int HttpProxyPort
        {
            set { _httpProxyPort = value; }
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            try
            {
                string request = Encoding.UTF8.GetString(firstPacket, 0, length);
                string[] lines = request.Split('\r', '\n');
                bool hostMatch = false, pathMatch = false, useSocks = false;
                foreach (string line in lines)
                {
                    string[] kv = line.Split(new[] { ':' }, 2);
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "Host")
                        {
                            if (kv[1].Trim() == ((IPEndPoint)socket.LocalEndPoint).ToString())
                            {
                                hostMatch = true;
                            }
                        }
                        else if (kv[0] == "User-Agent")
                        {
                            // we need to drop connections when changing servers
                            /* if (kv[1].IndexOf("Chrome") >= 0)
                            {
                                useSocks = true;
                            } */
                        }
                    }
                    else if (kv.Length == 1)
                    {
                        if (line.IndexOf("pac", StringComparison.Ordinal) >= 0)
                        {
                            pathMatch = true;
                        }
                    }
                }
                if (hostMatch && pathMatch)
                {
                    SendResponse(firstPacket, length, socket, useSocks);
                    return true;
                }
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }


        public string TouchPACFile()
        {
            if (File.Exists(_pacFile))
            {
                return _pacFile;
            }
            else
            {
                FileManager.UncompressFile(_pacFile, Resources.proxy_pac_txt);
                return _pacFile;
            }
        }

        internal string TouchUserRuleFile()
        {
            if (File.Exists(_userRuleFile))
            {
                return _userRuleFile;
            }
            else
            {
                File.WriteAllText(_userRuleFile, Resources.user_rule);
                return _userRuleFile;
            }
        }

        private string GetPACContent()
        {
            if (File.Exists(_pacFile))
            {
                return File.ReadAllText(_pacFile, Encoding.UTF8);
            }
            else
            {
                return Utils.UnGzip(Resources.proxy_pac_txt);
            }
        }

        public void SendResponse(byte[] firstPacket, int length, Socket socket, bool useSocks)
        {
            try
            {
                string pac = GetPACContent();

                IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;

                string proxy = GetPACAddress(firstPacket, length, localEndPoint, useSocks);

                pac = pac.Replace("__PROXY__", proxy);

                string text = String.Format(@"HTTP/1.1 200 OK
Server: xSocks
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {0}
Connection: Close

", Encoding.UTF8.GetBytes(pac).Length) + pac;
                byte[] response = Encoding.UTF8.GetBytes(text);
                socket.BeginSend(response, 0, response.Length, 0, SendCallback, socket);
                Utils.ReleaseMemory();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                socket.Close();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            try
            {
                conn.Shutdown(SocketShutdown.Send);
            }
            catch
            {
                // ignored
            }
        }

        private void WatchPacFile()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
            }
            _watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _watcher.Filter = _pacFile;
            _watcher.Changed += Watcher_Changed;
            _watcher.Created += Watcher_Changed;
            _watcher.Deleted += Watcher_Changed;
            _watcher.Renamed += Watcher_Changed;
            _watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (PACFileChanged != null)
            {
                PACFileChanged(this, new EventArgs());
            }
        }

        private string GetPACAddress(byte[] requestBuf, int length, IPEndPoint localEndPoint, bool useSocks)
        {
            //try
            //{
            //    string requestString = Encoding.UTF8.GetString(requestBuf);
            //    if (requestString.IndexOf("AppleWebKit") >= 0)
            //    {
            //        string address = "" + localEndPoint.Address + ":" + config.GetCurrentServer().local_port;
            //        proxy = "SOCKS5 " + address + "; SOCKS " + address + ";";
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
            return (useSocks ? "SOCKS5 " : "PROXY ") + localEndPoint.Address + ":" + _httpProxyPort + ";";
        }
    }
}
