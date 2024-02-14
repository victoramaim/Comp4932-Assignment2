using Microsoft.VisualBasic;
using System.Drawing.Imaging;
using System.Xml.Linq;
using static System.Windows.Forms.LinkLabel;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms;

namespace Comp4932_Assignment2
{
    public partial class Form1 : Form
    {
        protected Bitmap? displayImage;
        protected Bitmap? originalImage;

        // Matrix used for the compression
        private static readonly double[,] forwardMatrix = 
        {
            { 0.299, 0.587, 0.114 },
            { -0.168736, -0.331264, 0.5 },
            { 0.5, -0.418688, -0.081312 }
        };

        // Matrix used for the decompression
        private static readonly double[,] backwardMatrix = {
            { 1, 0, 1.4 },
            { 1, -0.343, -0.711 },
            { 1, 1.765, 0 }
        };

        private static int biasVector = 128;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (displayImage != null)
            {
                e.Graphics.DrawImage(displayImage, 0, 0, ClientSize.Width, ClientSize.Height);
            }

        }

        // Resize the Bitmap to the window size
        private Bitmap ResizeBitmap(Bitmap originalBitmap, int width, int height)
        {
            Bitmap resizedBitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resizedBitmap))
            {
                g.DrawImage(originalBitmap, new Rectangle(0, 0, width, height));
            }
            return resizedBitmap;
        }

        // Open the picture
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open Image";
                openFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif;*.tiff|All files|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    originalImage = new Bitmap(openFileDialog.FileName);

                    displayImage = ResizeBitmap(originalImage, ClientSize.Width, ClientSize.Height);
                    Refresh();
                }
            }
        }

        // Convert the RGB to YCrCb
        private void rGBToYCrCbToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RGBtoYCrCb(originalImage);
        }

        // Method to convert RGB image to YCrCb
        private void RGBtoYCrCb(Bitmap rgbImage)
        {
            int width = rgbImage.Width;
            int height = rgbImage.Height;
            byte[] data = new byte[(int)(width * height * 1.5F + 4 + 3)];

            double[,] Y = new double[width, height];
            double[,] Cb = new double[width, height];
            double[,] Cr = new double[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = rgbImage.GetPixel(x, y);

                    double r = pixel.R;
                    double g = pixel.G;
                    double b = pixel.B;

                    Y[x, y] = forwardMatrix[0, 0] * r + forwardMatrix[0, 1] * g + forwardMatrix[0, 2] * b;
                    Cb[x, y] = forwardMatrix[1, 0] * r + forwardMatrix[1, 1] * g + forwardMatrix[1, 2] * b + biasVector;
                    Cr[x, y] = forwardMatrix[2, 0] * r + forwardMatrix[2, 1] * g + forwardMatrix[2, 2] * b + biasVector;
                }
            }

            Cb = Subsampling(Cb);
            Cr = Subsampling(Cr);

            int i = 0;
            data[i++] = (byte)(width >> 8);     // Store the most significant byte of width
            data[i++] = (byte)(width & 0xFF);   // Store the least significant byte of width
            data[i++] = (byte)(height >> 8);    // Store the most significant byte of height
            data[i++] = (byte)(height & 0xFF);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    data[i++] = (byte)(Y[x, y]);
                }
            }
            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width / 2; x++)
                {
                    data[i++] = (byte)(Cb[x, y]);
                }
            }
            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width / 2; x++)
                {
                    data[i++] = (byte)(Cr[x, y]);
                }
            }

            WriteToFile(data);
        }

        // Subample for both Cr and Cb
        private double[,] Subsampling(double[,] arr)
        {
            int width = (int)Math.Ceiling(arr.GetLength(0) / 2.0);
            int height = (int)Math.Ceiling(arr.GetLength(1) / 2.0);
            double[,] array = new double[width, height];

            for (int y = 0; y < height; y ++)
            {
                for (int x = 0; x < width; x ++)
                {
                    array[x, y] = arr[x * 2, y * 2];
                }
            }
            return array;
        }

        // Save the compressed array to a custom file
        private void WriteToFile(byte[] array)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save YCrCb Image";
                saveFileDialog.Filter = "YCrCb Image|*.victor|All files|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string savePath = saveFileDialog.FileName;
                    File.WriteAllBytes(savePath, array);
                }
            }
        }

        //Open the custom file to decompress
        private void yCrCbToRBGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open File";
                openFileDialog.Filter = "Image Files|*.victor|All files|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] data = File.ReadAllBytes(openFileDialog.FileName);
                    ReadArray(data);
                }
            }
        }

        // Read the custom file
        private void ReadArray(byte[] data)
        {
            int width = data[0] << 8 | data[1];   // Retrieve width from the stored bytes
            int height = data[2] << 8 | data[3];  // Retrieve height from the stored bytes

            double[,] Y = new double[width, height];

            int Subsampledwidth = (int)Math.Ceiling(Y.GetLength(0) / 2.0);
            int Subsampledheight = (int)Math.Ceiling(Y.GetLength(1) / 2.0);

            double[,] Cb = new double[Subsampledwidth, Subsampledheight];
            double[,] Cr = new double[Subsampledwidth, Subsampledheight];

            int index = 4;
            for (int y = 0;  y < height; y++)
            {
                for (int x= 0; x < height; x++)
                {
                    Y[x,y] = data[index++];
                }
            }

            for (int y = 0; y < height/2; y++)
            {
                for (int x = 0; x < width/2; x++)
                {
                    Cb[x, y] = data[index++];
                }
            }

            for (int y = 0; y < height/2; y++)
            {
                for (int x = 0; x < width/2; x++)
                {
                    Cr[x, y] = data[index++];
                }
            }

            Cb = Upsample(Cb);
            Cr = Upsample(Cr);

            YCrCbtoRGB(width, height, Y, Cb, Cr);
        }

        // Upsample the compressed Cr and Cb
        private double[,] Upsample(double[,] data)
        {
            int width = data.GetLength(0) * 2;
            int height = data.GetLength(1) * 2;
            double[,] result = new double[width, height];

            int countery = 0;
            for (int y = 0; y < height/2; y++)
            {
                int counterx = 0;
                for (int x = 0; x < width/2; x++)
                {
                    result[counterx, countery] = data[x,y];
                    result[counterx, countery + 1] = data[x,y];
                    result[counterx + 1, countery] = data[x, y];
                    result[counterx + 1, countery + 1] = data[x,y];
                    counterx += 2;
                }
                countery += 2;
            }
            return result;
        }

        // Convert the compressed data to a picture (Bitmap)
        private Bitmap YCrCbtoRGB (int width, int height, double[,] Y, double[,] Cb, double[,] Cr)
        {
            Bitmap bitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double r = backwardMatrix[0,0] * Y[x,y] + backwardMatrix[0,1] * (Cb[x,y] - biasVector) + backwardMatrix[0,2] * (Cr[x,y] - biasVector);
                    double g = backwardMatrix[1,0] * Y[x,y] + backwardMatrix[1,1] * (Cb[x,y] - biasVector) + backwardMatrix[1,2] * (Cr[x,y] - biasVector);
                    double b = backwardMatrix[2,0] * Y[x,y] + backwardMatrix[2,1] * (Cb[x,y] - biasVector) + backwardMatrix[2,2] * (Cr[x,y] - biasVector);

                    // Clamp the RGB values to 0-255
                    r = Math.Max(0, Math.Min(255, r));
                    g = Math.Max(0, Math.Min(255, g));
                    b = Math.Max(0, Math.Min(255, b));

                    Color pixel = Color.FromArgb((int)r, (int)g, (int)b);
                    bitmap.SetPixel(x,y,pixel);
                }
            }
            SaveBitmap(bitmap);
            return bitmap;
        }

        // Save the decompressed picture (Bitmap)
        private void SaveBitmap(Bitmap bitmap)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save YCrCb Image";
                saveFileDialog.Filter = "YCrCb Image|*.bmp|All files|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string savePath = saveFileDialog.FileName;
                    bitmap.Save(savePath);
                }
            }
        }
    }
}