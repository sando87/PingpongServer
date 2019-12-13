using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using System.Threading;
using System.IO;

namespace postgreDBServer
{
    public class PathDefines
    {
        public const string PATH_MUISC_RES_FILE = "D:\\musics\\";
    }
    public partial class Form1 : Form
    {
        bool[] done = new bool[10];
        static ManualResetEvent manualEvent = new ManualResetEvent(false);
        DBSession mDB;
        NetworkClient client = null;
        public Form1()
        {
            InitializeComponent();
            Task task = IOCPServer.StartServerAsync(9435);
            mDB = DBSession.Open("localhost", "postgres", "postgres", "root");
            stHeader.OnRecv += ProcRecvServer;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if(client == null)
            {
                client = new NetworkClient();
                if (client.ConnectAndRecv("127.0.0.1", 9435))
                {
                    btnConnect.Text = "DisConnect";
                }
                else
                {
                    MessageBox.Show("Fail Connect!!");
                    client = null;
                }
            }
            else
            {
                client.Close();
                client = null;
                btnConnect.Text = "Connect";
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if(client != null)
            {
                CMD_SongFile msg = new CMD_SongFile();

                byte[] buf = File.ReadAllBytes("E:\\test.txt");
                msg.stream.AddRange(buf);

                msg.song.DBID = 14;
                msg.song.Title = "ttt";
                msg.song.Artist = "aaa";
                msg.song.UserID = "kim";
                msg.song.FilePath = PathDefines.PATH_MUISC_RES_FILE;
                msg.song.FileNameNoExt = "test";

                msg.FillHeader(ICDDefines.CMD_Upload);

                client.SendToServer(msg.Serialize());
            }
        }

        public stHeader ProcRecvServer(stHeader _msg, string _info)
        {
            DBSession db = DBSession.Inst();
            string ipAddr = _info.Split(':')[0];

            if (_msg.head.cmd == ICDDefines.CMD_Download)
            {
                CMD_SongFile msg = (CMD_SongFile)_msg;
                CMD_SongFile recvMsg = new CMD_SongFile();
                db.GetMusicInfo(msg.song.DBID, ref recvMsg.song);
                string fullpath = PathDefines.PATH_MUISC_RES_FILE + recvMsg.song.UserID + "\\";
                byte[] meta = File.ReadAllBytes(fullpath + recvMsg.song.FileNameNoExt + ".bytes");
                Utils.Deserialize(ref recvMsg.song, meta);
                byte[] buf = File.ReadAllBytes(fullpath + recvMsg.song.FileNameNoExt + ".mp3");
                recvMsg.stream.AddRange(buf);
                recvMsg.FillHeader(ICDDefines.CMD_Download, true);
                IOCPServer.SendMsgToServer(recvMsg, ipAddr);
                return recvMsg;
                
            }
            else if (_msg.head.cmd == ICDDefines.CMD_Upload)
            {
                CMD_SongFile msg = (CMD_SongFile)_msg;
                Song dbResult = new Song();
                bool isExist = db.GetMusicInfo(msg.song.DBID, ref dbResult);
                bool ret = false;
                if (isExist)
                    ret = db.UpdateMusicInfo(msg.song);
                else
                    ret = db.AddMusicInfo(ref msg.song);

                if (ret)
                {
                    string fullpath = PathDefines.PATH_MUISC_RES_FILE + msg.song.UserID + "\\";
                    File.WriteAllBytes(fullpath + msg.song.FileNameNoExt + ".bytes", Utils.Serialize(msg.song));
                    File.WriteAllBytes(fullpath + msg.song.FileNameNoExt + ".mp3", msg.stream.ToArray());
                }
                msg.stream.Clear();
                msg.FillHeader(ICDDefines.CMD_Upload, true);
                msg.head.ack = ret ? ICDDefines.ACK_REP : ICDDefines.ACK_ERR;
                IOCPServer.SendMsgToServer(msg, ipAddr);
            }
            else if(_msg.head.cmd == ICDDefines.CMD_MusicList)
            {
                CMD_MusicList recvMsg = new CMD_MusicList();
                Song[] list = db.GetMusicLists();
                for(int i = 0; i < list.Length; ++i)
                    recvMsg.musics.Add(list[i]);
                recvMsg.FillHeader(ICDDefines.CMD_MusicList, true);
                IOCPServer.SendMsgToServer(recvMsg, ipAddr);
                return recvMsg;
            }
            else if (_msg.head.cmd == ICDDefines.CMD_NewUser)
            {
                CMD_UserInfo msg = (CMD_UserInfo)_msg;
                CMD_UserInfo recvMsg = new CMD_UserInfo();
                recvMsg.body = db.CreateNewUser(msg.body.devicename);
                recvMsg.FillHeader(ICDDefines.CMD_NewUser, true);
                CreateFolder(recvMsg.body.username);
                IOCPServer.SendMsgToServer(recvMsg, ipAddr);
                return recvMsg;
            }
            else if (_msg.head.cmd == ICDDefines.CMD_LoggingUser)
            {
                CMD_UserInfo msg = (CMD_UserInfo)_msg;
                CMD_UserInfo recvMsg = new CMD_UserInfo();
                bool isLogIN = msg.body.reserve == 1 ? true : false;
                db.AddLogging(msg.body.username, isLogIN);
                recvMsg.FillHeader(ICDDefines.CMD_LoggingUser, true);
                IOCPServer.SendMsgToServer(recvMsg, ipAddr);
                return recvMsg;
            }

            return null;
        }
        public stHeader ProcRecvClient(stHeader _msg, string _info)
        {
            return null;
        }
        public void CreateFolder(string foldername)
        {
            DirectoryInfo di = new DirectoryInfo(PathDefines.PATH_MUISC_RES_FILE + foldername);
            if (di.Exists)
                di.Delete();
            di.Create();
        }
    }
}
