using System;
using System.Diagnostics;
using System.IO;
using Xsocks.Model;
using Xsocks.Properties;

namespace Xsocks.Controller
{
    class XsocksRunner : Runner
    {
        private static readonly string Temppath;

        static XsocksRunner()
        {
            Temppath = Path.GetTempPath();
            try
            {
                FileManager.UncompressFile(Temppath + "/xsocks.exe", Resources.xsocks_exe);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public override void Start(Configuration config)
        {
            if (Process == null)
            {
                Process[] existingPolipo = Process.GetProcessesByName("xsocks");
                foreach (Process p in existingPolipo)
                {
                    if (Process.GetCurrentProcess().Id == p.Id)
                    {
                        continue;
                    }
                    try
                    {
                        p.Kill();
                        p.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }

                config.Socks5ProxyPort = RunningPort = GetFreePort(config.Socks5ProxyPort);
                Server server = config.GetCurrentServer();

                var arguments = string.Format("-s {0}:{1} -k {2} -l {3}:{4} -t 300 -n", 
                    server.Host, server.Port, server.Password,
                    config.ShareOverLan ? "0.0.0.0" : "127.0.0.1",
                    RunningPort);

                Process = new Process
                {
                    StartInfo =
                    {
                        FileName = Temppath + "/xsocks.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                Process.Start();
            }
        }

    }
}
