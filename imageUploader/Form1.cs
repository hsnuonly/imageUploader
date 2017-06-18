using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Media.Imaging;

namespace imageUploader
{
    public partial class Form1 : Form
    {
        Image bufferedImage;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                bufferedImage = Clipboard.GetImage();
                pictureBox1.Image = bufferedImage;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "jpg|*.jpg|png|*.png";
            ofd.FilterIndex = 0;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                bufferedImage = new Bitmap(ofd.FileName);
                pictureBox1.Image = bufferedImage;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (bufferedImage == null) return;
            Thread encodeThread = new Thread(encode);
            encodeThread.Start();
        }
        private void encode()
        {            
            Bitmap bitmap = new Bitmap(bufferedImage);
            BitmapSource bf = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 50;
            encoder.Frames.Add(BitmapFrame.Create(bf));
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            //System.IO.FileStream fs = new System.IO.FileStream("R:\\client.jpg", System.IO.FileMode.Create);
            encoder.Save(ms);
            //byte[] buf = ms.ToArray();
            //fs.Write(buf, 0, buf.Length);
            uploadSocket uploader = new uploadSocket(idTextbox.Text,ms.ToArray(),this);
        }
        public void showMessage(string text)
        {
            MessageBox.Show(text);
        }
    }
}
