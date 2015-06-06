using System;
using System.IO;
using System.Net.Sockets;

namespace Xsocks.Controller
{
    public class Logging
    {
        public static string LogFile { get; set; }

        public static bool OpenLogFile()
        {
            try
            {
                string temppath = Path.GetTempPath();
                LogFile = Path.Combine(temppath, "xsocks.log");
                FileStream fs = new FileStream(LogFile, FileMode.Append);
                StreamWriterWithTimestamp sw = new StreamWriterWithTimestamp(fs);
                sw.AutoFlush = true;
                Console.SetOut(sw);
                Console.SetError(sw);

                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static void LogUsefulException(Exception e)
        {
            // just log useful exceptions, not all of them
            var exception = e as SocketException;
            if (exception != null)
            {
                SocketException se = exception;
                switch (se.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                        // closed by browser when sending
                        // normally happens when download is canceled or a tab is closed before page is loaded
                        break;
                    case SocketError.ConnectionReset:
                        // received rst
                        break;
                    case SocketError.NotConnected:
                        // close when not connected
                        break;
                    default:
                        Console.WriteLine(exception);
                        break;
                }
            }
            else
            {
                Console.WriteLine(e);
            }
        }
    }

    // Simply extended System.IO.StreamWriter for adding timestamp workaround
    public class StreamWriterWithTimestamp : StreamWriter
    {
        public StreamWriterWithTimestamp(Stream stream)
            : base(stream)
        {
        }

        private string GetTimestamp()
        {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(GetTimestamp() + value);
        }

        public override void Write(string value)
        {
            base.Write(GetTimestamp() + value);
        }
    }
}
