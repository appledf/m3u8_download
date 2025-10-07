using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Xml;

namespace WFM3u8.Common
{
    public class HtmlFromUrl
    {
        /// <summary>
        /// 获取包含关键字的超链接
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<List<LinkEntity>> GetHyperLink(string webUrl, string key)
        {
            string content = await GetHtmlContentAsync(webUrl);

            var links = new List<LinkEntity>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);

            // 查找所有a标签
            var aNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (aNodes == null)
                return links;

            foreach (var node in aNodes)
            {
                string url = node.GetAttributeValue("href", string.Empty);
                string displayText = node.InnerText.Trim(); // 获取链接的显示文本
                if (displayText.Contains(key))
                { 
                    links.Add(new LinkEntity
                    {
                        UrlText = url.StartsWith("http")?url: GetBaseUrl(webUrl)+url,
                        Text = displayText
                    });
                }
            }

            return links;
        }
        /// <summary>
        /// 从指定URL获取HTML内容
        /// </summary>
        private async Task<string> GetHtmlContentAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                // 设置合理的超时时间
                client.Timeout = TimeSpan.FromSeconds(30);

                // 添加User-Agent头，有些网站会拒绝没有User-Agent的请求
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // 如果HTTP请求失败，会抛出异常

                return await response.Content.ReadAsStringAsync();
            }
        }
        /// <summary>
        /// 从HTML内容中解析出所有超链接
        /// </summary>
        private List<LinkEntity> ParseLinks(string htmlContent, string baseUrl,string key)
        {
            // 修正HtmlDocument实例化方式
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.OptionReadEncoding = false; // 禁用自动编码检测，避免一些潜在问题
            doc.LoadHtml(htmlContent);

            List<LinkEntity> links = new List<LinkEntity>();

            // 获取所有a标签
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
            foreach (var node in nodes)
            {
                string href = node.GetAttributeValue("href", string.Empty);

                if (node.InnerText.Contains(key))
                { 
                    if (!string.IsNullOrEmpty(href))
                    {
                        // 处理相对路径
                        if (Uri.TryCreate(href, UriKind.Absolute, out Uri absoluteUri))
                        {
                            links.Add(new LinkEntity() {Text= node.InnerText,UrlText= absoluteUri.ToString() });
                        }
                        else if (Uri.TryCreate(new Uri(baseUrl), href, out Uri combinedUri))
                        {
                            links.Add(new LinkEntity() { Text = node.InnerText, UrlText = combinedUri.ToString() });
                        }
                        else
                        {
                            links.Add(new LinkEntity() { Text = node.InnerText, UrlText = href });
                        }
                    }
                }
            }

            // 去重并排序
            return links.Distinct().OrderBy(l => l).ToList();
        }

        /// <summary>
        /// 检查HTML内容是否包含指定文本
        /// </summary>
        private bool CheckContentContains(string htmlContent, string searchText)
        {
            if (string.IsNullOrEmpty(htmlContent) || string.IsNullOrEmpty(searchText))
                return false;

            // 不区分大小写的检查
            return htmlContent.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        /// <summary>
        /// 从URL中提取基地址（协议+主机+非默认端口）
        /// </summary>
        /// <param name="url">完整URL</param>
        /// <returns>基地址，若URL无效则返回null</returns>
        public static string GetBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                Uri uri = new Uri(url);

                // 协议（如http、https）
                string scheme = uri.Scheme;

                // 主机名（域名或IP）
                string host = uri.Host;

                // 端口（处理默认端口的情况）
                int port = uri.Port;
                string portPart = string.Empty;

                // 判断是否为默认端口（http默认80，https默认443）
                if (!uri.IsDefaultPort)
                {
                    portPart = $":{port}";
                }

                // 组合基地址
                return $"{scheme}://{host}{portPart}";
            }
            catch (UriFormatException)
            {
                // URL格式无效
                return null;
            }
            catch (Exception)
            {
                // 其他异常
                return null;
            }
        }
    }
}
