using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WFM3u8
{
    public class StaticData
    {
        public static string ProxyHost { get; set; }
        public static int ProxyPort { get; set; }
        public static bool UseProxy { get; set; } = false;
        public static string Export { get; set; }
        public static string Ffmpeg { get; set; }

        public static string Proxy
        {
            get {
                return $@"http://{ProxyHost}:{ProxyPort}";
            }
        }

        public static void Load()
        {
            int int1;
            bool bool1;
            ProxyHost = ConfigurationManager.AppSettings["proxy_host"];
            string proxy_port = ConfigurationManager.AppSettings["proxy_port"];
            if (int.TryParse(proxy_port, out int1))
            {
                ProxyPort = int.Parse(proxy_port);
            }
            string use_proxy = ConfigurationManager.AppSettings["use_proxy"];
            if (bool.TryParse(use_proxy, out bool1))
            {
                UseProxy = bool.Parse(use_proxy);
            }
            Export = ConfigurationManager.AppSettings["export"];
            Ffmpeg = ConfigurationManager.AppSettings["ffmpeg"];
        }
        public static void Update()
        {
            UpdateAppSetting("proxy_host", ProxyHost);
            UpdateAppSetting("proxy_port", ProxyPort.ToString());
            UpdateAppSetting("use_proxy", UseProxy.ToString());
            UpdateAppSetting("export", Export);
            UpdateAppSetting("ffmpeg", Ffmpeg);
        }
        /// <summary>
        /// 修改或添加appSettings中的配置项
        /// </summary>
        /// <param name="key">配置项的键</param>
        /// <param name="newValue">要设置的新值</param>
        /// <returns>是否修改成功</returns>
        public static bool UpdateAppSetting(string key, string newValue)
        {
            try
            {
                // 1. 打开应用程序配置文件（获取可写的配置对象）
                // ConfigurationUserLevel.None 表示操作应用程序级配置（非用户级）
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // 2. 检查键是否已存在：存在则移除旧值，不存在则直接添加
                if (config.AppSettings.Settings[key] != null)
                {
                    // 移除旧值
                    config.AppSettings.Settings.Remove(key);
                }
                // 添加新值
                config.AppSettings.Settings.Add(key, newValue);

                // 3. 保存配置修改（写入到exe.config文件）
                config.Save(ConfigurationSaveMode.Modified);

                // 4. 刷新配置，使修改立即生效（否则ConfigurationManager读取的还是旧值）
                ConfigurationManager.RefreshSection("appSettings");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改配置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
