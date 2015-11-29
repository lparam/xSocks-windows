using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using xSocks.Controller;
using xSocks.Model;

namespace xSocks
{
    public class Listener
    {
        public interface IService
        {
            bool Handle(byte[] firstPacket, int length, Socket socket);
        }

        private Socket _socket;
        private readonly IList<IService> _services;

        public Listener(IList<IService> services)
        {
            _services = services;
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }

        public void Start(int port, bool share)
        {
            if (CheckIfPortInUse(port))
            {
                throw new Exception("Port already in use");
            }

            try
            {
                // Create a TCP/IP socket.
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                if (share)
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, port);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                }

                // Bind the socket to the local endpoint and listen for incoming connections.
                _socket.Bind(localEndPoint);
                _socket.Listen(1024);

                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("xSocks started");
                _socket.BeginAccept(AcceptCallback, _socket);
            }
            catch (SocketException)
            {
                _socket.Close();
                throw;
            }
        }

        public void Stop()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);

                byte[] buf = new byte[4096];
                object[] state = {
                    conn,
                    buf
                };

                conn.BeginReceive(buf, 0, buf.Length, 0, ReceiveCallback, state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                try
                {
                    listener.BeginAccept(AcceptCallback, listener);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                }
            }
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;

            Socket conn = (Socket)state[0];
            byte[] buf = (byte[])state[1];
            try
            {
                int bytesRead = conn.EndReceive(ar);
                foreach (IService service in _services)
                {
                    if (service.Handle(buf, bytesRead, conn))
                    {
                        return;
                    }
                }
                // no service found for this
                // shouldn't happen
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                conn.Close();
            }
        }
    }
}
