using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Deadlocked.Server.Medius
{
    public class UDPSocket
    {
        private const int SioUdpConnreset = -1744830452;
        public const int ReceivePollingTime = 500000; //0.5 second

        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public Action<IPEndPoint, byte[]> OnReceive;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
            public bool Closed = false;
        }

        public void Server(int port)
        {
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.DontFragment = true;
            _socket.EnableBroadcast = true;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _socket.IOControl(SioUdpConnreset, new byte[] { 0 }, null);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public void Send(EndPoint target, byte[] data)
        {
            if (state.Closed)
                return;

            try
            {
                int result = _socket.SendTo(data, 0, data.Length, SocketFlags.None, target);
            }
            catch (SocketException ex)
            {
                switch (ex.SocketErrorCode)
                {
                    case SocketError.NoBufferSpaceAvailable:
                    case SocketError.Interrupted:
                        return;
                    case SocketError.MessageSize: //do nothing              
                        break;
                    default:
                        Console.WriteLine("[S]" + ex);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[S]" + ex);
                return;
            }
        }

        public void ReadAvailable()
        {
            int result;
            EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receiveBuffer = new byte[4096];

            if (state.Closed)
                return;

            //Reading data
            try
            {
                if (_socket.Available == 0 && !_socket.Poll(ReceivePollingTime, SelectMode.SelectRead))
                    return;

                result = _socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref bufferEndPoint);
            }
            catch (SocketException ex)
            {
                switch (ex.SocketErrorCode)
                {
                    case SocketError.Interrupted:
                    case SocketError.NotSocket:
                        return;
                    case SocketError.ConnectionReset:
                    case SocketError.MessageSize:
                    case SocketError.TimedOut:
                        Console.WriteLine($"[R]Ignored error: {(int)ex.SocketErrorCode} - {ex}");
                        break;
                    default:
                        Console.WriteLine($"[R]Error code: {(int)ex.SocketErrorCode} - {ex}");
                        break;
                }
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            //All ok!
            //Console.WriteLine($"[R]Received data from {bufferEndPoint}, result: {result}");

            if (result > 0)
            {
                byte[] copy = new byte[result];
                Array.Copy(receiveBuffer, 0, copy, 0, result);
                OnReceive?.Invoke(bufferEndPoint as IPEndPoint, copy);
            }
        }

        public void Stop()
        {
            state.Closed = true;
            _socket.Close();
        }
    }
}
