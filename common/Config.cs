using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stock_tool.common;

class Config
{
    private static IConfiguration _configuration = new ConfigurationBuilder().Build();


    public static void LoadConfiguration(string file)
    {
        try
        {
            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile(file, optional: false, reloadOnChange: true)
             .Build();
            return;
        }
        catch (Exception e)
        {

            Logger.Info($"读取配置文件失败: {e.Message}");
        }
    }

    public static string Get(string key) => _configuration[key];
}
