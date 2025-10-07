using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WFM3u8.Common;

namespace WFM3u8
{
	public class FileDownloader:IDownload
	{
		private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        public event Action<DownloadProgress> DownloadProgress;
        public event Action<DownloadProgress> DownloadComplete;
		public event Action<DownloadProgress> DownloadCancel;

        public FileDownloader()
		{
			_httpClient = new HttpClient();
			// 设置超时时间为10分钟
			_httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

		/// <summary>
		/// 下载文件
		/// </summary>
		/// <param name="url">文件URL</param>
		/// <param name="savePath">保存路径</param>
		/// <param name="progressCallback">进度回调函数，包含详细统计信息</param>
		/// <returns>是否下载成功</returns>
		public async Task<bool> DownloadFileAsync(string url, string savePath)
		{
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
			var progress = new DownloadProgress();
            try
			{
                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 获取文件信息
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
				{
					response.EnsureSuccessStatusCode();
                    //获取文件大小
					var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    progress.TotalBytes = totalBytes ;
					var canReportProgress = totalBytes != -1 && DownloadProgress != null;

					// 用于计算下载速度的变量
					var startTime = DateTime.Now;
					var lastReportTime = startTime;
					var bytesSinceLastReport = 0L;

					using (var stream = await response.Content.ReadAsStreamAsync())
					using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						var buffer = new byte[8192];
						var downloadedBytes = 0L;
						int bytesRead;

						while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0 && !token.IsCancellationRequested)
						{
							await fileStream.WriteAsync(buffer, 0, bytesRead);
							downloadedBytes += bytesRead;
							bytesSinceLastReport += bytesRead;

							progress.DownloadedBytes = downloadedBytes;

							// 报告进度（每500ms或下载完成时更新一次）
							if (canReportProgress)
							{
								var now = DateTime.Now;
								var timeSinceLastReport = now - lastReportTime;

								// 计算下载进度百分比
								progress.Percentage = (double)downloadedBytes / totalBytes * 100;

								// 计算下载速度（至少每500ms计算一次）
								if (timeSinceLastReport.TotalMilliseconds >= 500 || downloadedBytes == totalBytes)
								{
									progress.BytesPerSecond = bytesSinceLastReport / timeSinceLastReport.TotalSeconds;

									// 计算剩余时间
									if (progress.BytesPerSecond > 0 && downloadedBytes < totalBytes)
									{
										var remainingBytes = totalBytes - downloadedBytes;
										progress.RemainingTime = TimeSpan.FromSeconds(remainingBytes / progress.BytesPerSecond);
									}
									else
									{
										progress.RemainingTime = null;
									}

                                    // 触发回调
                                    DownloadProgress(progress);

									// 重置速度计算变量
									lastReportTime = now;
									bytesSinceLastReport = 0;
								}
							}
						}
					}
				}
				
				if (progress.Percentage > 100)
				{
					if (DownloadComplete != null)
					{
						DownloadComplete.Invoke(progress);
					}
                    Console.WriteLine("文件下载完成！");
                }
				else
				{
					if (DownloadCancel != null)
					{
						DownloadCancel.Invoke(progress);
                        Console.WriteLine("文件下载取消！");
                        return false;
                    }
                }
				return true;
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"HTTP请求错误: {ex.Message}");
			}
			catch (IOException ex)
			{
				Console.WriteLine($"文件操作错误: {ex.Message}");
			}
			catch (TaskCanceledException)
			{
				Console.WriteLine("下载超时或被取消");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"发生错误: {ex.Message}");
			}

			return false;
		}
        /// <summary>
        /// 暂停后再次启动
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public async Task<bool> ReDownloadFileAsync(string url, string savePath)
        {
            long _downloadedBytes = 0;
            long _totalBytes = 0;
            var bytesSinceLastReport = 0L;
            var lastReportTime = DateTime.Now;
            DownloadProgress downloadProgress = new DownloadProgress();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            try
            {
                // 检查是否已有部分下载的文件
                if (File.Exists(savePath))
                {
                    var fileInfo = new FileInfo(savePath);
                    _downloadedBytes = fileInfo.Length;
                }

                // 获取文件总大小
                if (_totalBytes == 0)
                {
                    _totalBytes = await GetFileSizeAsync(url);
                    if (_totalBytes == 0)
                    {
                        Console.WriteLine("无法获取文件大小");
                        return false;
                    }
                }

                // 如果已经下载完成，直接触发完成事件
                if (_downloadedBytes >= _totalBytes)
                {
                    // 触发回调
                    downloadProgress.BytesPerSecond = 0.0;
                    downloadProgress.RemainingTime = TimeSpan.Zero;
                    downloadProgress.DownloadedBytes = _totalBytes;
                    downloadProgress.TotalBytes = _totalBytes;
                    return true;
                }

                // 开始下载
                using (var httpClient = new HttpClient())
                using (var fileStream = new FileStream(savePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    var canReportProgress = _totalBytes != -1 && DownloadProgress != null;

                    // 设置Range请求头，从已下载的位置继续下载
                    httpClient.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(_downloadedBytes, _totalBytes - 1);
                    lastReportTime = DateTime.Now;
                    using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
                    {
                        response.EnsureSuccessStatusCode();

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            var buffer = new byte[8192]; // 8KB缓冲区
                            int bytesRead;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0 && !token.IsCancellationRequested)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, token);

                                _downloadedBytes += bytesRead;
                                bytesSinceLastReport += bytesRead;
                                downloadProgress.DownloadedBytes = _downloadedBytes;

                                if (canReportProgress)
                                {
                                    var now = DateTime.Now;
                                    var timeSinceLastReport = now - lastReportTime;

                                    // 计算下载进度百分比
                                    downloadProgress.Percentage = ((double)_downloadedBytes / _totalBytes) * 100;

                                    if (timeSinceLastReport.TotalMilliseconds >= 500 || _downloadedBytes == _totalBytes)
                                    {
                                        downloadProgress.BytesPerSecond = bytesSinceLastReport / timeSinceLastReport.TotalSeconds;

                                        // 计算剩余时间
                                        if (downloadProgress.BytesPerSecond > 0 && _downloadedBytes < _totalBytes)
                                        {
                                            var remainingBytes = _totalBytes - _downloadedBytes;
                                            downloadProgress.RemainingTime = TimeSpan.FromSeconds(remainingBytes / downloadProgress.BytesPerSecond);
                                        }
                                        else
                                        {
                                            downloadProgress.RemainingTime = null;
                                        }

                                        // 触发回调
                                        DownloadProgress(downloadProgress);

                                        // 重置速度计算变量
                                        lastReportTime = now;
                                        bytesSinceLastReport = 0;
                                    }
                                }
                            }
                        }
                    }
                }
                if (downloadProgress.Percentage > 100)
                {
                    if (DownloadComplete != null)
                    {
                        DownloadComplete.Invoke(downloadProgress);
                    }
                    Console.WriteLine("文件下载完成！");
                }
                else
                {
                    if (DownloadCancel != null)
                    {
                        DownloadCancel.Invoke(downloadProgress);
                        Console.WriteLine("文件下载取消！");
                        return false;
                    }
                }
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP请求错误: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"文件操作错误: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("下载超时或被取消");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }

            return false;
        }
        /// <summary>
        /// 取消当前任务
        /// </summary>
        public void CancelTask()
		{
			if (_cts != null)
			{
                _cts.Cancel();
            }
		}
        /// <summary>
        /// 获取文件总大小
        /// </summary>
        private async Task<long> GetFileSizeAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // 先尝试发送HEAD请求获取文件信息（更轻量）
                    var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
                    var response = await httpClient.SendAsync(headRequest);
                    response.EnsureSuccessStatusCode();

                    if (response.Content.Headers.ContentLength.HasValue)
                    {
                        return response.Content.Headers.ContentLength.Value;
                    }
                }
                catch
                {
                    // 如果HEAD请求失败，尝试发送GET请求获取Content-Length
                    using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        if (response.Content.Headers.ContentLength.HasValue)
                        {
                            return response.Content.Headers.ContentLength.Value;
                        }
                    }
                }
            }
            return 0;
        }
    }
}
