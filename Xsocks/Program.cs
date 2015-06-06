using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Xsocks.Controller;
using Xsocks.View;

namespace Xsocks
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
                    MessageBox.Show("Xsocks is already running.\nFind Xsocks icon in notify tray.",
                        "Xsocks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Directory.SetCurrentDirectory(Application.StartupPath);
#if !DEBUG
                Logging.OpenLogFile();
#endif
                XsocksController controller = new XsocksController();
                controller.Start();
                Application.Run(new XsocksApplicationContext(controller));
            }
        }
    }
}
