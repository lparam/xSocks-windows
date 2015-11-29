using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace xSocks.Controller
{
    class AutoStartup
    {
        public static void Set(bool enabled)
        {
            string path = Application.ExecutablePath;
            RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (runKey != null)
            {
                if (enabled)
                {
                    runKey.SetValue("xSocks", path);
                }
                else
                {
                    runKey.DeleteValue("xSocks");
                }
                runKey.Close();
            }
        }

        public static bool Check()
        {
            try
            {
                RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (runKey != null)
                {
                    string[] runList = runKey.GetValueNames();
                    runKey.Close();
                    foreach (string item in runList)
                    {
                        if (item.Equals("xSocks"))
                            return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return false;
            }
        }
    }
}
