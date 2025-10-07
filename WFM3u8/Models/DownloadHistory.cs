using System;

namespace WFM3u8.Models
{
    public class DownloadHistory
    {
        public string Url { get; set; }
        public string SavePath { get; set; }
        public long DownloadedBytes { get; set; } = 0;
        public long TotalBytes { get; set; } = 0;
        public bool IsDownloadComplete { get; set; } = false;
        public DateTime? AddOn { get; set; }
        public DateTime? LastOn { get; set; }
    }
}
