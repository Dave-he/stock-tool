using stock_tool.common;
using System.IO;

namespace stock_tool.utils;


class FileUtil
{
    public static string GetLatestCreatedFolder(string path)
    {
        // 检查指定路径是否存在
        if (!Directory.Exists(path))
        {
           Logger.Error($"指定的文件夹路径 {path} 不存在。");
        }

        // 获取指定路径下的所有子文件夹
        string[] directories = Directory.GetDirectories(path);

        if (directories.Length == 0)
        {
            return "";
        }

        // 假设第一个文件夹是最新创建的
        string latestFolder = directories[0];
        DateTime latestCreationTime = Directory.GetCreationTime(latestFolder);

        // 遍历所有子文件夹
        foreach (string directory in directories)
        {
            DateTime creationTime = Directory.GetCreationTime(directory);
            if (creationTime > latestCreationTime)
            {
                latestCreationTime = creationTime;
                latestFolder = directory;
            }
        }
        
        return latestFolder;
    }
}
