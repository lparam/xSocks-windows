using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using xSocks.Controller;
using xSocks.Model;
using xSocks.Properties;

namespace xSocks.View
{
    class xSocksApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private ContextMenu _trayIconContextMenu;

        private MenuItem _enableItem;
        private MenuItem _autoStartupItem;
        private MenuItem _shareOverLanItem;
        private MenuItem _localPACItem;
        private MenuItem _onlinePACItem;
        private MenuItem _editLocalPACItem;
        private MenuItem _updateFromGFWListItem;
        private MenuItem _editGFWUserRuleItem;
        private MenuItem _editOnlinePACItem;
        private MenuItem _pacModeItem;
        private MenuItem _globalModeItem;
        private MenuItem _modeItem;
        private MenuItem _seperatorItem;
        private MenuItem _serversItem;

        private MainForm _configForm;
        private readonly xSocksController _controller;

        private bool _isFirstRun;

        public xSocksApplicationContext(xSocksController controller)
        {
            Application.ApplicationExit += OnApplicationExit;

            BuildMenu();

            _controller = controller;
            controller.EnableStatusChanged += ControllerEnableStatusChanged;
            controller.ConfigChanged += ControllerConfigChanged;
            controller.PacFileReadyToOpen += ControllerFileReadyToOpen;
            controller.UserRuleFileReadyToOpen += ControllerFileReadyToOpen;
            controller.ShareOverLanStatusChanged += ControllerShareOverLanStatusChanged;
            controller.EnableGlobalChanged += ControllerEnableGlobalChanged;
            controller.Errored += ControllerErrored;
            controller.UpdatePacFromGfwListCompleted += ControllerUpdatePACFromGFWListCompleted;
            controller.UpdatePacFromGfwListError += ControllerUpdatePACFromGFWListError;

            _trayIcon = new NotifyIcon
            {
                Visible = true
            };
            _trayIcon.MouseDoubleClick += TrayIconDoubleClick;
            _trayIcon.ContextMenu = _trayIconContextMenu;
            UpdateTrayIcon();

            LoadCurrentConfiguration();

            if (controller.GetConfiguration().IsDefault)
            {
                _isFirstRun = true;
                ShowConfigForm();
            }
        }

        private void LoadCurrentConfiguration()
        {
            var config = _controller.GetConfiguration();
            UpdateServersMenu();
            _enableItem.Checked = config.Enabled;
            _modeItem.Enabled = config.Enabled;
            _globalModeItem.Checked = config.Global;
            _pacModeItem.Checked = !config.Global;
            _shareOverLanItem.Checked = config.ShareOverLan;
            _autoStartupItem.Checked = AutoStartup.Check();
            _onlinePACItem.Checked = _onlinePACItem.Enabled && config.UseOnlinePac;
            _localPACItem.Checked = !_onlinePACItem.Checked;
            UpdatePACItemsEnabledStatus();
        }

        private void UpdateTrayIcon()
        {
            var graphics = Graphics.FromHwnd(IntPtr.Zero);
            var dpi = (int)graphics.DpiX;
            graphics.Dispose();
            var config = _controller.GetConfiguration();
            bool enabled = config.Enabled;
            bool global = config.Global;
            Bitmap icon;
            if (dpi < 97)
            {
                // dpi = 96;
                icon = Resources.icon16g;
            }
            else if (dpi < 121)
            {
                // dpi = 120;
                icon = Resources.icon20g;
            }
            else
            {
                icon = Resources.icon24g;
            }

            if (!enabled)
            {
                Bitmap iconCopy = new Bitmap(icon);
                for (int x = 0; x < iconCopy.Width; x++)
                {
                    for (int y = 0; y < iconCopy.Height; y++)
                    {
                        Color color = icon.GetPixel(x, y);
                        iconCopy.SetPixel(x, y, Color.FromArgb((byte)(color.A / 1.25), color.R, color.G, color.B));
                    }
                }
                icon = iconCopy;
            }
            _trayIcon.Icon = Icon.FromHandle(icon.GetHicon());

            string text;

            if (config.ShareOverLan)
            {
                var ports = "xSocks" + " " + Utils.GetVersion() + "\n" + 
                            "PAC: " + config.PACServerPort + "/pac\n"
                            + "HTTP: " + config.HttpProxyPort + "\n"
                            + "Socks5: " + config.Socks5ProxyPort;
                text = ports + "\n" + config.GetCurrentServer().FriendlyName();
            }
            else
            {
                text = "xSocks" + " " + Utils.GetVersion() + "\n" +
                    (enabled ?
                        "System Proxy On: " + (global ? "Global" : "PAC") :
                        String.Format("Running: Port {0}", config.HttpProxyPort))
                    + "\n" + config.GetCurrentServer().FriendlyName();
            }

            SetNotifyIconText(_trayIcon, text);
        }

        public static void SetNotifyIconText(NotifyIcon ni, string text)
        {
            if (text.Length >= 128) throw new ArgumentOutOfRangeException("Text limited to 127 characters");
            Type t = typeof(NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);
            if ((bool)t.GetField("added", hidden).GetValue(ni))
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
        }

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(text, click);
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(text, items);
        }

        private void UpdatePACItemsEnabledStatus()
        {
            if (_localPACItem.Checked)
            {
                _editLocalPACItem.Enabled = true;
                _updateFromGFWListItem.Enabled = true;
                _editGFWUserRuleItem.Enabled = true;
                _editOnlinePACItem.Enabled = false;
            }
            else
            {
                _editLocalPACItem.Enabled = false;
                _updateFromGFWListItem.Enabled = false;
                _editGFWUserRuleItem.Enabled = false;
                _editOnlinePACItem.Enabled = true;
            }
        }

        private void EnableItemClick(object sender, EventArgs e)
        {
            _controller.ToggleEnable(!_enableItem.Checked);
        }
        private void GlobalModeItemClick(object sender, EventArgs e)
        {
            _controller.ToggleGlobal(true);
        }

        private void PACModeItemClick(object sender, EventArgs e)
        {
            _controller.ToggleGlobal(false);
        }

        private void ShareOverLanItemClick(object sender, EventArgs e)
        {
            _shareOverLanItem.Checked = !_shareOverLanItem.Checked;
            _controller.ToggleShareOverLan(_shareOverLanItem.Checked);
        }

        private void EditPACFileItemClick(object sender, EventArgs e)
        {
            _controller.TouchPACFile();
        }

        private void UpdatePACFromGFWListItemClick(object sender, EventArgs e)
        {
            _controller.UpdatePACFromGFWList();
        }

        private void EditUserRuleFileForGFWListItemClick(object sender, EventArgs e)
        {
            _controller.TouchUserRuleFile();
        }

        private void ServerItemClick(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            _controller.SelectServerIndex((int)item.Tag);
        }

        private void ConfigClick(object sender, EventArgs e)
        {
            ShowConfigForm();
        }

        private void QuitClick(object sender, EventArgs e)
        {
            _controller.Stop();
            _trayIcon.Visible = false;
            Application.Exit();
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            string argument = Logging.LogFile;

            Process.Start("notepad.exe", argument);
        }

        private void LocalPacItemClick(object sender, EventArgs e)
        {
            if (!_localPACItem.Checked)
            {
                _localPACItem.Checked = true;
                _onlinePACItem.Checked = false;
                _controller.UseOnlinePAC(false);
                UpdatePACItemsEnabledStatus();
            }
        }

        private void OnlinePACItemClick(object sender, EventArgs e)
        {
            if (!_onlinePACItem.Checked)
            {
                if (String.IsNullOrEmpty(_controller.GetConfiguration().PacUrl))
                {
                    UpdateOnlinePacUrlItemClick(sender, e);
                }
                if (!String.IsNullOrEmpty(_controller.GetConfiguration().PacUrl))
                {
                    _localPACItem.Checked = false;
                    _onlinePACItem.Checked = true;
                    _controller.UseOnlinePAC(true);
                }
                UpdatePACItemsEnabledStatus();
            }
        }

        private void UpdateOnlinePacUrlItemClick(object sender, EventArgs e)
        {
            string origPacUrl = _controller.GetConfiguration().PacUrl;
            string pacUrl = Microsoft.VisualBasic.Interaction.InputBox(
                "Please input PAC Url", "Edit Online PAC URL",
                origPacUrl);
            if (!string.IsNullOrEmpty(pacUrl) && pacUrl != origPacUrl)
            {
                _controller.SavePACUrl(pacUrl);
            }
        }

        private void AutoStartupItemClick(object sender, EventArgs e)
        {
            bool rc = false;
            try
            {
                AutoStartup.Set(!_autoStartupItem.Checked);
                rc = !_autoStartupItem.Checked;
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to update registry", "xSocks", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            _autoStartupItem.Checked = rc;
        }

        private void AboutItemClick(object sender, EventArgs e)
        {
            Process.Start("https://github.com/lparam/xSocks-windows");
        }

        private void BuildMenu()
        {
            _trayIconContextMenu = new ContextMenu(new[] {
                _enableItem = CreateMenuItem("Enable System Proxy", EnableItemClick),
                _modeItem = CreateMenuGroup("Mode", new[]
                {
                    _pacModeItem = CreateMenuItem("PAC", PACModeItemClick),
                    _globalModeItem = CreateMenuItem("Global", GlobalModeItemClick)
                }),
                _serversItem = CreateMenuGroup("Servers", new[]
                {
                    _seperatorItem = new MenuItem("-"),
                    CreateMenuItem("Edit Servers...", ConfigClick)
                }),
                CreateMenuGroup("PAC ", new[]
                {
                    _localPACItem = CreateMenuItem("Local PAC", LocalPacItemClick),
                    _onlinePACItem = CreateMenuItem("Online PAC", OnlinePACItemClick),
                    new MenuItem("-"),
                    _editLocalPACItem = CreateMenuItem("Edit Local PAC File...", EditPACFileItemClick),
                    _updateFromGFWListItem =
                        CreateMenuItem("Update Local PAC from GFWList", UpdatePACFromGFWListItemClick),
                    _editGFWUserRuleItem =
                        CreateMenuItem("Edit User Rule for GFWList...", EditUserRuleFileForGFWListItemClick),
                    _editOnlinePACItem = CreateMenuItem("Edit Online PAC URL...", UpdateOnlinePacUrlItemClick)
                }),
                new MenuItem("-"),
                _autoStartupItem = CreateMenuItem("Start on Boot", AutoStartupItemClick),
                _shareOverLanItem = CreateMenuItem("Allow Clients from LAN", ShareOverLanItemClick),
                new MenuItem("-"),
                CreateMenuItem("Show Logs", ShowLogItem_Click),
                CreateMenuItem("About", AboutItemClick),
                new MenuItem("-"),
                CreateMenuItem("Quit", QuitClick)
            });
        }

        private void ControllerConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void ControllerEnableStatusChanged(object sender, EventArgs e)
        {
            _enableItem.Checked = _controller.GetConfiguration().Enabled;
            _modeItem.Enabled = _enableItem.Checked;
        }

        void ControllerShareOverLanStatusChanged(object sender, EventArgs e)
        {
            _shareOverLanItem.Checked = _controller.GetConfiguration().ShareOverLan;
        }

        void ControllerEnableGlobalChanged(object sender, EventArgs e)
        {
            _globalModeItem.Checked = _controller.GetConfiguration().Global;
            _pacModeItem.Checked = !_globalModeItem.Checked;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            _trayIcon.Visible = false;
        }

        private void TrayIconDoubleClick(object sender, MouseEventArgs e)
        {
            //Here you can do stuff if the tray icon is doubleclicked
            //trayIcon.ShowBalloonTip(10000);
            if (e.Button == MouseButtons.Left)
            {
                ShowConfigForm();
            }
        }

        private void ControllerErrored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(),
                String.Format("xSocks Error: {0}", e.GetException().Message));
        }

        private void ControllerFileReadyToOpen(object sender, xSocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;
            Process.Start("explorer.exe", argument);
        }

        void ControllerUpdatePACFromGFWListError(object sender, System.IO.ErrorEventArgs e)
        {
            ShowBalloonTip("Failed to update PAC file", e.GetException().Message, ToolTipIcon.Error, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void ControllerUpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            string result = e.Success ? "PAC updated" : "No updates found. Please report to GFWList if you have problems with it.";
            ShowBalloonTip("xSocks", result, ToolTipIcon.Info, 1000);
        }

        private void UpdateServersMenu()
        {
            var items = _serversItem.MenuItems;
            while (items[0] != _seperatorItem)
            {
                items.RemoveAt(0);
            }

            Configuration config = _controller.GetConfiguration();
            for (int i = 0; i < config.Servers.Count; i++)
            {
                Server server = config.Servers[i];
                MenuItem item = new MenuItem(server.FriendlyName());
                item.Tag = i;
                item.Click += ServerItemClick;
                items.Add(i, item);
            }

            if (config.Index >= 0 && config.Index < config.Servers.Count)
            {
                items[config.Index].Checked = true;
            }
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = content;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(timeout);
        }

        private void ShowFirstTimeBalloon()
        {
            if (_isFirstRun)
            {
                _trayIcon.BalloonTipTitle = "xSocks is here";
                _trayIcon.BalloonTipText = "You can turn on/off xSocks in the context menu";
                _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                _trayIcon.ShowBalloonTip(0);
                _isFirstRun = false;
            }
        }

        private void ShowConfigForm()
        {
            if (_configForm != null)
            {
                _configForm.Activate();
            }
            else
            {
                _configForm = new MainForm(_controller);
                _configForm.Show();
                _configForm.FormClosed += ConfigFormFormClosed;
            }
        }

        void ConfigFormFormClosed(object sender, FormClosedEventArgs e)
        {
            _configForm = null;
            Utils.ReleaseMemory();
            ShowFirstTimeBalloon();
        }
    }
}
