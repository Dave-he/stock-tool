using stock_tool.common;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;

namespace stock_tool.service;

class ClearService
{

    private  static ClearService? _instance;

    private Button _btn;

    private ClearService(Button clearBtn) {
        _btn = clearBtn;
        _btn.Click += ZipClick;
    }

    internal static void Init(Button clearBtn)
    {
        _instance = new ClearService(clearBtn);
    }

    public void ZipClick(object sender, RoutedEventArgs e)
    {
        // 获取用户输入的源文件夹路径
        string sourceFolder = Config.Get("ImagePath");
        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        //Task.Run(() =>
        //{

            try
            {
                if (Directory.Exists(sourceFolder))
                {
                    // 获取源文件夹的上一级目录
                    DirectoryInfo parentDir = Directory.GetParent(sourceFolder);
                    if (parentDir != null)
                    {
                        string parentPath = parentDir.FullName;
                        string zipFileName = Path.Combine(parentPath, $"image_{timestamp}.zip");

                        Logger.Info($"开始压缩..{sourceFolder}");
                        ZipFile.CreateFromDirectory(sourceFolder, zipFileName);
                        Logger.Info($"压缩成功: {zipFileName}");
                    }
                    Directory.Delete(sourceFolder, true);
                    Logger.Info($"清理成功: {sourceFolder}");
                }

                string cachePath = Config.Get("Cache");
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                    Logger.Info($"缓存清理完毕: {cachePath}");
                }

                foreach (string dir in Directory.GetDirectories(Config.Get("SaveCmpPath")))
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                        Logger.Info($"缓存清理完毕: {dir}");
                    }
                }
                Logger.Info($"全部清理成功！");
            }
            catch (Exception ex)
            {
                Logger.Error($"清理失败: {ex.Message}");
            }
           
        //});
    }
}
