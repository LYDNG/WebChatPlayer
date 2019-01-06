using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebChat.Player;

namespace WebChat
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

#if DEBUG
            测试ToolStripMenuItem.Visible = true;
            txtFileName.Visible = true;
            测试ToolStripMenuItem.Visible = true;
#endif
        }

        double pressCoefficient = 0;
        bool isFullAutomation = true;
        bool IsStop = true;

        #region 测试
        private void 获取起点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string p = Environment.CurrentDirectory + $"//img//fail//{txtFileName.Text}.png";
            Bitmap bt = ImageHelper.CopyMap(p);
            if (bt != null)
            {
                var dd = ImageHelper.GetStartPoint(bt);
                ImageHelper.DrawPoint(bt, dd.Item2);
                pictureBox1.Image = bt;
            }
        }

        private void 获取终点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string p = Environment.CurrentDirectory + $"//img//fail//{txtFileName.Text}.png";
            Bitmap bt = ImageHelper.CopyMap(p);
            if (bt != null)
            {
                var startPoint = ImageHelper.GetStartPoint(bt);
                var endPoints = ImageHelper.GetEndPoints(bt, startPoint.Item2, startPoint.Item1);

                ImageHelper.DrawPoint(bt, startPoint.Item2);
                ImageHelper.DrawPoint(bt, endPoints.Item1);
                ImageHelper.DrawWarnPoint(bt);
                ITytPlayer player = new AndroidTytPlayer();
                var pressTime = player.GetPressTime(startPoint.Item2, endPoints.Item1, 1.463, bt.Width);
                LogMessage($"距离：{pressTime.Item2}按压时间：{pressTime.Item1}");
                //pictureBox1.Image = endPoints.Item2;
                pictureBox1.Image = bt;
            }
        }

        private void 获取图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ITytPlayer player = new AndroidTytPlayer();
            string name = "autojump";
            var image = player.GetScreenshots(name);
            if (image != null)
            {
                var bt = image.Item2;
                var startPoint = ImageHelper.GetStartPoint(bt);
                var endPoints = ImageHelper.GetEndPoints(bt, startPoint.Item2, startPoint.Item1);

                ImageHelper.DrawPoint(bt, startPoint.Item2);
                ImageHelper.DrawPoint(bt, endPoints.Item1);
                ImageHelper.DrawWarnPoint(bt);
                var pressTime = player.GetPressTime(startPoint.Item2, endPoints.Item1, 1.463, bt.Width);
                LogMessage($"距离：{pressTime.Item2}按压时间：{pressTime.Item1}");
                pictureBox1.Image = endPoints.Item2;
                //pictureBox1.Image = bt;
            }
        }

        #endregion

        #region 状态控制
        private void 停止ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setState(true);
        }

        private void setState(bool isStop, bool isAutomation = true)
        {
            IsStop = isStop;
            isFullAutomation = isAutomation;
            if (IsStop)
            {
                laState.Text = "暂停";

                IphoneToolStripMenuItem.Enabled = true;
                AndroidToolStripMenuItem.Enabled = true;

                全自动ToolStripMenuItem.Enabled = true;
                半自动ToolStripMenuItem.Enabled = true;
            }
            else
            {
                IphoneToolStripMenuItem.Enabled = false;
                AndroidToolStripMenuItem.Enabled = false;

                全自动ToolStripMenuItem.Enabled = false;
                半自动ToolStripMenuItem.Enabled = false;

                停止ToolStripMenuItem1.Enabled = true;

                laState.Text = (isFullAutomation ? "全自动" : "半自动") + ":运行中";
            }
        }
        #region 全自动
        private void 开始ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (pressCoefficient <= 0)
            {
                LogMessage("请选择机型");
                return;
            }
            setState(false);
            try
            {
                pressCoefficient = Convert.ToDouble(textBox2.Text);
                playAndroid(pressCoefficient);
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message , ex.StackTrace);
            }
        }
        public void playAndroid(double pressFactor)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    ITytPlayer tytPlayer = new AndroidTytPlayer();
                    string name = "autojump";
                    int sleepTime = 1000;
                    Random ra = new Random();
                    while (!IsStop)
                    {
                        var tytMap = tytPlayer.GetScreenshots(name);
                        if (!string.IsNullOrEmpty(tytMap.Item1) || tytMap.Item2 == null)
                        {
                            LogMessage(tytMap.Item1);
                            break;
                        }
                        else
                        {
                            var startPointInfo = ImageHelper.GetStartPoint(tytMap.Item2);
                            var startPoint = startPointInfo.Item2;
                            ImageHelper.DrawPoint(tytMap.Item2, startPoint);
                            LogMessage(tytMap.Item1);

                            var endInfo = ImageHelper.GetEndPoints(tytMap.Item2, startPoint, startPointInfo.Item1);
                            var btEndPoint = endInfo.Item1;
                            if (btEndPoint.X == -1 && btEndPoint.Y == -1)
                            {
                                LogMessage("未分析出有效区域");
                                continue;
                            }
                            LodImage2(endInfo.Item2);

                            ImageHelper.DrawPoint(tytMap.Item2, btEndPoint);
                            ImageHelper.DrawWarnPoint(tytMap.Item2);

                            LodImage(tytMap.Item2);

                            var pressPoint = tytPlayer.GetPressPoint(tytMap.Item2.Width, tytMap.Item2.Width);
                            var pressTime = tytPlayer.GetPressTime(startPoint, btEndPoint, pressFactor, tytMap.Item2.Width);
                            LogMessage($"距离：{pressTime.Item2}按压时间：{pressTime.Item1}");
                            tytPlayer.TryJump(pressTime.Item1, pressPoint);
                        }

                        Thread.Sleep(sleepTime + ra.Next(0, 500));
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("获取数据失败" + ex.Message, ex.StackTrace);
                }
                LogMessage("运行完毕");
            });
        }

        #endregion

        #region 半自动

        ITytPlayer tytPlayer = null;
        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (tytPlayer == null)
                {
                    tytPlayer = new AndroidTytPlayer();
                }
                pressCoefficient = Convert.ToDouble(textBox2.Text);
                var bitMap = pictureBox1.Image as Bitmap;
                int width = bitMap.Width;
                int height = bitMap.Height;
                int ddx = (int)(e.X / ((decimal)pictureBox1.Height / height));
                int ddy = (int)(e.Y / ((decimal)pictureBox1.Height / height));
                var startPointInfo = ImageHelper.GetStartPoint(bitMap);
                var pressTime = tytPlayer.GetPressTime(startPointInfo.Item2, new Point(ddx, ddy), pressCoefficient, width);

                ImageHelper.DrawPoint(bitMap, new Point(ddx, ddy));
                var pressPoint = tytPlayer.GetPressPoint(width, height);
                pictureBox1.Refresh();
                LogMessage(tytPlayer.TryJump(pressTime.Item1, pressPoint));
                SemiAutoPlay();
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);
            }
        }

        public void SemiAutoPlay()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Thread.Sleep(3000);
                    var tytMap = tytPlayer.GetScreenshots("autojump");
                    if (!string.IsNullOrEmpty(tytMap.Item1) || tytMap.Item2 == null)
                    {
                        LogMessage(tytMap.Item1);
                    }
                    else
                    {
                        this.Invoke(new EventHandler(delegate
                        {
                            pictureBox1.Dock = DockStyle.Left;
                            pictureBox1.Height = splitContainer1.Panel1.Height;
                            pictureBox1.Width = Convert.ToInt32(((double)pictureBox1.Height / tytMap.Item2.Height) * tytMap.Item2.Width);
                        }));
                        var startPointInfo = ImageHelper.GetStartPoint(tytMap.Item2);
                        ImageHelper.DrawPoint(tytMap.Item2, startPointInfo.Item2);
                        LogMessage(tytMap.Item1);
                        LodImage(tytMap.Item2);
                        LogMessage("请点击跳跃目标点");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(ex.Message);
                }
            });
        }

        private void 半自动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pressCoefficient <= 0)
            {
                LogMessage("请选择机型");
                return;
            }
            setState(false, false);
            pictureBox1.MouseUp -= PictureBox1_MouseUp;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
            tytPlayer = new AndroidTytPlayer();
            SemiAutoPlay();
        }

        #endregion


        #endregion

        #region 手机配置


        private void setConfig(string name, double coefficient)
        {
            label3.Text = name;
            pressCoefficient = coefficient;
            textBox2.Text = coefficient.ToString();
            LogMessage("如需调整跳跃时间系数请先调整，然后点击操作下面的全自动或半自动,");
        }

        #region 小米
        private void max2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("小米max2", 1.5);
        }

        private void mi5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("小米mi5", 1.475);
        }

        private void mi5sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("小米mi5s", 1.463);
        }
        private void mi5xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("小米mi5x", 1.45);
        }
        private void mi6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("小米mi6", 1.44);
        }
        private void note2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("小米note2", 1.47);
        }
        #endregion

        #region 华为
        private void honorNote8ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("honorNote8", 1.47);
        }
        #endregion

        #region 三星
        private void s7EdgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("三星s7Edge", 1);
        }
        private void s7ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("三星s8", 1.365);
        }
        #endregion

        #region 锤子
        private void pRO2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("锤子PRO2", 1.392);
        }
        #endregion

        private void 一般情况ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("一般情况", 1.392);
        }

        #region iPhone
        private void 苹果6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果6", 2.0);
        }

        private void 苹果8PToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果8 Plus", 1.2);
        }

        private void 苹果7PToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果7 Plus", 1.2);
        }

        private void 苹果6SPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果6s Plus", 1.2);
        }

        private void 苹果6PToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果6 Plus", 1.2);
        }

        private void 苹果SEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果 SE", 2.3);
        }

        private void iPhoneXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("敬请期待");
            return;
            setConfig("苹果 X", 1.31);
        }
        #endregion

        #region 屏幕大小
        private void x540ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("960x540手机", 2.732);
        }

        private void x720ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("1280x720手机", 2.099);
        }

        private void x720ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            setConfig("1440x720手机", 2.099);
        }

        private void x1080ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("1920x1080手机", 1.392);
        }

        private void x1080ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            setConfig("2160x1080手机", 1.372);
        }

        private void x1440ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setConfig("2560x1440手机", 1.475);
        }

        #endregion
        #endregion

        private void LodImage(Bitmap bitmap)
        {
            this.Invoke(new EventHandler(delegate
            {
                pictureBox1.Image = bitmap;
            }));
        }
        private void LodImage2(Bitmap bitmap)
        {
            this.Invoke(new EventHandler(delegate
            {
                pictureBox2.Image = bitmap;
            }));
        }

        private void LogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))

            {
                return;
            }
            if (message.Contains("failed") && message.Contains("no devices") && message.Contains("emulators"))
            {
                message = "获取手机截图失败，请检查驱动安装情况及是否开启开发者模式";
            }

            this.BeginInvoke(new EventHandler(delegate
            {
                txtMessage.AppendText(message + Environment.NewLine);
            }));
        }
        private void LogMessage(string message,string detail)
        {
            LogMessage(message);
        }

    }
}
