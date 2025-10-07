using System;

namespace WFM3u8
{
	/// <summary>
	/// 下载进度信息类，包含详细的下载统计数据
	/// </summary>
	public class DownloadProgress
	{
		public int id { get; set; }
		/// <summary>
		/// 下载进度百分比 (0-100)
		/// </summary>
		public double Percentage { get; set; }

		/// <summary>
		/// 已下载字节数
		/// </summary>
		public long DownloadedBytes { get; set; }
		/// <summary>
		/// 总字节数
		/// </summary>
		public long TotalBytes { get; set; }
		/// <summary>
		/// 已下载文件数量
		/// </summary>
		public long DownloadedFile { get; set; }
		/// <summary>
		/// 全部文件数量
		/// </summary>
        public long TotalFile { get; set; }

        /// <summary>
        /// 下载速度 (字节/秒)
        /// </summary>
        public double BytesPerSecond { get; set; }

		/// <summary>
		/// 预计剩余时间
		/// </summary>
		public TimeSpan? RemainingTime { get; set; }

		/// <summary>
		/// 格式化字节数为易读的字符串（KB, MB, GB等）
		/// </summary>
		public string FormatSize(long bytes)
		{
			string[] units = { "B", "KB", "MB", "GB", "TB" };
			double size = bytes;
			int unitIndex = 0;

			while (size >= 1024 && unitIndex < units.Length - 1)
			{
				size /= 1024;
				unitIndex++;
			}

			return $"{size:0.##} {units[unitIndex]}";
		}
	}
}
