using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebChat
{
    public class ImageHelper
    {
        public static void DrawPoint(Bitmap bitmap, Point point)
        {
            if (point == null || bitmap == null)
            {
                return;
            }
            using (var g = Graphics.FromImage(bitmap))
            {
                using (Pen p = new Pen(Color.Red, 2))
                {
                    g.DrawEllipse(p, point.X - 2, point.Y - 2, 4, 4);
                }
            }
        }
        public static void DrawWarnPoint(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return;
            }
            int warnWidth = bitmap.Width / 8;
            int warnHeight = bitmap.Height / 8;
            Color lineColor = Color.Black;
            using (var g = Graphics.FromImage(bitmap))
            {
                using (Pen p = new Pen(Color.Black, 2))
                {
                    for (int h = 0; h < bitmap.Height; h++)
                    {
                        bitmap.SetPixel(2 * warnWidth, h, lineColor);
                        bitmap.SetPixel(3 * warnWidth, h, lineColor);
                        bitmap.SetPixel(5 * warnWidth, h, lineColor);
                        bitmap.SetPixel(6 * warnWidth, h, lineColor);
                    }

                    for (int w = 0; w < bitmap.Width; w++)
                    {
                        bitmap.SetPixel(w, 3 * warnHeight, lineColor);
                        bitmap.SetPixel(w, 4 * warnHeight, lineColor);
                        bitmap.SetPixel(w, 5 * warnHeight, lineColor);
                    }
                }
            }
        }

        public static Bitmap CopyMap(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }
            var bitmap = new Bitmap(fileName);
            Bitmap newImg = new Bitmap(bitmap.Width, bitmap.Height);
            using (bitmap)
            {
                using (Graphics g = Graphics.FromImage(newImg))
                {
                    g.DrawImage(bitmap, 0, 0, new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
                }
            }
            return newImg;
        }

        public static Tuple<int, Point> GetStartPoint(Bitmap bitmap)
        {
            string tempGrayPath = Environment.CurrentDirectory + $"//img//Current.png";
            var tempGrayImg = new Image<Rgb, byte>(tempGrayPath);
            Image<Rgb, Byte> img = new Image<Rgb, Byte>(bitmap);
            var match = img.MatchTemplate(tempGrayImg, TemplateMatchingType.CcorrNormed);

            double min = 0, max = 0;
            Point maxp = new Point(0, 0);//最好匹配的点
            Point minp = new Point(0, 0);
            CvInvoke.MinMaxLoc(match, ref min, ref max, ref minp, ref maxp);

            int x = maxp.X + (int)(tempGrayImg.Width / 2.0);
            int y = maxp.Y + tempGrayImg.Height - tempGrayImg.Width / 4;
            return new Tuple<int, Point>(tempGrayImg.Width, new Point(x, y));
        }


        public static Tuple<Point, Bitmap> GetEndPoints(Bitmap bitmap, Point startPoint, int playerWidth)
        {
            var result = new List<Point>();
            var resultTop = new List<Point>();
            var effectiveArea = GetEffectiveArea(bitmap, startPoint, playerWidth);
            var effectiveBasePoint = effectiveArea.Item1;
            var effectiveBitmap = effectiveArea.Item2;

            bool isLeft = true;
            if (startPoint.X < bitmap.Width / 2)
            {
                isLeft = false;
            }

            Color targetColor = Color.FromArgb(0, 0, 0);
            Point firstTargetPoint = new Point(-1, -1);
            double xRange = 0.5;
            int minEffectiveX = 0;
            int maxEffectiveX = 0;
            if (isLeft)
            {
                maxEffectiveX = Convert.ToInt32(effectiveBitmap.Width * xRange);
            }
            else
            {
                minEffectiveX = Convert.ToInt32(effectiveBitmap.Width * (1 - xRange));
                maxEffectiveX = effectiveBitmap.Width;
            }

            int minIgnoreX = startPoint.X - playerWidth / 2;
            int maxIgnoreX = startPoint.X + playerWidth / 2;

            for (int h = 0; h < effectiveBitmap.Height; h++)
            {
                var firstPixel = effectiveBitmap.GetPixel(0, h);

                if (!isLeft)
                {
                    firstPixel = effectiveBitmap.GetPixel(effectiveBitmap.Width - 1, h);
                }

                for (int w = minEffectiveX; w < maxEffectiveX; w++)
                {
                    //player的X区间不检测
                    if (minIgnoreX < w && w < maxIgnoreX)
                    {
                        continue;
                    }
                    var pixel = effectiveBitmap.GetPixel(w, h);
                    var isTar = IsTarget(firstPixel, pixel, targetColor);
                    if (isTar.Item2)
                    {
                        var p = new Point(w, h);
                        if (result.Count <= 0)
                        {
                            targetColor = effectiveBitmap.GetPixel(w, h + 5);
                            firstTargetPoint = p;
                        }
                        if (isTar.Item1)
                        {
                            resultTop.Add(p);
                        }
                        result.Add(p);
                    }
                }
            }

            int trueX = 0;
            int trueY = 0;
            int minX = 0;
            int maxX = 0;
            int minY = 0;
            int maxY = 0;
            resultTop = getTragetYs(resultTop);

            if (resultTop.Count / result.Count < 0.8)
            {
                trueX = (int)resultTop.Average(p => p.X);
                trueY = (int)resultTop.Average(p => p.Y);
                minX = resultTop.Min(p => p.X);
                maxX = resultTop.Max(p => p.X);
                minY = resultTop.Min(p => p.Y);
                maxY = resultTop.Max(p => p.Y);
            }
            else
            {
                var minYxs = result.Where(p => p.Y == firstTargetPoint.Y).ToList();
                trueX = (int)minYxs.Average(p => p.X);

                minX = result.Min(p => p.X);
                maxX = result.Max(p => p.X);
                minY = result.Min(p => p.Y);
                maxY = result.Max(p => p.Y);
                var maxXy = result.Where(p => p.X <= maxX && p.X > maxX - 3).ToList();
                trueY = maxXy.Min(p => p.Y) + 2;
            }
            #region 添加随机偏移
            //var diff = maxX - minX;
            //if (diff > playerWidth * 2)
            //{
            //    Random r = new Random();

            //    var diff2 = diff - playerWidth;
            //    var i = r.Next(3, 7);
            //    var i2 = r.Next(-10, 10);
            //    int i3 = (int)(i * 0.1 * diff2);
            //    if (i > 0)
            //    {
            //        trueX = trueX + i3;
            //    }
            //    else
            //    {
            //        trueX = trueX - i3;
            //    }
            //} 
            #endregion
            
            DrawLine(effectiveBitmap, minX, minY, maxX, maxY);
            DrawPoint(effectiveBitmap, new Point(trueX, trueY));
            return new Tuple<Point, Bitmap>(new Point(effectiveBasePoint.X + trueX, effectiveBasePoint.Y + trueY), effectiveBitmap);
        }

        /// <summary>
        /// 去除不相邻的坐标
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<Point> getTragetYs(List<Point> list)
        {
            var result = new List<int>();
            var listY = list.ConvertAll(p => p.Y).Distinct().ToList();
            listY.Sort();
            int temp = 0;
            for (int i = 1; i < listY.Count; i++)
            {
                if (listY[i] - listY[i - 1] > 1)
                {
                    temp = listY[i];
                }
            }

            if (temp > 0)
            {
                list.RemoveAll(p => p.Y >= temp);
            }
            return list;

        }

        public static Tuple<bool, bool> IsTarget(Color startColor, Color color, Color targetColor)
        {
            var resultTop = false;
            var resultTarget = false;

            if (targetColor.R != 0 && targetColor.G != 0 && targetColor.B != 0)
            {
                if (Math.Abs(color.R - targetColor.R) + Math.Abs(color.G - targetColor.G) + Math.Abs(color.B - targetColor.B) < 10)
                {
                    resultTop = true;
                }
                else
                {
                    resultTop = false;
                }
            }

            if (Math.Abs(color.R - startColor.R) + Math.Abs(color.G - startColor.G) + Math.Abs(color.B - startColor.B) > 10)
            {
                resultTarget = true;
            }
            return new Tuple<bool, bool>(resultTop, resultTarget);
        }

        public static Tuple<Point, Bitmap> GetEffectiveArea(Bitmap bitmap, Point startPoint, int playWidth)
        {
            int minX = 0;
            int maxX = bitmap.Width;
            int minY = 0;
            int maxY = 0;

            minY = bitmap.Height / 3;
            maxY = startPoint.Y - playWidth;


            int width = maxX - minX;
            int height = maxY - minY;
            if (width < 0 || height < 0)
            {
                return new Tuple<Point, Bitmap>(new Point(width, height), null);
            }
            Bitmap newImg = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(newImg))
            {
                g.DrawImage(bitmap, 0, 0, new Rectangle(minX, minY, width, height), GraphicsUnit.Pixel);
            }
            return new Tuple<Point, Bitmap>(new Point(minX, minY), newImg);
        }

        public static void DrawPoint(Bitmap bitmap, List<Point> points)
        {
            if (points == null || bitmap == null || points.Count <= 0)
            {
                return;
            }
            using (var g = Graphics.FromImage(bitmap))
            {
                using (Pen p = new Pen(Color.Red, 2))
                {
                    foreach (var point in points)
                    {
                        g.DrawEllipse(p, point.X - 2, point.Y - 2, 4, 4);
                    }
                }
            }
        }
        public static void DrawLine(Bitmap bitmap, int minX, int minY, int maxX, int maxY)
        {
            if (bitmap == null)
            {
                return;
            }
            using (var g = Graphics.FromImage(bitmap))
            {
                using (Pen p = new Pen(Color.Red, 2))
                {
                    g.DrawLine(p, new Point(minX, minY), new Point(maxX, minY));
                    g.DrawLine(p, new Point(maxX, minY), new Point(maxX, maxY));
                    g.DrawLine(p, new Point(maxX, maxY), new Point(minX, maxY));
                    g.DrawLine(p, new Point(minX, maxY), new Point(minX, minY));
                }
            }
        }
    }
}
