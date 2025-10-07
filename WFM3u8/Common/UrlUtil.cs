using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WFM3u8.Common
{
    public class UrlUtil
    {
        /// <summary>
        /// 判断是不是url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取基地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetBaseUrl(string url)
        {
            string re = string.Empty;
            int index = url.IndexOf("?");
            if (index > -1)
            {
                re = url.Substring(0, index);
            }
            else
            {
                re = url;
            }
                return re;
        }
        /// <summary>
        /// 获取下载文件的扩展名
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetExtension(string url)
        {
            string re = "";
            string tmp = GetBaseUrl(url);
            re = Path.GetExtension(tmp);
            return re;
        }
        public static string GetFileName(string url)
        {
            string re = "";
            string tmp = GetBaseUrl(url);
            re = Path.GetFileName(tmp);
            return re;
        }
        /// <summary>
        /// 替换或添加 URL 中的指定参数
        /// </summary>
        /// <param name="url">原始 URL（支持绝对路径和相对路径）</param>
        /// <param name="paramName">参数名</param>
        /// <param name="newValue">参数新值</param>
        /// <returns>处理后的 URL</returns>
        public static string ReplaceUrlParameter(string url, string paramName, string newValue)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentNullException(nameof(paramName));

            // 解析URL
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                // 如果不是绝对URL，尝试作为相对URL处理
                uri = new Uri(new Uri("http://tempuri.org"), url);
            }

            // 解析查询字符串
            NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);

            // 替换或添加参数
            queryParams[paramName] = newValue;

            // 重建URL
            UriBuilder uriBuilder = new UriBuilder(uri)
            {
                Query = queryParams.ToString()
            };

            // 如果原始URL是相对路径，返回相对路径形式
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return uriBuilder.Uri.PathAndQuery + uriBuilder.Uri.Fragment;
            }

            return uriBuilder.ToString();
        }
    }
}
