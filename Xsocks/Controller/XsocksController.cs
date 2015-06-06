using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Xsocks.Model;

namespace Xsocks.Controller
{
    public class XsocksController
    {
        private Listener _listener;
        private PACServer _pacServer;
        private Configuration _config;
        private XsocksRunner _xsocksRunner;
        private PolipoRunner _polipoRunner;
        private GFWListUpdater _gfwListUpdater;
        private bool _stopped;
        private bool _systemProxyIsDirty;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }

        public event EventHandler ConfigChanged;
        public event EventHandler EnableStatusChanged;
        public event EventHandler EnableGlobalChanged;
        public event EventHandler ShareOverLanStatusChanged;

        // when user clicked Edit PAC, and PAC file has already created
        public event EventHandler<PathEventArgs> PacFileReadyToOpen;
        public event EventHandler<PathEventArgs> UserRuleFileReadyToOpen;

        public event EventHandler<GFWListUpdater.ResultEventArgs> UpdatePacFromGfwListCompleted;
        public event ErrorEventHandler UpdatePacFromGfwListError;

        public event ErrorEventHandler Errored;

        public XsocksController()
        {
            _config = Configuration.Load();
        }

        public void ToggleEnable(bool enabled)
        {
            _config.Enabled = enabled;
            //UpdateSystemProxy();
            SaveConfig(_config);
            if (EnableStatusChanged != null)
            {
                EnableStatusChanged(this, new EventArgs());
            }
        }

        public void ToggleGlobal(bool global)
        {
            _config.Global = global;
            //UpdateSystemProxy();
            SaveConfig(_config);
            if (EnableGlobalChanged != null)
            {
                EnableGlobalChanged(this, new EventArgs());
            }
        }

        public void ToggleShareOverLan(bool enabled)
        {
            _config.ShareOverLan = enabled;
            SaveConfig(_config);
            if (ShareOverLanStatusChanged != null)
            {
                ShareOverLanStatusChanged(this, new EventArgs());
            }
        }

        public Configuration GetConfiguration()
        {
            return _config;
        }

        public void TouchPACFile()
        {
            string pacFilename = _pacServer.TouchPACFile();
            if (PacFileReadyToOpen != null)
            {
                PacFileReadyToOpen(this, new PathEventArgs() { Path = pacFilename });
            }
        }

        public void TouchUserRuleFile()
        {
            string userRuleFilename = _pacServer.TouchUserRuleFile();
            if (UserRuleFileReadyToOpen != null)
            {
                UserRuleFileReadyToOpen(this, new PathEventArgs() { Path = userRuleFilename });
            }
        }

        public void UpdatePACFromGFWList()
        {
            if (_gfwListUpdater != null)
            {
                _gfwListUpdater.UpdatePacFromGfwList(_config);
            }
        }

        public void SavePACUrl(string pacUrl)
        {
            _config.PacUrl = pacUrl;
            UpdateSystemProxy();
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        public void UseOnlinePAC(bool useOnlinePac)
        {
            _config.UseOnlinePac = useOnlinePac;
            UpdateSystemProxy();
            SaveConfig(_config);
            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }
        }

        protected void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            Reload();
        }

        public void SaveServers(List<Server> servers)
        {
            _config.Servers = servers;
            SaveConfig(_config);
        }

        public void Reload()
        {
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();

            Server server = _config.GetCurrentServer();
            if (String.IsNullOrEmpty(server.Host) ||
                (server.Port <= 0 || server.Port > 65535) ||
                String.IsNullOrEmpty(server.Password))
            {
                return;
            }

            if (_xsocksRunner == null)
            {
                _xsocksRunner = new XsocksRunner();
                _xsocksRunner.LogMessageReceived += _xsocksRunner_LogMessageReceived;
            }
            if (_polipoRunner == null)
            {
                _polipoRunner = new PolipoRunner();
            }
            if (_pacServer == null)
            {
                _pacServer = new PACServer();
                _pacServer.PACFileChanged += PACServerPACFileChanged;
            }
            if (_gfwListUpdater == null)
            {
                _gfwListUpdater = new GFWListUpdater();
                _gfwListUpdater.UpdateCompleted += PACServerPACUpdateCompleted;
                _gfwListUpdater.Error += PACServerPACUpdateError;
            }

            if (_listener != null)
            {
                _listener.Stop();
            }

            // don't put polipoRunner.Start() before pacServer.Stop()
            // or bind will fail when switching bind address from 0.0.0.0 to 127.0.0.1
            // though UseShellExecute is set to true now
            // http://stackoverflow.com/questions/10235093/socket-doesnt-close-after-application-exits-if-a-launched-process-is-open
            _xsocksRunner.Stop();
            _polipoRunner.Stop();

            try
            {
                _xsocksRunner.Start(_config);
                _polipoRunner.Start(_config);

                _pacServer.HttpProxyPort = _polipoRunner.RunningPort;

                List<Listener.IService> services = new List<Listener.IService>();
                services.Add(_pacServer);
                _listener = new Listener(services);
                _listener.Start(_config.PACServerPort, _config.ShareOverLan);
            }
            catch (Exception e)
            {
                // translate Microsoft language into human language
                // i.e. An attempt was made to access a socket in a way forbidden by its access permissions => Port already in use
                if (e is SocketException)
                {
                    SocketException se = (SocketException)e;
                    if (se.SocketErrorCode == SocketError.AccessDenied)
                    {
                        e = new Exception("Port already in use", e);
                    }
                }
                Logging.LogUsefulException(e);
                ReportError(e);
            }

            if (ConfigChanged != null)
            {
                ConfigChanged(this, new EventArgs());
            }

            UpdateSystemProxy();
            Utils.ReleaseMemory();
        }

        public event EventHandler<LogEventArgs> LogMessageReceived;

        void _xsocksRunner_LogMessageReceived(object sender, LogEventArgs e)
        {
            if (LogMessageReceived != null)
            {
                LogMessageReceived(this, e);
            }
        }

        protected void ReportError(Exception e)
        {
            if (Errored != null)
            {
                Errored(this, new ErrorEventArgs(e));
            }
        }

        public void SelectServerIndex(int index)
        {
            _config.Index = index;
            SaveConfig(_config);
        }

        public void Start()
        {
            Reload();
        }

        public void Stop()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;

            if (_listener != null)
            {
                _listener.Stop();
            }
            if (_xsocksRunner != null)
            {
                _xsocksRunner.Stop();
            }
            if (_polipoRunner != null)
            {
                _polipoRunner.Stop();
            }

            if (_config.Enabled)
            {
                SystemProxy.Update(_config, true);
            }
        }

        private void UpdateSystemProxy()
        {
            if (_config.Enabled)
            {
                SystemProxy.Update(_config, false);
                _systemProxyIsDirty = true;
            }
            else
            {
                // only switch it off if we have switched it on
                if (_systemProxyIsDirty)
                {
                    SystemProxy.Update(_config, false);
                    _systemProxyIsDirty = false;
                }
            }
        }

        private void PACServerPACFileChanged(object sender, EventArgs e)
        {
            UpdateSystemProxy();
        }

        private void PACServerPACUpdateCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            if (UpdatePacFromGfwListCompleted != null)
                UpdatePacFromGfwListCompleted(this, e);
        }

        private void PACServerPACUpdateError(object sender, ErrorEventArgs e)
        {
            if (UpdatePacFromGfwListError != null)
                UpdatePacFromGfwListError(this, e);
        }

    }
}
