using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab5
{
    public partial class Form1 : Form
    {

        Bitmap origImage;
        public Form1()
        {
            InitializeComponent();
        }

        #region Элементы управления
        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            label10.Text = "Размер маски = " + Convert.ToString(trackBar5.Value);
        }
        private void дилатацияtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = toBitmap(EroseImage(toArray(pictureBox1.Image),
                                                    trackBar5.Value, 255));
        }
        private void построениеОстоваToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = toBitmap(Skeleton(toArray(pictureBox1.Image),
                                                    trackBar5.Value, 255));
        }

        private void выпуклаяОболочкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = toBitmap(ConvexShell(toArray(pictureBox1.Image), 255));
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            var res = toArray(origImage);
            FillingArea(res, pos.X, pos.Y, 100);
            pictureBox2.Image = toBitmap(res);
        }

        private void загрузитьИзображениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Image image = Image.FromFile(dialog.FileName);
                origImage = new Bitmap(image, pictureBox1.Width, pictureBox1.Height);
                pictureBox1.Image = origImage;
                морфологическаяОбработкаToolStripMenuItem.Enabled = true;
            }
        }
        #endregion

        #region Вспомогательные методы

        public byte[,] toArray(Image image)
        {
            byte[,] res = new byte[image.Width, image.Height];
            Bitmap bmp10 = new Bitmap(image);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    res[x, y] = (byte)(bmp10.GetPixel(x, y).GetBrightness() * 255 > 100?255:0);
                }
            }
            return res;
        }

        public Bitmap toBitmap(byte[,] byteMass)
        {
            int width = byteMass.GetLength(0);
            int height = byteMass.GetLength(1);
            Bitmap res = new Bitmap(width, height);
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)

                {
                    Color color = Color.FromArgb(byteMass[x, y],
                        byteMass[x, y],
                        byteMass[x, y]);
                    res.SetPixel(x, y, color);
                }
            return res;
        }
        #endregion

        #region Морфологическая обработка
        private byte HM(byte[,] image, byte[,] mask, int x, int y, byte brightness)
        {
            for (int i = -1; i <= 1; ++i)
                for (int j = -1; j <= 1; ++j)
                {
                    if (mask[i+1, j+1] == 1 && image[x + i, y + j] != brightness)
                        return image[x, y];
                    if (mask[i + 1, j + 1] == 0 && image[x + i, y + j] == brightness)
                        return image[x, y];
                }
            return brightness;
        }
            
        private byte[,] HMImage(byte[,] image, byte[,] mask, byte brightness)
        {
            byte[,] res = (byte[,])image.Clone();
            bool f = true;
            while (f)
            {
                f = false;
                for (int x = 1; x < pictureBox1.Width - 1; x++)
                    for (int y = 1; y < pictureBox1.Height - 1; y++)
                    {
                        byte oldVal = res[x, y];
                        res[x, y] = HM(res, mask, x, y, brightness);
                        if (oldVal != res[x, y])
                            f = true;
                    }
            }

            return res;
        }

        private byte[,] ConvexShell(byte[,] image, byte brightness)
        {
            byte[,] mask = { { 1, 2, 2},
                             { 1, 0, 2},
                             { 1, 2, 2} };
            var res = HMImage(image, mask, brightness);
            mask = new byte[,]{ { 1, 1, 1},
                                { 2, 0, 2},
                                { 2, 2, 2} };
            res = HMImage(res, mask, brightness);
            mask = new byte[,]{ { 2, 2, 1},
                                { 2, 0, 1},
                                { 2, 2, 1} };
            res = HMImage(res, mask, brightness);
            mask = new byte[,]{ { 2, 2, 2},
                                { 2, 0, 2},
                                { 1, 1, 1} };
            return HMImage(res, mask, brightness);
        }

        private byte[,] EroseImage(byte[,] originalBright, int border, byte brightness)
        {
            byte[,] res = new byte[pictureBox1.Width, pictureBox1.Height];
            for (int x = border; x < pictureBox1.Width - border; x++)
                for (int y = border; y < pictureBox1.Height - border; y++)
                    res[x, y] = Erose(originalBright, x, y, border, brightness);
            return res;
        }

        private byte Erose(byte[,] image, int x, int y, int border, byte brightness)
        {
            for (int m = x - border / 2 - 1; m <= x + border / 2 + 1; m++)
                for (int n = y - border / 2 - 1; n <= y + border / 2 + 1; n++)
                    if (image[m, n] == brightness)
                        return brightness;
            return image[x,y];
        }

        private byte[,] OpeningClosing(byte[,] originalBright, int border)
        {
            return EroseImage(EroseImage(originalBright, border, 0), border, 255);
        }

        private byte[,] Skeleton(byte[,] originalBright, int border, byte brightness)
        {
            byte[,] erosenImg = (byte[,])originalBright.Clone();
            byte[,] OCbright;
            byte[,] res = new byte[pictureBox1.Width, pictureBox1.Height];
            byte diff;
            int flag = 1;
            while (flag != 0)
            {
                flag = 0;
                erosenImg = EroseImage(erosenImg, border, 0);
                OCbright = OpeningClosing(erosenImg, border);
                for (int x = border; x < pictureBox1.Width - border; x++)
                    for (int y = border; y < pictureBox1.Height - border; y++)
                    {
                        diff = (byte)(erosenImg[x, y] - OCbright[x, y]);
                        if (diff == brightness)
                        {
                            flag++;
                        }
                        res[x, y] += diff;
                    }
            }
            return res;
        }
        
        void FillingArea(byte[,] originalBright, int x, int y, byte brightness)
        {
            Queue<Point> nextPoints = new Queue<Point>();
            HashSet<Point> processedPoints = new HashSet<Point>();
            nextPoints.Enqueue(new Point(x, y));
            Point currentPoint;
            Point nextPoint;
            byte origBrightness = originalBright[x, y];

            while (nextPoints.Count != 0)
            {
                currentPoint = nextPoints.Dequeue();
                originalBright[currentPoint.X, currentPoint.Y] = brightness;
                processedPoints.Add(currentPoint);
                if (currentPoint.X - 2 < pictureBox1.Width &&
                    originalBright[currentPoint.X - 2, currentPoint.Y] != origBrightness)
                    currentPoint = currentPoint;
                for (int i = -1; i <= 1; ++i)
                    for (int j = -1; j <= 1; ++j)
                    {
                        if (i == 0 && j == 0) continue;
                        nextPoint = new Point(currentPoint.X + i, currentPoint.Y + j);
                        if (nextPoint.X > 0 && nextPoint.X < pictureBox1.Width &&
                            nextPoint.Y > 0 && nextPoint.Y < pictureBox1.Height &&
                            originalBright[nextPoint.X, nextPoint.Y] == origBrightness &&
                            !processedPoints.Contains(nextPoint) && !nextPoints.Contains(nextPoint))
                            nextPoints.Enqueue(nextPoint);
                    }
            }
        }
        #endregion
    }
}
