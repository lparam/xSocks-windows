using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Xsocks.Controller;
using Xsocks.Model;
using Xsocks.Properties;

namespace Xsocks.View
{
    public partial class MainForm : Form
    {
        private readonly XsocksController _controller;
        // this is a copy of configuration that we are working on
        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;

        public MainForm(XsocksController controller)
        {
            InitializeComponent();


            // a dirty hack
            this.ServersListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PerformLayout();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.icon128.GetHicon());

            _controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            this.Text = "Xsocks " + Utils.GetVersion();
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void ShowWindow()
        {
            this.Opacity = 1;
            this.Show();
            IPTextBox.Focus();
        }

        private bool SaveOldSelectedServer()
        {
            try
            {
                if (_oldSelectedIndex == -1 || _oldSelectedIndex >= _modifiedConfiguration.Servers.Count)
                {
                    return true;
                }
                Server server = new Server
                {
                    Host = Regex.Replace(IPTextBox.Text, @"\s+", ""),
                    Port = int.Parse(Regex.Replace(ServerPortTextBox.Text, @"\s+", "")),
                    Password = PasswordTextBox.Text,
                    Remarks = RemarksTextBox.Text
                };
                Configuration.CheckServer(server);
                _modifiedConfiguration.Servers[_oldSelectedIndex] = server;

                return true;
            }
            catch (FormatException)
            {
                MessageBox.Show("Illegal port number format");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        private void LoadSelectedServer()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.Servers.Count)
            {
                Server server = _modifiedConfiguration.Servers[ServersListBox.SelectedIndex];

                IPTextBox.Text = server.Host;
                ServerPortTextBox.Text = server.Port.ToString();
                PasswordTextBox.Text = server.Password;
                RemarksTextBox.Text = server.Remarks;
                ServerGroupBox.Visible = true;
                //IPTextBox.Focus();
            }
            else
            {
                ServerGroupBox.Visible = false;
            }
        }

        private void LoadConfiguration(Configuration configuration)
        {
            ServersListBox.Items.Clear();
            foreach (Server server in _modifiedConfiguration.Servers)
            {
                ServersListBox.Items.Add(server.FriendlyName());
            }
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = _controller.GetConfiguration();
            LoadConfiguration(_modifiedConfiguration);
            _oldSelectedIndex = _modifiedConfiguration.Index;
            ServersListBox.SelectedIndex = _modifiedConfiguration.Index;
            LoadSelectedServer();
        }

        private void ServersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_oldSelectedIndex == ServersListBox.SelectedIndex)
            {
                // we are moving back to oldSelectedIndex or doing a force move
                return;
            }
            if (!SaveOldSelectedServer())
            {
                // why this won't cause stack overflow?
                ServersListBox.SelectedIndex = _oldSelectedIndex;
                return;
            }
            LoadSelectedServer();
            _oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            Server server = Configuration.GetDefaultServer();
            _modifiedConfiguration.Servers.Add(server);
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _modifiedConfiguration.Servers.Count - 1;
            _oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (_modifiedConfiguration.Servers.Count == 1)
            {
                MessageBox.Show("Must keep least one server");
                return;
            }

            _oldSelectedIndex = ServersListBox.SelectedIndex;
            if (_oldSelectedIndex >= 0 && _oldSelectedIndex < _modifiedConfiguration.Servers.Count)
            {
                _modifiedConfiguration.Servers.RemoveAt(_oldSelectedIndex);
            }
            if (_oldSelectedIndex >= _modifiedConfiguration.Servers.Count)
            {
                // can be -1
                _oldSelectedIndex = _modifiedConfiguration.Servers.Count - 1;
            }
            ServersListBox.SelectedIndex = _oldSelectedIndex;
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _oldSelectedIndex;
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            if (_modifiedConfiguration.Servers.Count == 0)
            {
                MessageBox.Show("Please add at least one server");
                return;
            }
            _controller.SaveServers(_modifiedConfiguration.Servers);
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigForm_Shown(object sender, EventArgs e)
        {
            IPTextBox.Focus();
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _controller.ConfigChanged -= controller_ConfigChanged;
            _controller.LogMessageReceived -= _controller_LogMessageReceived; 
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _controller.LogMessageReceived += _controller_LogMessageReceived;
        }

        void _controller_LogMessageReceived(object sender, LogEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

    }
}
