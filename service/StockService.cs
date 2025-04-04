using stock_tool.common;
using stock_tool.utils;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;

namespace stock_tool.service;

class StockService
{


    public void StockClick(object sender, RoutedEventArgs e)
    {
        Logger.Info("开始处理所有....");
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
        max(mainWindow);
        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));


        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品");
            return;
        }
        int maxCount = GetMaxCount(mainWindow);
        ProcessSingle(mainWindow, targetWindow, maxCount, true, "StockRight");

    }

    private void ProcessSingle(AutomationElement mainWindow, AutomationElement targetWindow,
        int time = 0, bool submit = true, string right = "StockRight")
    {
        Logger.Info("开始执行....");

        // 假设要点击窗口内的坐标 (100, 200)，可根据实际情况修改

        //删除临时文件
        string resultPath = Config.Get("ResultFilePath");
        string resultFile = resultPath + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
        File.Delete(Config.Get("CompareFilePath"));

        Rect rectangle = targetWindow.Current.BoundingRectangle;
        int x = (int)rectangle.Right - ConvertFromConfig("RefreshRight", true);
        int y = (int)targetWindow.Current.BoundingRectangle.Top + ConvertFromConfig("RefreshTop", false);



        MouseSimulator.Click(x, y);
        Logger.Info($"点击刷新图标 x:{x} y:{y}");
        Thread.Sleep(500);
        //PrintElementInfo(mainWindow);
        AutomationElement productId = Retry.Run(() => AutomationSearchHelper.FindFirstElementById(targetWindow, Config.Get("ID")), 10, 500);
        AutomationElement subject = Retry.Run(() => AutomationSearchHelper.FindFirstElementById(targetWindow, Config.Get("Subject")), 10, 500);
        //AutomationElement submit = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, Config.Get("Submit"]), 10, 500);
        if (subject == null)
        {
            Logger.Info($"刷新失败! 请检查配置项【Refresh】");
            return;
        }
        else
        {
            Logger.Info($"刷新成功---ID: {productId.Current.Name}");
        }

       
        Task.Run(() =>
        {
            string last_id = "";
            List<string> processed = new List<string>();
            int errorTime = 0;
            for (int i = 0; i <= time; i++)
            {
                if (!MainWindow.IsProcess())
                {
                    // 如果标志为 false，终止处理
                    return;
                }
                try
                {
                    if (Config.Get("maxNum") != null && i >= int.Parse(Config.Get("maxNum")))
                    {
                        MessageBox.Show("已处理250个是否继续?");
                    }


                    AutomationElement id = Retry.Run(() => AutomationSearchHelper.FindFirstElementById(targetWindow, Config.Get("ID")), 20, 100);
                    if (id == null || !Regex.IsMatch(id.Current.Name, @"^-?\d+$"))
                    {
                        throw new Exception("ID错误");
                    }

                    int stock = (int)rectangle.Right - ConvertFromConfig(right, true);

                    MouseSimulator.Click(stock, y);
                    Logger.Info($"{id.Current.Name},第{i}个,点击库存图标 x:{stock} y:{y}");
                    last_id = id.Current.Name;

                    if (submit)
                    {
                        Thread.Sleep(200);
                        StockInput.PressY();
                        Thread.Sleep(100);
                        StockInput.PressEnter();
                        Thread.Sleep(int.Parse(Config.Get("WaitMillSeconds")));
                    }
                    else
                    {
                        StockInput.PressN();
                        Thread.Sleep(100);

                    }


                    processed.Add(id.Current.Name);

                    //AutomationElement element = AutomationSearchHelper.FindFirstElementByName(mainWindow, "错误");
                    //if (element != null)
                    //{
                    //    AutomationElement yes = AutomationSearchHelper.FindFirstElementByName(mainWindow, "确定");
                    //    // 检查按钮是否支持 Invoke 模式（即可点击）
                    //    if (yes.TryGetCurrentPattern(InvokePattern.Pattern, out object invokePatternObject))
                    //    {
                    //        InvokePattern invokePattern = (InvokePattern)invokePatternObject;
                    //        // 点击按钮
                    //        invokePattern.Invoke();

                    //    }
                    //}
                    errorTime = 0;
                }
                catch (Exception ex)
                {
                    if (errorTime < 10)
                    {
                        Logger.Info($"发生错误, 尝试刷新x:{x} y:{y}: {ex.Message}");
                        errorTime++;
                        i--;
                    }
                    else
                    {
                        errorTime = 0;
                        Logger.Info("发生错误, 已经刷新了10次，跳过该商品");
                    }

                    MouseSimulator.Click(x, y);
                    Thread.Sleep(100);
                    StockInput.PressEnter();
                    Thread.Sleep(500);
                }


            }

            try
            {
                string compare = Config.Get("CompareFilePath");
                List<string> needProcess = !File.Exists(compare) ? new List<string>() : File.ReadAllLines(compare).Distinct().ToList();
                List<string> notProcess = new List<string>();
                foreach (string line in needProcess)
                {

                    if (processed.Contains(line))
                    {
                        //File.AppendLines(resultFile, line);
                    }
                    else
                    {
                        notProcess.Add(line);
                    }
                }

                if (notProcess.Count > 0)
                {
                    File.WriteAllLines(resultFile, notProcess);
                    int success = time == 0 ? 1 : time - notProcess.Count;
                    Logger.Info($"所有{time},成功:{success} 未处理:{notProcess.Count}, 请查看{resultFile}");
                }
                else
                {

                    Logger.Info($"全部处理成功 {time}");
                }
            } catch { }

            //int x = (int)targetWindow.Current.BoundingRectangle.Right - ConvertFromConfig("RefreshRight", true);
            int close_x = (int)targetWindow.Current.BoundingRectangle.Right - ConvertFromConfig("CloseDiff", true);
            //int y = (int)targetWindow.Current.BoundingRectangle.Top + ConvertFromConfig("RefreshTop", false);
            MouseSimulator.Click(close_x, y);
            Logger.Info($"点击关闭图标 x:{x} y:{y}");

            try
            {
                // 创建一个 ProcessStartInfo 对象
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "taskkill /F /IM ZYing.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                // 创建一个 Process 对象
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    // 启动进程
                    process.Start();
                    // 读取命令输出
                    string output = process.StandardOutput.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"发生错误: {ex.Message}");
            }
           
        });


    }


    private int GetMaxCount(AutomationElement mainWindow)
    {

        AutomationElement total = AutomationSearchHelper.FindFirstElementById(mainWindow, "lblTotal");

        Match match = Regex.Match(total.Current.Name, @"\d+");
        if (match.Success)
        {
            int totalNum = int.Parse(match.Value);
            return totalNum + (totalNum / 60) + 5;
        }
        return 0;
    }

    private void SaveTest(object sender, RoutedEventArgs e)
    {

    }



    private void max(AutomationElement targetWindow)
    {
        // 检查窗口是否支持窗口模式切换
        if (targetWindow.TryGetCurrentPattern(WindowPattern.Pattern, out object windowPatternObject))
        {
            WindowPattern windowPattern = (WindowPattern)windowPatternObject;

            // 检查窗口是否可以最大化
            if (windowPattern.Current.CanMaximize)
            {
                // 执行最大化操作
                windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
                Logger.Info("窗口已最大化。");
            }
            else
            {
                Logger.Info("窗口不支持最大化操作。");
            }
        }
    }


    public void StockTest(object sender, RoutedEventArgs e)
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
        max(mainWindow);
        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));
        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品并点击【库存】");
            return;
        }
        ProcessSingle(mainWindow, targetWindow);
    }

    public void PutStock(int x, int y)
    {
        double width = SystemParameters.PrimaryScreenWidth;   // 逻辑宽度（像素）
        double height = SystemParameters.PrimaryScreenHeight; // 逻辑高度（像素）
        int x_offset = ConvertFromConfig("XOffset", true);
        int y_offset = ConvertFromConfig("YOffset", false);

        // 生成20 - 100之间的随机数
        string num = Config.Get("Num");
        int randomNumber = 100;
        if (num.Contains("-"))
        {
            string[] split = num.Split("-");
            int min = int.Parse(split[0]);
            int max = int.Parse(split[1]);
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }
            randomNumber = new Random().Next(min, max);
            num = randomNumber.ToString();
        }
        StockInput.Input(x, y, num, y_offset, x_offset);
    }



    public void PageTest(object sender, RoutedEventArgs e)
    {
 
        Logger.Info("开始处理所有....");
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
        max(mainWindow);
        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));


        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品");
            return;
        }
        int maxCount = GetMaxCount(mainWindow);
        ProcessSingle(mainWindow, targetWindow, maxCount, false);
    }


    private int ConvertFromConfig(string config, bool isWidth)
    {

        double width = SystemParameters.PrimaryScreenWidth;   // 逻辑宽度（像素）
        double height = SystemParameters.PrimaryScreenHeight; // 逻辑高度（像素）
        return (int)(isWidth ? int.Parse(Config.Get(config)) * 1280 / width
            : int.Parse(Config.Get(config)) * 720 / height);
    }

}
