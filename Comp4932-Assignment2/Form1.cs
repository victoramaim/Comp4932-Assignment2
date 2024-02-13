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

        private static double[,] conversionMatrix = new double[,]
        {
            {65.481, 128.553, 24.966},
            {-37.797, -74.203, 112},
             {112, -93.786, -18.214}
        };

        private static double[,] ReverseConversionMatrix = {
            { 0.00456621, 0.0000, 0.00625893 },
            { 0.00456621, -0.00153632, -0.00318811 },
            { 0.00456621, 0.00791071, 0.0000 }
        };

        private static double[] biasVector = new double[] { 16, 128, 128 };

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

        private void rGBToYCrCbToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RGBtoYCrCb(originalImage);
        }

        // Method to convert RGB image to YCrCb
        private void RGBtoYCrCb(Bitmap rgbImage)
        {
            int width = rgbImage.Width;
            int height = rgbImage.Height;

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

                    Y[x, y] = (conversionMatrix[0, 0] * r + conversionMatrix[0, 1] * g + conversionMatrix[0, 2] * b + biasVector[0]);
                    Cb[x, y] = conversionMatrix[1, 0] * r + conversionMatrix[1, 1] * g + conversionMatrix[1, 2] * b + biasVector[1];
                    Cr[x, y] = conversionMatrix[2, 0] * r + conversionMatrix[2, 1] * g + conversionMatrix[2, 2] * b + biasVector[2];
                }
            }

            double[,] CbSubsampled = Subsampling(Cb);
            double[,] CrSubsampled = Subsampling(Cr);

            byte[] YResult = ConvertToByteArray(Y);
            byte[] CbResult = ConvertToByteArray(CbSubsampled);
            byte[] CrResult = ConvertToByteArray(CrSubsampled);

            WriteToFile(width, height, YResult, CbResult, CrResult);
        }

        private double[,] Subsampling(double[,] arr)
        {
            int width = (int)Math.Ceiling(arr.GetLength(0) / 2.0);
            int height = (int)Math.Ceiling(arr.GetLength(1) / 2.0);
            double[,] array = new double[width, height];
            int counterx = 0;
            int countery = 0;
            for (int y = 0; y < arr.GetLength(1); y += 2)
            {
                for (int x = 0; x < arr.GetLength(0); x += 2)
                {
                    array[counterx, countery] = arr[x, y];
                    counterx++;
                }
                counterx = 0;
                countery++;
            }
            return array;
        }

        private void WriteToFile(int width, int height, byte[] y, byte[] cb, byte[] cr)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save YCrCb Image";
                saveFileDialog.Filter = "YCrCb Image|*.test|All files|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string savePath = saveFileDialog.FileName;

                    byte[] dimensios = new byte[4];
                    dimensios[0] = (byte)(width >> 8);
                    dimensios[1] = (byte)(width & 0xFF);
                    dimensios[2] = (byte)(height >> 8);
                    dimensios[3] = (byte)(height & 0xFF);

                    // Concatenate all the bytes into a single byte array
                    byte[] concatenatedBytes = ConcatenateBytes(dimensios, y, cb, cr);

                    // Write the concatenated bytes to the file
                    File.WriteAllBytes(savePath, concatenatedBytes);
                }
            }
        }

        // Helper method to convert a 2D array to a byte array
        private byte[] ConvertToByteArray(double[,] array)
        {
            int width = array.GetLength(0);
            int heigth = array.GetLength(1);
            byte[] byteArray = new byte[width * heigth];

            Buffer.BlockCopy(array, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        // Helper method to concatenate multiple byte arrays
        private byte[] ConcatenateBytes(params byte[][] arrays)
        {
            int totalLength = arrays.Sum(arr => arr.Length);
            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (var arr in arrays)
            {
                Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }
            return result;
        }

        private void yCrCbToRBGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open File";
                openFileDialog.Filter = "Image Files|*.test|All files|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] data = File.ReadAllBytes(openFileDialog.FileName);
                    ReadArray(data);
                }
            }
        }

        private void ReadArray(byte[] data)
        {
            // Zero at the moment
            int width = data[0] << 8 | data[1];   // Retrieve width from the stored bytes
            int height = data[2] << 8 | data[3];  // Retrieve height from the stored bytes

            double[,] Y = new double[width, height];
            int test= data.Length;

            int Subsampledwidth = (int)Math.Ceiling(Y.GetLength(0) / 2.0);
            int Subsampledheight = (int)Math.Ceiling(Y.GetLength(1) / 2.0);

            double[,] Subsampledcb = new double[Subsampledwidth, Subsampledheight];
            double[,] Subsampledcr = new double[Subsampledwidth, Subsampledheight];

            int index = 4;
            for (int y1 = 0;  y1 < height; y1++)
            {
                for (int x1= 0; x1 < height; x1++)
                {
                    Y[x1,y1] = data[index++];
                }
            }

            for (int y2 = 0; y2 < height/2; y2++)
            {
                for (int x2= 0; x2 < width/2; x2++)
                {
                    Subsampledcb[x2, y2] = data[index++];
                }
            }

            for (int y3 = 0; y3 < height/2; y3++)
            {
                for (int x3 = 0; x3 < width/2; x3++)
                {
                    Subsampledcr[x3, y3] = data[index++];
                }
            }

            double[,] Cb = Upsample(Subsampledcb);
            double[,] Cr = Upsample(Subsampledcr);

            YCrCbtoRGB(width, height, Y, Cb, Cr);
        }

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

        private Bitmap YCrCbtoRGB (int width, int height, double[,] Y, double[,] cb, double[,] cr)
        {
            Bitmap bitmap = new Bitmap(width, height);
            double r;
            double g;
            double b;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    r = (ReverseConversionMatrix[0,0] * (Y[x,y] - biasVector[0]) + (ReverseConversionMatrix[0,1] * (cb[x,y] - biasVector[1])) + (ReverseConversionMatrix[0,2] * (cr[x,y] - biasVector[2])));
                    g = (ReverseConversionMatrix[1,0] * (Y[x,y] - biasVector[0]) + (ReverseConversionMatrix[1,1] * (cb[x,y] - biasVector[1])) + (ReverseConversionMatrix[1,2] * (cr[x,y] - biasVector[2])));
                    b = (ReverseConversionMatrix[2,0] * (Y[x,y] - biasVector[0]) + (ReverseConversionMatrix[2,1] * (cb[x,y] - biasVector[1])) + (ReverseConversionMatrix[2,2] * (cr[x,y] - biasVector[2])));

                    // Clamp the RGB values to 0-255
                    r= (int)Math.Max(0, Math.Min(255, r));
                    g = (int)Math.Max(0, Math.Min(255, g));
                    b = (int)Math.Max(0, Math.Min(255, b));

                    Color pixel = Color.FromArgb((int)r, (int)g, (int)b);
                    bitmap.SetPixel(x,y,pixel);
                }
            }
            SaveBitmap(bitmap);
            return bitmap;
        }

        private void SaveBitmap(Bitmap bitmap)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save YCrCb Image";
                saveFileDialog.Filter = "YCrCb Image|*.bmp|All files|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string savePath = saveFileDialog.FileName;

                    // Write the concatenated bytes to the file
                    bitmap.Save(savePath);
                }
            }
        }

    }
}