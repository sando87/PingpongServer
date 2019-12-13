using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace postgreDBServer
{
    class DBSession : IDisposable
    {
        static private DBSession mInst = new DBSession();
        private NpgsqlConnection mDBSession;
        private object mLockObject = new object();
        private DBSession() { }
        public void Dispose()
        {
            mDBSession.Close();
            mDBSession = null;
        }
        static public DBSession Inst() { return mInst; }
        static public DBSession Open(string hostIP, string database, string userid, string password)
        {
            if (mInst.IsOpen())
                return mInst;

            string connectParam =
                "host=" + hostIP +
                ";database=" + database +
                ";username=" + userid +
                ";password=" + password;
            mInst.mDBSession = new NpgsqlConnection(connectParam);
            try
            {
                mInst.mDBSession.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return mInst.IsOpen() ? mInst : null;
        }

        public bool IsOpen()
        {
            if (mDBSession != null && mDBSession.State.ToString() == "Open")
                return true;

            return false;
        }

        public bool GetUserInfo(string userID, ref UserInfo info)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("SELECT * FROM users WHERE userid = '{0}'", userID);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            info.username = reader["userid"].ToString();
                            info.password = reader["password"].ToString();
                            info.devicename = reader["devicename"].ToString();
                            info.score = int.Parse(reader["score"].ToString());
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public bool AddNewUser(string userID, string password)
        {
            if (IsSameUserID(userID))
                return false;

            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("INSERT INTO users VALUES('{0}', '{1}', 0, now(), now())", userID, password);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public bool AddLogging(string username, bool isLogIN)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("INSERT INTO logging VALUES('{0}', {1}, now())", username, isLogIN ? 1 : 2);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public UserInfo CreateNewUser(string devicename)
        {
            int nextUserID = GetMusicInfoNextID("seqUserID");
            if (nextUserID < 0)
                return null;

            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    UserInfo info = new UserInfo();
                    info.username = "user#" + nextUserID;
                    info.password = "password";
                    info.devicename = devicename;
                    string query = String.Format("INSERT INTO users VALUES('{0}', '{1}', '{2}', 0, now(), now())", info.username, info.password, info.devicename);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        return info;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return null;
        }
        public bool IsSameUserID(string userID)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("SELECT * FROM users WHERE userid = '{0}'", userID);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                            return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public bool UpdateScore(string userID, int score)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("UPDATE users SET score={0} WHERE userid='{1}'", score, userID);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        

        public Song[] GetMusicLists()
        {
            List<Song> list = new List<Song>();
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    cmd.Connection = mDBSession;
                    cmd.CommandText = "select * from musics";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Song info = new Song();
                            info.DBID = int.Parse(reader["id"].ToString());
                            info.Title = reader["title"].ToString();
                            info.Artist = reader["artist"].ToString();
                            info.UserID = reader["userid"].ToString();
                            info.FileNameNoExt = reader["filename"].ToString();
                            list.Add(info);
                        }
                            
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return list.ToArray();
        }
        public bool UpdateMusicInfo(Song info)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string values = 
                        "title='" + info.Title +
                        "', artist='" + info.Artist +
                        "', userid='" + info.UserID +
                        "', filename='" + info.FileNameNoExt +
                        "', updatetime=now()";
                    string query = String.Format("UPDATE musics SET {0} WHERE id='{1}'", values, info.DBID);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public bool AddMusicInfo(ref Song info)
        {
            int nextMusicID = GetMusicInfoNextID("seqMusicID");
            if (nextMusicID < 0)
                return false;

            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string values = nextMusicID.ToString() +
                        ",'" + info.Title +
                        "','" + info.Artist +
                        "','" + info.UserID +
                        "','" + info.FileNameNoExt +
                        "', now(), now()";
                    string query = String.Format("INSERT INTO musics(id, title, artist, userid, filename, createtime, updatetime) VALUES({0})", values);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        info.DBID = nextMusicID;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public bool GetMusicInfo(int id, ref Song info)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("SELECT * FROM musics WHERE id = '{0}'", id);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            info.DBID = int.Parse(reader["id"].ToString());
                            info.Title = reader["title"].ToString();
                            info.Artist = reader["artist"].ToString();
                            info.UserID = reader["userid"].ToString();
                            info.FileNameNoExt = reader["filename"].ToString();
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return false;
        }
        public int GetMusicInfoNextID(string seqName)
        {
            using (var cmd = new NpgsqlCommand())
            {
                try
                {
                    string query = String.Format("SELECT * FROM nextval('{0}')", seqName);
                    cmd.Connection = mDBSession;
                    cmd.CommandText = query;
                    int val = Convert.ToInt32(cmd.ExecuteScalar());
                    return val;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return -1;
        }


        //public bool Test(int id)
        //{ 
        //    using (var cmd = new NpgsqlCommand())
        //    {
        //        try
        //        {
        //            Random rand = new Random();
        //           // for (int i = 1000; i < 10000; ++i)
        //            {
        //                int random = rand.Next() % 100;
        //                string query = String.Format("INSERT INTO emp VALUES('sjlee', {0})", random);
        //                cmd.Connection = mDBSession;
        //                cmd.CommandText = query;
        //                lock(mLockObject)
        //                {
        //                    using (var reader = cmd.ExecuteReader())
        //                    {
        //                        bool ret = reader.Read();
        //                        Console.WriteLine("*********{0}**********", id);
        //                        //continue;
        //                    }
        //
        //                }
        //
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("========={0}=========", id);
        //            Console.WriteLine(ex.ToString());
        //        }
        //    }
        //    return false;
        //}
        //public bool TestA(int id, int random)
        //{
        //    using (var cmd = new NpgsqlCommand())
        //    {
        //        try
        //        {
        //            string query = String.Format("UPDATE test SET " +
        //                "nameA = 'thA_{0}', nameB = 'thB_{0}', nameC = 'thC_{0}', nameD = 'thD_{0}' " +
        //                "WHERE random={1};", id, random);
        //            cmd.Connection = mDBSession;
        //            cmd.CommandText = query;
        //            using (var reader = cmd.ExecuteReader()) ;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("ERR: " + id);
        //            //Console.WriteLine(ex.ToString());
        //        }
        //    }
        //    return false;
        //}
        //public bool PrintRandom(int rand)
        //{
        //    using (var cmd = new NpgsqlCommand())
        //    {
        //        try
        //        {
        //            string query = String.Format("SELECT * FROM musics", rand);
        //            cmd.Connection = mDBSession;
        //            cmd.CommandText = query;
        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                int cnt = 0;
        //                while (reader.Read())
        //                {
        //                    string nameA = reader["title"].ToString();
        //                    string nameB = reader["artist"].ToString();
        //                    string nameC = reader["userid"].ToString();
        //                    string nameD = reader["id"].ToString();
        //                    cnt++;
        //                    Console.WriteLine(nameA + "=" + nameB + "=" + nameC + "=" + nameD + ";");
        //                    if (cnt > 10)
        //                        break;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("ERR Print");
        //            //Console.WriteLine(ex.ToString());
        //        }
        //    }
        //    return false;
        //}
    }
}
