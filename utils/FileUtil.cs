using stock_tool.common;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace stock_tool.utils;


class FileUtil
{
    public static async Task CloseProcessByName(string processName, int time = 2)
    {
        for (int i = 0; i < time; i++)
        {
            try
            {
                // 获取所有指定名称的进程
                Process[] processes = Process.GetProcessesByName(processName);

                foreach (Process process in processes)
                {
                    // 尝试关闭进程
                    if (!process.HasExited)
                    {
                        process.Kill();
                        await process.WaitForExitAsync();
                        Logger.Info($"已成功关闭进程: {processName}");
                    }
                }
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Logger.Error($"关闭进程时出现错误: {ex.Message}");
            }
        }
    }

    public static async Task ZipDir(string sourceFolder)
    {
        // 获取用户输入的源文件夹路径
        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        try
        {
            if (Directory.Exists(sourceFolder))
            {
                // 获取源文件夹的上一级目录
                DirectoryInfo? parentDir = Directory.GetParent(sourceFolder);
                if (parentDir != null)
                {
                    string parentPath = parentDir.FullName;
                    string zipFileName = Path.Combine(parentPath, $"image_{timestamp}.zip");

                    Logger.Info($"开始压缩..{sourceFolder}");
                    ZipFile.CreateFromDirectory(sourceFolder, zipFileName);
                    Logger.Info($"压缩成功: {zipFileName}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"压缩失败: {ex.Message}");
        }
    }

    public static async Task DeleteDir(string dir) {
        await Task.Run(() =>
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                Logger.Info($"清理成功: {dir}");
            }
        });
    }

    public static async Task DeleteSubDir(string dir)
    {
        List<Task> tasks = new List<Task>();
        foreach (string subDir in Directory.GetDirectories(dir))
        {
            tasks.Add(Task.Run(() =>
            {
                if (Directory.Exists(subDir))
                {
                    Directory.Delete(subDir, true);
                    Logger.Info($"清理成功: {subDir}");
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

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

    public static void Delete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Logger.Error($"删除文件 {filePath} 时出错: {ex.Message}");
        }
    }
}
