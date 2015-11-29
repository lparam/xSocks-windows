using System;
using System.Diagnostics;
using System.IO;
using xSocks.Model;
using xSocks.Properties;

namespace xSocks.Controller
{
    class xSocksRunner : Runner
    {
        private static readonly string Temppath;

        static xSocksRunner()
        {
            Temppath = Path.GetTempPath();
            try
            {
                FileManager.UncompressFile(Temppath + "/xSocks.exe", Resources.xSocks_exe);
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
                Process[] existingPolipo = Process.GetProcessesByName("xSocks");
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
                        FileName = Temppath + "/xSocks.exe",
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
