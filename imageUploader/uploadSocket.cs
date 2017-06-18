using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

namespace imageUploader
{
    class uploadSocket
    {
        TcpClient uploader;
        StreamWriter writer;
        StreamReader reader;
        Form1 form;
        public uploadSocket(string id,byte[] buf,Form1 form)
        {
            this.form = form;

            try
            {
                uploader = new TcpClient();
                uploader.Connect("localhost", 8888);
                NetworkStream stream = uploader.GetStream();
                writer = new StreamWriter(stream);
                reader = new StreamReader(stream);

                writer.WriteLine(id);
                writer.WriteLine(buf.Length);
                writer.Flush();
                System.Threading.Thread.Sleep(100);
                stream.Write(buf, 0, buf.Length);
                stream.Flush();
                form.showMessage("Upload success!\n" +reader.ReadLine());

                writer.Close();
                reader.Close();
                stream.Close();
                uploader.Close();
            }
            catch
            {
                form.showMessage("Error! Can't access server");
            }
            
        }
    }
}
