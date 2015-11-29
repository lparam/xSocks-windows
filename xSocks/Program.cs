using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using xSocks.Controller;
using xSocks.View;

namespace xSocks
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Utils.ReleaseMemory();
            using (Mutex mutex = new Mutex(false, "Global\\" + "71904632-A427-497F-AB91-241CD477EC1F"))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("xSocks is already running.\nFind xSocks icon in notify tray.",
                        "xSocks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Directory.SetCurrentDirectory(Application.StartupPath);
#if !DEBUG
                Logging.OpenLogFile();
#endif
                xSocksController controller = new xSocksController();
                controller.Start();
                Application.Run(new xSocksApplicationContext(controller));
            }
        }
    }
}
