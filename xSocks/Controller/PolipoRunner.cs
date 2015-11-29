using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using xSocks.Model;
using xSocks.Properties;

namespace xSocks.Controller
{
    class PolipoRunner : Runner
    {
        private static readonly string Temppath;

        static PolipoRunner()
        {
            Temppath = Path.GetTempPath();
            try
            {
                FileManager.UncompressFile(Temppath + "/xs_polipo.exe", Resources.polipo_exe);
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
                Process[] existingPolipo = Process.GetProcessesByName("xs_polipo");
                foreach (Process p in existingPolipo)
                {
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

                config.HttpProxyPort = RunningPort = GetFreePort(config.HttpProxyPort);

                string polipoConfig = Resources.polipo_config;
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", config.Socks5ProxyPort.ToString());
                polipoConfig = polipoConfig.Replace("__POLIPO_BIND_PORT__", RunningPort.ToString());
                polipoConfig = polipoConfig.Replace("__POLIPO_BIND_IP__", config.ShareOverLan ? "0.0.0.0" : "127.0.0.1");
                FileManager.ByteArrayToFile(Temppath + "/polipo.conf", Encoding.UTF8.GetBytes(polipoConfig));

                Process = new Process
                {
                    StartInfo =
                    {
                        FileName = Temppath + "/xs_polipo.exe",
                        Arguments = "-c \"" + Temppath + "/polipo.conf\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                Process.OutputDataReceived += Process_LogDataReceived;
                Process.ErrorDataReceived += Process_LogDataReceived;
                Process.Start();
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
            }
        }
    }
}
