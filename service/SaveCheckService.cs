using Microsoft.Win32;
using stock_tool.common;
using stock_tool.utils;
using System.IO;
using System.Text.Json.Nodes;
using System.Windows;

namespace stock_tool.service;

class SaveCheckService
{
    public void SaveCheck(object sender, RoutedEventArgs e)
    {
        string savePath = Path.GetFullPath(Config.Get("SaveCmpPath"));
        string path = FileUtil.GetLatestCreatedFolder(savePath);
        var openFileDialog = new OpenFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "文件夹选择",
            Filter = "文件夹|.",
            ValidateNames = false,
            InitialDirectory = savePath
        };

        if (openFileDialog.ShowDialog() == true)
        {
            path = Path.GetDirectoryName(openFileDialog.FileName);
        }


        Task.Run(() =>
        {

            int size = 0;
            foreach (string fileName in Directory.GetFiles(path))
            {
                try
                {
                    if (!Path.GetFileName(fileName).StartsWith("text-"))
                    {
                        continue;
                    }
                    size++;
                    string text = File.ReadAllText(Path.Combine(path, fileName));
                    // 解析 JSON 字符串
                    JsonNode jsonNode = JsonNode.Parse(text);

                    // 获取 root 数组中的第一个元素
                    JsonNode rootItem = jsonNode["root"][0];

                    string id = rootItem["sale_id"].AsValue().ToString();

                    // 获取 sale_pic 数组
                    JsonArray salePicArray = rootItem["sale_pic"].AsArray();

                    // 获取图片个数
                    int pictureCount = salePicArray.Count;
                    if (pictureCount == 0)
                    {
                        Logger.Info($"{id}  sale_pic为空");
                        continue;
                    }

                    string imagePath = Path.Combine(Config.Get("ImagePath"), id);
                    if (!Directory.Exists(imagePath))
                    {
                        Logger.Info($"{id}  里【{pictureCount}】张,全部未下载");
                    }
                    else
                    {
                        int targetCount = Directory.GetFiles(imagePath).Length;
                        if (targetCount != pictureCount)
                        {
                            Logger.Info($"{id}  里部分【{pictureCount - targetCount}】张,未下载");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"检测失败{fileName}");
                }
            }
            Logger.Info($"检测完毕,共{size}个");
        });
    }
}
