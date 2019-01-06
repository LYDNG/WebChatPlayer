using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebChat.Player
{
    public interface ITytPlayer
    {
        /// <summary>
        /// 获取截屏
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Tuple<string, Bitmap> GetScreenshots(string name);
        /// <summary>
        /// 模拟跳跃
        /// </summary>
        /// <param name="time"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        string TryJump(int time, Point point);
        /// <summary>
        /// 获取按压点
        /// </summary>
        /// <param name="screenWidth"></param>
        /// <param name="screenHeight"></param>
        /// <returns></returns>
        Point GetPressPoint(int screenWidth, int screenHeight);
        /// <summary>
        /// 获取按压时间
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="pressCoefficient"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        Tuple<int, int> GetPressTime(Point startPoint, Point endPoint, double pressCoefficient,int width);
    }
}
