using Microsoft.Extensions.Configuration;
using System.IO;
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

    public static string GetDefault(string key, string de) => Get(key) == null ? de : Get(key);

    public static bool Enable(string key) => Get(key) == "true";

    public static int GetInt(string key, int defalut = 0)
    {
        string value = Get(key);
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        return defalut;
    }
}
