using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WFM3u8
{
    public partial class Form1 : Form
    {
        private static string _ffmpegPath = @"E:\ffmpeg-20250721\bin\ffmpeg.exe";
        private static string _proxyHost = "";
        private static M3U8Downloader downloader = null;

        public Form1()
        {
            InitializeComponent();
            LoadSetting();
        }

        #region event
        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtM3u8.Text))
            {
                MessageBox.Show("请输入m3u8地址");
                return;
            }
            if (string.IsNullOrEmpty(txtExport.Text))
            {
                MessageBox.Show("请输入视频导出地址");
                return;
            }
            txtMess.Text = "";
            // 示例用法
            string m3u8Url = txtM3u8.Text;
            string outputFile = $@"{txtExport.Text}\{DateTime.Now.ToString("yyyyMMddHHmmss")}.mp4";

            // 创建下载器实例,使用代理
            if (StaticData.UseProxy)
            {
                downloader = new M3U8Downloader(m3u8Url, outputFile, 10, StaticData.Proxy, _ffmpegPath);
            }
            else
            {
                downloader = new M3U8Downloader(m3u8Url, outputFile, 10, null, _ffmpegPath);
            }
            downloader.ShowMess = ConsoleWriteLine;
            downloader.DownloadAsync();
        }
        /// <summary>
        /// 测试代理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetting_Click(object sender, EventArgs e)
        {
            FSetting fs = new FSetting();
            fs.Reload = LoadSetting;
            fs.StartPosition = FormStartPosition.CenterParent;
            fs.Owner = this;
            fs.ShowDialog();
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (downloader != null)
            {
                downloader.CancelTask();
            }
        }
        private void btnOpen_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(txtExport.Text)
            {
                UseShellExecute = true // 使用系统默认程序打开
            });
        }
        #endregion

        #region function
        public void LoadSetting()
        {
            txtExport.Text = StaticData.Export;
        }
        public void ConsoleWriteLine(string mess)
        {
            mess = mess+Environment.NewLine;
            if (txtMess.InvokeRequired)
            {
                // 如果需要，通过委托在UI线程执行
                txtMess.BeginInvoke(new Action<string>(ConsoleWriteLine), mess);
            }
            else
            {
                // 不需要跨线程，直接更新UI
                txtMess.AppendText(mess);
            }
        }
        #endregion
    }
}
