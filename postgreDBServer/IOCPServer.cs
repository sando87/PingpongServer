using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace postgreDBServer
{
    class IOCPServer
    {
        private const int PACKET_SIZE = 8 * 1024;
        static private ConcurrentDictionary<string, TcpClient> gClients = new ConcurrentDictionary<string, TcpClient>();
        async static public Task StartServerAsync(int portNumber)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, portNumber);
            listener.Start();
            while (true)
            {
                // 비동기 Accept                
                TcpClient tc = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

                // 새 쓰레드에서 처리
                ThreadPool.QueueUserWorkItem(AsyncTcpReceiver, tc);
            }
        }
        async static private void AsyncTcpReceiver(object o)
        {
            TcpClient tc = (TcpClient)o;
            IPEndPoint ep = (IPEndPoint)tc.Client.RemoteEndPoint;
            string ipAddress = ep.Address.ToString();
            gClients[ipAddress] = tc;
            FifoBuffer fifoBuf = new FifoBuffer();

            NetworkStream stream = null;
            try
            {
                int MAX_SIZE = 64 * 1024;  // 16KBytes
                stream = tc.GetStream();

                while (true)
                {
                    var buff = new byte[MAX_SIZE];
                    var nbytes = await stream.ReadAsync(buff, 0, buff.Length).ConfigureAwait(false);
                    if (nbytes <= 0)
                        break;

                    ProcRecvBuffer(tc, buff, nbytes, fifoBuf);
                    
                    //await stream.WriteAsync(returndata, 0, returndata.Length).ConfigureAwait(false);
                }
            }
            catch(Exception ex)
            {
                LOG.echo(ex.ToString());
            }

            if(stream != null)
                stream.Close();

            gClients[ipAddress] = null;
            fifoBuf.Clear();
            tc.Close();
        }
        static private byte[] ProcRecvBuffer(TcpClient tc, byte[] recvBuf, int  recvSize, FifoBuffer fifoBuf)
        {
            fifoBuf.Push(recvBuf, recvSize);

            while(true)
            {
                try
                {
                    byte[] buf = fifoBuf.readSize(fifoBuf.GetSize());
                    bool isError = false;
                    stHeader msg = stHeader.Parse(buf, ref isError);
                    if (isError)
                        fifoBuf.Clear();

                    if (msg == null)
                        break;
                    else
                        fifoBuf.Pop(msg.head.len);

                    IPEndPoint ep = (IPEndPoint)tc.Client.RemoteEndPoint;
                    string ipAddress = ep.Address.ToString();
                    int port = ep.Port;
                    stHeader.OnRecv(msg, ipAddress + ":" + port.ToString());
                }
                catch (Exception ex)
                { LOG.echo(ex.ToString()); }
            }
            return null;
        }
        static private bool SendToServer(byte[] data, string ipAddr)
        {
            if (!gClients.ContainsKey(ipAddr) || gClients[ipAddr] == null)
                return false;

            try
            {
                TcpClient tc = gClients[ipAddr];
                NetworkStream stream = tc.GetStream();
                int currentSize = data.Length;
                int off = 0;
                do
                {
                    int sendSize = Math.Min(currentSize, PACKET_SIZE);
                    stream.Write(data, off, sendSize);
                    off += sendSize;
                    currentSize -= sendSize;
                } while (currentSize > 0);

            }
            catch (Exception ex)
            {
                LOG.echo(ex.ToString());
                return false;
            }
            return true;
        }
        static public bool SendMsgToServer(stHeader msg, string ipAddr)
        {
            return SendToServer(msg.Serialize(), ipAddr);
        }
    }
}
