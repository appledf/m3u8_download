using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WFM3u8
{
    public class M3U8Downloader
    {
        private string _m3u8Url;
        private string _outputFile;
        private int _maxWorkers;
        private WebProxy _proxy;
        private string _ffmpegPath;
        private string _tempDir;
        private List<string> _tsUrls = new List<string>();
        private HttpClient _httpClient;

        public Action<string> ShowMess;

        private CancellationTokenSource _cts;

        /// <summary>
        /// 初始化M3U8下载器
        /// </summary>
        /// <param name="m3u8Url">m3u8文件的URL</param>
        /// <param name="outputFile">输出的视频文件名</param>
        /// <param name="maxWorkers">最大并发下载数</param>
        /// <param name="proxy">代理设置，格式: http://proxy:port 或 https://proxy:port</param>
        /// <param name="ffmpegPath">ffmpeg可执行文件的路径，默认为'ffmpeg'（需在环境变量中）</param>
        public M3U8Downloader(string m3u8Url, string outputFile, int maxWorkers = 5, string proxy = null, string ffmpegPath = "ffmpeg")
        {
            _m3u8Url = m3u8Url;
            _outputFile = outputFile;
            _maxWorkers = maxWorkers;
            _ffmpegPath = ffmpegPath;

            // 创建临时目录
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            // 配置HTTP客户端
            var handler = new HttpClientHandler();

            // 设置代理
            if (!string.IsNullOrEmpty(proxy))
            {
                _proxy = new WebProxy(proxy);
                handler.Proxy = _proxy;
            }

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            // 检查ffmpeg是否可用
            CheckFfmpegAvailable();
        }

        /// <summary>
        /// 检查ffmpeg是否可用
        /// </summary>
        private void CheckFfmpegAvailable()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    ConsoleWriteLine($"成功找到ffmpeg: {_ffmpegPath}");
                }
                else
                {
                    ConsoleWriteLine($"错误: ffmpeg路径存在问题，无法正常执行: {_ffmpegPath}");
                }
            }
            catch (FileNotFoundException)
            {
                ConsoleWriteLine($"错误: 找不到ffmpeg可执行文件，请检查路径是否正确: {_ffmpegPath}");
                ConsoleWriteLine("请确保提供的路径指向正确的ffmpeg可执行文件");
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"检查ffmpeg时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取m3u8文件内容
        /// </summary>
        /// <returns>m3u8文件内容</returns>
        private async Task<string> GetM3u8ContentAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_m3u8Url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"获取m3u8文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析m3u8文件，提取ts文件URL
        /// </summary>
        /// <param name="m3u8Content">m3u8文件内容</param>
        /// <returns>是否解析成功</returns>
        private bool ParseM3u8(string m3u8Content)
        {
            if (string.IsNullOrEmpty(m3u8Content))
                return false;

            _tsUrls.Clear();
            var baseUri = new Uri(_m3u8Url);

            foreach (var line in m3u8Content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#"))
                {
                    // 如果是相对路径，则拼接成绝对URL
                    var tsUrl = new Uri(baseUri, trimmedLine).ToString();
                    _tsUrls.Add(tsUrl);
                }
            }

            ConsoleWriteLine($"成功解析到 {_tsUrls.Count} 个ts片段");
            return _tsUrls.Count > 0;
        }

        /// <summary>
        /// 下载单个ts文件
        /// </summary>
        /// <param name="tsInfo">包含索引和URL的元组</param>
        /// <returns>是否下载成功</returns>
        private async Task<bool> DownloadTsAsync(Tuple<int, string> tsInfo)
        {
            int index = tsInfo.Item1;
            string tsUrl = tsInfo.Item2;
            string tsFilename = Path.Combine(_tempDir, $"{index:D8}.ts");

            try
            {
                var response = await _httpClient.GetAsync(tsUrl);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tsFilename, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }

                ConsoleWriteLine($"已下载 {index + 1}/{_tsUrls.Count}\r");
                return true;
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"\n下载ts片段 {index + 1} 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 并发下载所有ts文件
        /// </summary>
        /// <returns>是否所有ts文件都下载成功</returns>
        private async Task<bool> DownloadAllTsAsync()
        {
            if (_tsUrls.Count == 0)
            {
                ConsoleWriteLine("没有需要下载的ts文件");
                return false;
            }
            //CancellationToken
            if (_cts != null)
            {
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            // 创建包含索引和URL的元组列表，用于保持下载顺序
            var tsInfoList = _tsUrls.Select((url, index) => Tuple.Create(index, url)).ToList();

            ConsoleWriteLine($"开始下载 {_tsUrls.Count} 个ts片段...");

            // 限制并发数量
            var semaphore = new SemaphoreSlim(_maxWorkers);
            var tasks = new List<Task<bool>>();

            int countComplete = 0;
            foreach (var tsInfo in tsInfoList)
            {
                await semaphore.WaitAsync();
                tasks.Add(DownloadTsAsync(tsInfo).ContinueWith(t =>
                {
                    semaphore.Release();
                    return t.Result;
                }, token));
            }

            var results = await Task.WhenAll(tasks);
            int successCount = results.Count(r => r);

            ConsoleWriteLine($"\n下载完成，成功 {successCount}/{_tsUrls.Count}");
            return successCount == _tsUrls.Count;
        }

        /// <summary>
        /// 使用ffmpeg合并ts文件
        /// </summary>
        /// <returns>是否合并成功</returns>
        private bool MergeTsFiles()
        {
            var tsFiles = Directory.GetFiles(_tempDir, "*.ts");
            if (tsFiles.Length == 0)
            {
                ConsoleWriteLine("没有可合并的ts文件");
                return false;
            }

            // 创建ts文件列表
            string tsListFile = Path.Combine(_tempDir, "ts_list.txt");
            // 使用无 BOM 的 UTF-8 编码，避免 FFmpeg 解析乱码
            using (var writer = new StreamWriter(tsListFile, false, new UTF8Encoding(false)))
            {
                foreach (var filename in tsFiles.OrderBy(f => f))
                {
                    string filePath = filename.Replace("\\", "/");
                    writer.WriteLine($"file '{filePath}'");
                }
            }

            ConsoleWriteLine($"ts_list_file: {tsListFile}");

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = $"-f concat -safe 0 -i \"{tsListFile}\" -c copy -y \"{_outputFile}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    ConsoleWriteLine($"合并视频失败: {errorOutput}");
                    return false;
                }

                ConsoleWriteLine($"视频合并完成，已保存为: {_outputFile}");
                return true;
            }
            catch (FileNotFoundException)
            {
                ConsoleWriteLine($"未找到ffmpeg，请检查路径是否正确: {_ffmpegPath}");
                return false;
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"合并视频时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        private void CleanTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                    ConsoleWriteLine("临时文件已清理");
                }
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"清理临时文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 完整下载流程
        /// </summary>
        /// <returns>是否下载成功</returns>
        public async Task<bool> DownloadAsync()
        {
            ConsoleWriteLine($"开始下载: {_m3u8Url}");

            // 1. 获取m3u8内容
            string m3u8Content = await GetM3u8ContentAsync();
            if (string.IsNullOrEmpty(m3u8Content))
                return false;

            // 2. 解析m3u8获取ts文件URL
            if (!ParseM3u8(m3u8Content))
                return false;

            // 3. 下载所有ts文件
            if (!await DownloadAllTsAsync())
                return false;

            // 4. 合并ts文件
            if (!MergeTsFiles())
                return false;

            // 5. 清理临时文件
            CleanTempFiles();

            ConsoleWriteLine("下载完成！");
            return true;
        }

        public void ConsoleWriteLine(string mess)
        {
            if (ShowMess != null)
            {
                ShowMess.Invoke(mess);
            }
        }

        public void CancelTask()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                // 发送取消请求
                _cts.Cancel();
                ConsoleWriteLine("正在取消任务...");
            }
        }
    }
}
