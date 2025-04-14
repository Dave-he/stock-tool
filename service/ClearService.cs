using stock_tool.common;
using stock_tool.utils;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace stock_tool.service;

class ClearService
{

    private static ClearService? _instance;

    private Button _btn;

    private ClearService(Button clearBtn) {
        _btn = clearBtn;
        _btn.Click += CleanClick;
    }

    internal static void Init(Button clearBtn)
    {
        _instance = new ClearService(clearBtn);
    }
    public void CleanClick(object sender, RoutedEventArgs e) {
        ZipClick();
    }


    public async Task ZipClick()
    {
        _btn.Content = "清理中";
        try
        {
           
            MessageBoxResult res = MessageBox.Show("是否关闭ZYing?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                await FileUtil.CloseProcessByName("ZYing");
            }


            MessageBoxResult res2 = MessageBox.Show("是否清理图片?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res2 == MessageBoxResult.Yes)
            {
                await FileUtil.ZipDir(Config.Get("ImagePath"));
                await FileUtil.DeleteDir(Config.Get("ImagePath"));
                await FileUtil.DeleteSubDir(Config.Get("SaveCmpPath"));
            }

            MessageBoxResult res3 = MessageBox.Show("是否清理缓存?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res3 == MessageBoxResult.Yes)
            {
                await FileUtil.DeleteDir(Config.Get("Cache"));
            }
            Logger.Info($"全部清理成功！");
        }
        catch (Exception ex)
        {
            Logger.Error($"清理失败: {ex.Message}");
        }

        _btn.Content = "清理image";

    }

}
