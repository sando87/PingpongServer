using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace postgreDBServer
{
    class NetworkClient
    {
        public static NetworkClient mInst = new NetworkClient();
        TcpClient mClient = null;
        bool isRunThread = false;
        public static NetworkClient Inst() { return mInst; }
        FifoBuffer mFifoBuffer = new FifoBuffer();
        public const int PACKET_SIZE = 16 * 1024;

        public bool ConnectAndRecv(string ip, int port)
        {
            if (mClient != null)
                return false;

            try
            {
                mClient = new TcpClient(ip, port);
                isRunThread = true;
                Task task = new Task(new Action(RunRecieve));
                task.Start();
            }
            catch(Exception ex)
            {
                LOG.echo(ex.ToString());
                return false;
            }

            return true;
        }

        void RunRecieve()
        {
            byte[] outbuf = new byte[PACKET_SIZE];
            int nbytes = 0;
            NetworkStream stream = mClient.GetStream();
            while (isRunThread)
            {
                try
                {
                    nbytes = stream.Read(outbuf, 0, outbuf.Length);
                    mFifoBuffer.Push(outbuf, nbytes);

                    while (true)
                    {
                        byte[] buf = mFifoBuffer.readSize(mFifoBuffer.GetSize());
                        bool isError = false;
                        stHeader msg = stHeader.Parse(buf, ref isError);
                        if (isError)
                            mFifoBuffer.Clear();

                        if (msg == null)
                            break;
                        else
                            mFifoBuffer.Pop(msg.head.len);

                        IPEndPoint ep = (IPEndPoint)mClient.Client.RemoteEndPoint;
                        string ipAddress = ep.Address.ToString();
                        int port = ep.Port;
                        string info = ipAddress + ":" + port.ToString();
                        stHeader.OnRecv.Invoke(msg, info);
                    }
                }
                catch (Exception ex)
                { LOG.echo(ex.ToString()); }
            }
            mFifoBuffer.Clear();
            stream.Close();
        }

        public bool SendToServer(byte[] data)
        {
            if (mClient == null)
                return false;

            try
            {
                NetworkStream stream = mClient.GetStream();
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
            catch(Exception ex)
            {
                LOG.echo(ex.ToString());
                return false;
            }
            return true;
        }
        public void Close()
        {
            if (mClient == null)
                return;

            isRunThread = false;
            NetworkStream st = mClient.GetStream();
            st.Close();
            mClient.Close();
            mClient = null;
        }
    }
}
