using stock_tool.common;
using stock_tool.utils;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Automation;

namespace stock_tool.service;

class WhiteService
{
    public void WhiteClick(object sender, RoutedEventArgs e)
    {

        // 获取用户输入的源文件夹路径
        string sourceFolder = Config.Get("ImagePath");
        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");


        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                Logger.Error($"白框失败，源文件夹 {sourceFolder} 不存在");
                return;
            }
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
        }
        catch (Exception ex)
        {
            Logger.Error($"压缩失败: {ex.Message}");
        }
        Task.Run(async () =>
        {
            //List<Task> task = new List<Task>();
            try
            {
                var allFiles = Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories);

                List<Task> tasks = new List<Task>();
                foreach (var file in allFiles)
                {
                    tasks.Add(Task.Run(() => ProcessFile(file)));

                }
                foreach (var task in tasks)
                {
                    await task;
                }
                Logger.Info($"本地白框,处理完毕,共 {allFiles.Count()} ");
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Error("没有权限访问某些文件夹或文件。");
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("指定的文件夹未找到。");
            }
            //foreach(Task t in task) {
            //    t.Wait();
            //}
        });
    }

    private void WhiteTest(object sender, RoutedEventArgs e)
    {

        // 获取配置项
        string targetWindowTitle = Config.Get("TargetWindowTitle");
        string targetElementTitle = Config.Get("TargetElementTitle");

        // 查找目标窗口
        AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, targetWindowTitle));

        if (mainWindow == null)
        {
            Logger.Info($"找不到【{targetWindowTitle}】，请运行程序");
            return;
        }

        AutomationSearchHelper.TryActivateWindow(mainWindow);
        //max(mainWindow);
        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));
        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品并点击【库存】");
            return;
        }
        //ProcessSingle(mainWindow, targetWindow, 0, true, "WhiteRight");
    }


    private void ProcessFile(string filePath)
    {
        int width = int.Parse(Config.Get("WhiteSize"));
        try
        {
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(filePath))
            {
                using MemoryStream memoryStream = new MemoryStream();
                Save(memoryStream, 白框(image, width));
                memoryStream.Seek(0L, SeekOrigin.Begin);
                byte[] buffer = new byte[4096];
                using (FileStream fileStream = File.Open(filePath + ".白框", FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    int count;
                    while ((count = memoryStream.Read(buffer, 0, 4096)) > 0)
                    {
                        fileStream.Write(buffer, 0, count);
                    }
                }
            }
            File.Delete(filePath);
            File.Move(filePath + ".白框", filePath);
        }
        catch (Exception ex)
        {
            Logger.Error($"处理图片文件 {filePath} 时出错: {ex.Message}");
        }
    }


    private void Save(Stream sr, System.Drawing.Image img)
    {
        Bitmap bitmap = null;
        EncoderParameter encoderParameter = null;
        EncoderParameters encoderParameters = null;
        try
        {
            bitmap = new Bitmap(img.Width, img.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(img, 0, 0);
            }

            encoderParameters = new EncoderParameters(1);
            encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
            encoderParameters.Param[0] = encoderParameter;
            bitmap.Save(sr, ImageCodecInfo.GetImageEncoders()[1], encoderParameters);
        }
        finally
        {
            encoderParameter?.Dispose();
            encoderParameters?.Dispose();
            bitmap?.Dispose();
        }
    }


    private Bitmap 白框(System.Drawing.Image img, int whiteSize)
    {
        System.Drawing.Size size = img.Size;
        int num = Math.Max(size.Width, size.Height);
        int num2 = Math.Max(0, whiteSize - num);
        size.Width += num2;
        size.Height += num2;
        Bitmap bitmap = new Bitmap(size.Width, size.Height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.Clear(Color.White);
        graphics.DrawImage(img, new Rectangle(50, 50, bitmap.Width - 100, bitmap.Height - 100), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
        return bitmap;
    }

}
