using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using xSocks.Model;

namespace xSocks.Controller
{
    abstract class Runner
    {
        private Process _process;
        private int _runningPort;

        protected Process Process {
            get
            {
                return _process;
            }
            set
            {
                _process = value;
            }
        }

        public int RunningPort
        {
            get
            {
                return _runningPort;
            }
            set
            {
                _runningPort = value;
            }
        }

        public abstract void Start(Configuration config);

        public void Stop()
        {
            if (_process != null)
            {
                try
                {
                    _process.Kill();
                    _process.WaitForExit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                _process = null;
            }
        }

        protected int GetFreePort(int defaultPort)
        {
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                properties.GetActiveTcpListeners();

                List<int> usedPorts = new List<int>();
                foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    usedPorts.Add(endPoint.Port);
                }
                for (int port = defaultPort; port <= 65535; port++)
                {
                    if (!usedPorts.Contains(port))
                    {
                        return port;
                    }
                }
            }
            catch (Exception e)
            {
                // in case access denied
                Logging.LogUsefulException(e);
                return defaultPort;
            }
            throw new Exception("No free port found.");
        }

        public event EventHandler<LogEventArgs> LogMessageReceived;

        protected void Process_LogDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (LogMessageReceived != null && !String.IsNullOrEmpty(e.Data))
            {
                LogMessageReceived(this, new LogEventArgs() { Data = e.Data });
            }
        }
    }
}
