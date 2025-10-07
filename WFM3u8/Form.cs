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
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Xml.Linq;
using WFM3u8.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WFM3u8
{
    public partial class Form : System.Windows.Forms.Form
    {
        private static string _ffmpegPath = @"E:\ffmpeg-20250721\bin\ffmpeg.exe";
        private static string _proxyHost = "";
        private static M3U8Downloader downloader = null;
        BindingList<LinkEntity> hyperlinks = new BindingList<LinkEntity>();
        private static string _currentFileType = "";

        public Form()
        {
            InitializeComponent();
            LoadSetting();
            //txtM3u8.Text = @"https://www.girlswithmuscle.com/images/full/2580596.mp4";
            txtM3u8.Text = @"https://vip.ffzy-plays.com/20250921/45586_b145d383/3000k/hls/mixed.m3u8";
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
        }

        #region event
        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            string url = txtM3u8.Text;
            _currentFileType = UrlUtil.GetExtension(url);
            if (_currentFileType.EndsWith(".m3u8"))
            {
                DownloadM3u8(txtM3u8.Text, txtExport.Text);
            }
            else // if (url.EndsWith(".mp4"))
            {
				DownloadMp4Async(txtM3u8.Text, txtExport.Text);
			}
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
        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (downloader != null)
            {
                downloader.CancelTask();
            }
        }
        /// <summary>
        /// 打开保存路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpen_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(txtExport.Text)
            {
                UseShellExecute = true // 使用系统默认程序打开
            });
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // 获取剪贴板中的文本
            string clipboardText = Clipboard.GetText();
            txtM3u8.Text = clipboardText;
        }
        #endregion

        #region function
        private void DownloadM3u8(string m3u8Url,string videoPath)
        {
			txtMess.Text = "";
			// 示例用法
			string outputFile = $@"{videoPath}\{DateTime.Now.ToString("yyyyMMddHHmmss")}.mp4";

			// 创建下载器实例,使用代理
			if (StaticData.UseProxy)
			{
				downloader = new M3U8Downloader(m3u8Url, outputFile, 10, StaticData.Proxy, _ffmpegPath);
			}
			else
			{
				downloader = new M3U8Downloader(m3u8Url, outputFile, 10, null, _ffmpegPath);
			}
            downloader.DownloadComplete += DownLoadTsComplete;
            downloader.DownloadProgress += DownloadFileProgress;
            downloader.ShowMess += ConsoleWriteLine;
			downloader.DownloadAsync();
		}
        private async Task DownloadMp4Async(string mp4Url, string videoPath)
        {
			var downloader = new FileDownloader();
            downloader.DownloadProgress += DownloadFileProgress;

            string savePath = $@"{videoPath}\{Path.GetFileName(mp4Url)}";

			ConsoleWriteLine($"开始下载: {mp4Url}");
			ConsoleWriteLine($"保存路径: {savePath}\n");

            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
		    bool success = await downloader.DownloadFileAsync(mp4Url,savePath);

			ConsoleWriteLine("\n");
			if (success)
			{
				ConsoleWriteLine($"文件已成功保存到: {savePath}");
			}
			else
			{
				ConsoleWriteLine("文件下载失败");
			}
		}
        private void DownloadFileProgress(DownloadProgress progress)
        {
            // 清除当前行并显示新进度
            if (_currentFileType.Equals(".m3u8"))
            { 
                ConsoleWriteLine($"进度: {progress.Percentage:F2}% " +
                          $"| 已下载: {progress.DownloadedFile} " +
                          $"| 总大小: {progress.TotalFile} " +
                          $"| 速度: {progress.FormatSize((long)progress.BytesPerSecond)}/s " +
                          $"| 剩余时间: {progress.RemainingTime?.ToString(@"mm\:ss") ?? "未知"}");
                lSpeed.Text = $"{progress.FormatSize((long)progress.BytesPerSecond)}/s";
                lSize.Text = $"{progress.DownloadedFile}/{progress.TotalFile}";
            }
            else
            {
                ConsoleWriteLine($"进度: {progress.Percentage:F2}% " +
                          $"| 已下载: {progress.FormatSize(progress.DownloadedBytes)} " +
                          $"| 总大小: {progress.FormatSize(progress.TotalBytes)} " +
                          $"| 速度: {progress.FormatSize((long)progress.BytesPerSecond)}/s " +
                          $"| 剩余时间: {progress.RemainingTime?.ToString(@"mm\:ss") ?? "未知"}");
                lSpeed.Text = $"{progress.FormatSize((long)progress.BytesPerSecond)}/s";
                lSize.Text = $"{progress.FormatSize(progress.DownloadedBytes)}/{progress.FormatSize(progress.TotalBytes)}";
            }
            
            progressBar1.Value = (int)progress.Percentage;
        }
        private void DownLoadTsComplete(object parameter)
        {
            if (parameter != null)
            {
                DownloadProgress progress = parameter as DownloadProgress;
                progressBar1.Value = (int)progress.DownloadedBytes;
                lSpeed.Text = progress.BytesPerSecond.ToString();
                lSize.Text = $"{((int)progress.DownloadedBytes).ToString()}/{((int)progress.TotalBytes).ToString()}";

                downloader.DownloadComplete -= DownLoadTsComplete;
                downloader.DownloadProgress -= DownloadFileProgress;
                downloader.ShowMess -= ConsoleWriteLine;
            }
        }
        /// <summary>
        /// 加载设置
        /// </summary>
        public void LoadSetting()
        {
            txtExport.Text = StaticData.Export;
        }
        /// <summary>
        /// 显示提示信息
        /// </summary>
        /// <param name="mess"></param>
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
