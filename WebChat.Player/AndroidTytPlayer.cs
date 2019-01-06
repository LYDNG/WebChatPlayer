using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebChat.Player
{
    public class AndroidTytPlayer : ITytPlayer
    {
        /// <summary>
        /// 获取Android截图
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Tuple<string, Bitmap> GetScreenshots(string name)
        {
            string cmd = $"adb shell screencap -p /sdcard/{name}.png";
            string output = "";
            CmdHelper.RunCmd(cmd, out output);


            cmd = $"adb pull /sdcard/{name}.png ./img";
            CmdHelper.RunCmd(cmd, out output);

            if (output.Contains("error:"))
            {
                return new Tuple<string, Bitmap>("请连接设备" + output, null);
            }


            string dirName = Environment.CurrentDirectory + "//img";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            string fileName = dirName + $"//{name}.png";

            if (File.Exists(fileName))
            {
                string backDir = Environment.CurrentDirectory + "//img/back";
                if (!Directory.Exists(backDir))
                {
                    Directory.CreateDirectory(backDir);
                }
                File.Copy(fileName, backDir + $"//{DateTime.Now.ToString("yyyyMMddHHmmss")}.png", true);
                return new Tuple<string, Bitmap>(string.Empty, ImageHelper.CopyMap(fileName));
            }
            else
            {
                return new Tuple<string, Bitmap>("获取截图失败" + output, null);
            }
        }
        /// <summary>
        /// 模拟滑动跳跃
        /// </summary>
        /// <param name="time"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public string TryJump(int time, Point point)
        {
            Random ra = new Random();
            int raX = ra.Next(30, 70);
            int raY = ra.Next(30, 70);
            string cmd = $"adb shell input swipe {point.X + raX} {point.Y - raY} {point.X + raX} {point.Y - raY} {time}";
            //string cmd = $"adb shell input swipe {point.X} {point.Y} {point.X} {point.Y} {time}";
            string output = "";
            CmdHelper.RunCmd(cmd, out output);
            return cmd;
        }
        /// <summary>
        /// 获取按压位置
        /// </summary>
        /// <param name="screenWidth"></param>
        /// <param name="screenHeight"></param>
        /// <returns></returns>
        public Point GetPressPoint(int screenWidth, int screenHeight)
        {
            int swipeX = (screenWidth / 4) * 3;
            int swipeY = (screenHeight / 3) * 2;
            var ra = new Random().Next(0, 10);
            var ranX = swipeX + ra;
            var ranY = swipeY + ra;
            return new Point(ranX, ranY);
        }
        /// <summary>
        /// 获取按压时间
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="pressCoefficient"></param>
        /// <param name="width"></param>
        /// <returns>按压时间及距离</returns>
        public Tuple<int, int> GetPressTime(Point startPoint, Point endPoint, double pressCoefficient, int width)
        {
            int xl = Math.Abs(startPoint.X - endPoint.X);
            int yl = Math.Abs(startPoint.Y - endPoint.Y);

            int length = (int)Math.Sqrt((double)(xl * xl + yl * yl));
            return new Tuple<int, int>((int)(length * pressCoefficient), length);
        }
    }
}
