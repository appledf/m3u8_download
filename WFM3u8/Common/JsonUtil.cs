using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFM3u8.Common
{
    public class JsonUtil
    {
        /// <summary>
        /// 将对象序列化为JSON并写入文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="data">要序列化的对象</param>
        /// <param name="filePath">文件路径</param>
        public static void WriteJsonToFile<T>(T data, string filePath)
        {
            try
            {
                // 序列化对象为JSON字符串，设置格式化以提高可读性
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);

                // 写入文件
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入JSON文件时出错: {ex.Message}");
            }
        }
        /// <summary>
        /// 从文件读取JSON并反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>反序列化后的对象</returns>
        public static T ReadJsonFromFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("文件不存在");
                    return default(T);
                }

                // 读取文件内容
                string json = File.ReadAllText(filePath);

                // 反序列化为对象
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取JSON文件时出错: {ex.Message}");
                return default(T);
            }
        }
    }
}
