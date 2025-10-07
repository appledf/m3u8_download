using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFM3u8.Common
{
    public interface IDownload
    {
        event Action<DownloadProgress> DownloadProgress;
        event Action<DownloadProgress> DownloadComplete;
        event Action<DownloadProgress> DownloadCancel;

        Task<bool> DownloadFileAsync(string url, string savePath);
        Task<bool> ReDownloadFileAsync(string url, string savePath);
        void CancelTask();
    }
}
