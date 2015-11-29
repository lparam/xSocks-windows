using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using xSocks.Model;

namespace xSocks.Controller
{
    public class SystemProxy
    {

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;
        private static bool _settingsReturn, _refreshReturn;

        public static void NotifyIE()
        {
            // These lines implement the Interface in the beginning of program 
            // They cause the OS to refresh the settings, causing IP to realy update
            _settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            _refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        public static void Update(Configuration config, bool forceDisable)
        {
            bool global = config.Global;
            bool enabled = config.Enabled;
            if (forceDisable)
            {
                enabled = false;
            }
            try
            {
                RegistryKey registry =
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                if (registry == null) return;
                if (enabled)
                {
                    if (global)
                    {
                        registry.SetValue("ProxyEnable", 1);
                        registry.SetValue("ProxyServer", "127.0.0.1:" + config.HttpProxyPort);
                        registry.SetValue("AutoConfigURL", "");
                    }
                    else
                    {
                        string pacUrl;
                        if (config.UseOnlinePac && !string.IsNullOrEmpty(config.PacUrl))
                            pacUrl = config.PacUrl;
                        else
                            pacUrl = "http://127.0.0.1:" + config.PACServerPort + "/pac?t=" + GetTimestamp(DateTime.Now);
                        registry.SetValue("ProxyEnable", 0);
                        registry.SetValue("ProxyServer", "");
                        registry.SetValue("AutoConfigURL", pacUrl);
                    }
                }
                else
                {
                    registry.SetValue("ProxyEnable", 0);
                    registry.SetValue("ProxyServer", "");
                    registry.SetValue("AutoConfigURL", "");
                }
                NotifyIE();
                //Must Notify IE first, or the connections do not chanage
                CopyProxySettingFromLan();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                // TODO this should be moved into views
                MessageBox.Show("Failed to update registry", "xSocks", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopyProxySettingFromLan()
        {
            RegistryKey registry =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", true);
            if (registry == null) return;
            var defaultValue = registry.GetValue("DefaultConnectionSettings");
            try
            {
                var connections = registry.GetValueNames();
                foreach (String each in connections)
                {
                    if (!(each.Equals("DefaultConnectionSettings")
                        || each.Equals("LAN Connection")
                        || each.Equals("SavedLegacySettings")))
                    {
                        //set all the connections's proxy as the lan
                        registry.SetValue(each, defaultValue);
                    }
                }
                NotifyIE();
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
    }
}
