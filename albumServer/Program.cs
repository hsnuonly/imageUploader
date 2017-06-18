using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Data.SQLite;


namespace albumServer
{
    class Program
    {
        TcpListener listener;
        TcpListener httpServer;
        Thread httpThread;
        long nextIndex;
        MD5 md5;
        SQLiteConnection sql_conn;
        SQLiteCommand sql_cmd;
        FileStream readIndexfs;
        StreamWriter readIndexSW;
        public Program()
        {
            md5 = MD5.Create();
            if (!File.Exists("hashtable.db"))
            {
                SQLiteConnection.CreateFile("hashtable.db");
            }
            sql_conn = new SQLiteConnection("Data source=hashtable.db");
            sql_conn.Open();
            sql_cmd = sql_conn.CreateCommand();

            sql_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS md5 (num INTEGER PRIMARY KEY AUTOINCREMENT, hash TEXT, file TEXT, user TEXT)";
            sql_cmd.ExecuteNonQuery();

            if (!Directory.Exists("images"))
            {
                Directory.CreateDirectory("images");
            }
            if (File.Exists("imageIndex"))
            {
                try
                {
                    readIndexfs = new FileStream("imageIndex", FileMode.OpenOrCreate);
                    readIndexfs.Seek(0, SeekOrigin.Begin);
                    StreamReader sr = new StreamReader(readIndexfs);
                    readIndexSW = new StreamWriter(readIndexfs);
                    nextIndex = Int64.Parse(sr.ReadLine());
                    sr.Close();
                }
                catch
                {
                    readIndexSW = new StreamWriter(readIndexfs);
                    readIndexSW.WriteLine(0);
                }
            }
            else
            {
                readIndexfs = new FileStream("imageIndex", FileMode.OpenOrCreate);
                readIndexfs.Seek(0, SeekOrigin.Begin);
                readIndexSW = new StreamWriter(readIndexfs);
                nextIndex = 0;
            }
            listener = new TcpListener(System.Net.IPAddress.Any, 8888);
            listener.Start();
            httpThread = new Thread(httpListen);
            httpThread.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(new ParameterizedThreadStart(client_handler));
                thread.Start(client);
            }
        }
        private void httpListen() {
            httpServer = new TcpListener(System.Net.IPAddress.Any,80);
            httpServer.Start();
            while (true)
            {
                Thread accepeter = new Thread(new ParameterizedThreadStart(httpAccept));
                accepeter.Start(httpServer.AcceptTcpClient());
            }
        }
        private void httpAccept(object obj)
        {
            try
            {
                TcpClient client = (TcpClient)obj;
                Stream stream = client.GetStream();
                StreamWriter writer = new StreamWriter(stream);
                StreamReader reader = new StreamReader(stream);

                string buf = reader.ReadLine();
                Console.WriteLine(buf);
                string path =  buf.Split(' ')[1].Remove(0,1);
                while (true)
                {
                    buf = reader.ReadLine();
                    if (buf == null || buf.Length <= 0) break;
                    Console.WriteLine(buf);
                }
                Console.WriteLine("===========");
                if (path.Contains(@"u/"))
                {
                    string id = path.Remove(0, 2);
                    string res = "";
                    Console.WriteLine(path);
                    Console.WriteLine(id);
                    sql_cmd.CommandText = "SELECT * FROM md5 WHERE user = '" + id + "'";
                    SQLiteDataReader sqlite_datareader = sql_cmd.ExecuteReader();
                    while (sqlite_datareader.Read())
                    {
                        res += sqlite_datareader["file"].ToString()+"\r\n";
                        Console.WriteLine(sqlite_datareader["num"].ToString());
                    }
                    sqlite_datareader.Close();

                    writer.WriteLine(@"HTTP/1.1 200 OK
                    Date: {0}
                    Last-Modified: {0}
                    Content-Length: {1}
                    Content-Type: text/html
                    Connection: Close

                    ", DateTime.Now.ToUniversalTime().ToString("r"),res.Length);
                    writer.WriteLine(res);
                    writer.Flush();
                }
                else
                {
                    if (!File.Exists(@"images\" + path)) path = "notfound.jpg";
                    FileStream fs = new FileStream(@"images\" + path, FileMode.Open);
                    byte[] res = new byte[fs.Length];
                    fs.Read(res, 0, res.Length);
                    fs.Close();
                    stream.Write(res, 0, res.Length);
                    stream.Flush();
                }
                client.Close();
            }
            catch
            {
                return;
            }
        }
        static void Main(string[] args)
        {
            Program prog = new Program();
        }
        
        void client_handler(object obj)
        {
            try
            {
                TcpClient client = (TcpClient)obj;
                NetworkStream stream = client.GetStream();
                StreamWriter writer = new StreamWriter(stream);
                StreamReader reader = new StreamReader(stream);
                string id = reader.ReadLine();
                int size = Int32.Parse(reader.ReadLine());
                byte[] buf = new byte[size];
                
                stream.Read(buf, 0, size);

                byte[] md5key = md5.ComputeHash(buf, 0, buf.Length);
                string md5str = "";
                foreach(byte b in md5key)
                {
                    md5str += b.ToString("x2");
                }

                sql_cmd.CommandText = "SELECT * FROM md5 WHERE hash = '"+md5str+"'"; 
                SQLiteDataReader sqlite_datareader = sql_cmd.ExecuteReader();
                string index = null;
                while (sqlite_datareader.Read())
                {
                    index = sqlite_datareader["file"].ToString();
                }
                sqlite_datareader.Close();

                if (index != null)
                {
                    writer.WriteLine(index);
                }
                else
                {
                    string path = System.Convert.ToBase64String(BitConverter.GetBytes(nextIndex));
                    path = path.Remove(path.Length - 1);
                    FileStream fs = new FileStream(@"images\" + path + ".jpg", FileMode.Create);
                    fs.Write(buf, 0, size);
                    fs.Close();
                    writer.WriteLine(path + ".jpg");

                    sql_cmd.CommandText = "INSERT INTO md5 (hash,file,user) VALUES ('" + md5str + "','" + path + ".jpg','"+id+"');";
                    sql_cmd.ExecuteNonQuery();

                    nextIndex++;
                    readIndexfs.Seek(0, SeekOrigin.Begin);
                    readIndexSW.WriteLine(nextIndex);
                }
                
                writer.Flush();
                stream.Close();
                reader.Close();
                writer.Close();
                client.Close();
            }
            catch
            {
                return;
            }
        }
    }
}
