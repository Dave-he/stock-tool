using stock_tool.common;
using stock_tool.utils;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Automation;

namespace stock_tool.service;

class SubmitService
{

    public void SubmitClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Logger.Info("开始处理所有....");

        try
        {
            // 获取配置项
            string targetWindowTitle = GetConfigValue("TargetWindowTitle");
            string targetElementTitle = GetConfigValue("TargetElementTitle");

            IntPtr mainWindowHandle = WindowApi.FindAndActivateWindow(targetWindowTitle);
            if (mainWindowHandle == IntPtr.Zero)
            {
                Logger.Info($"找不到窗口: {targetWindowTitle}");
                return;
            }

            // 将窗口句柄转换为 AutomationElement
            AutomationElement mainWindow = AutomationElement.FromHandle(mainWindowHandle);
            AutomationSearchHelper.MaximizeWindow(mainWindow);
            // 查找右边 pannel
            AutomationElement targetWindow = FindElement(mainWindow, targetElementTitle);
            if (targetWindow == null)
            {
                Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品");
                return;
            }

        
            FileUtil.Delete(GetConfigValue("CompareFilePath"));

            int maxCount = GetMaxCount(mainWindow);

            // 异步处理任务
            Task.Run(() =>
            {
                try
                {
                    Submit(maxCount, targetWindow);
                }
                catch (Exception ex)
                {
                    Logger.Error($"终止处理: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"处理过程中出现错误: {ex.Message}");
        }
    }

    private string GetConfigValue(string key)
    {
        string value = Config.Get(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"配置项 {key} 为空或未找到");
        }
        return value;
    }

    private AutomationElement FindElement(AutomationElement rootElement, string elementTitle)
    {
        return rootElement.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, elementTitle));
    }

    private int GetMaxCount(AutomationElement mainWindow)
    {
        AutomationElement total = FindElementById(mainWindow, "lblTotal");
        if (total != null)
        {
            string totalText = total.Current.Name;
            Match match = Regex.Match(totalText, @"\d+");
            if (match.Success)
            {
                return int.Parse(match.Value);
            }
        }
        return 0;
    }

    private AutomationElement FindElementById(AutomationElement rootElement, string automationId)
    {
        return rootElement.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
    }

    private void Submit(int maxCount, AutomationElement targetWindow)
    {
        int errorTime = 1;
        HashSet<string> processed = new HashSet<string>();

        AutomationElement targetButton = FindElementById(targetWindow, GetConfigValue("SubmitBtn"));
        if (targetButton == null)
        {
            Logger.Info("未找到提交按钮。");
            return;
        }

        // 删除临时文件
        string resultPath = GetConfigValue("ResultFilePath");
        string resultFile = resultPath + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
        Point buttonCenter = GetElementCenter(targetButton);

        int x = (int)buttonCenter.X;
        int y = (int)buttonCenter.Y;
        MouseSimulator.Click(x, y);
        Thread.Sleep(100);
        StockInput.PressY();
        StockInput.PressEnter();

        string max = GetConfigValue("maxNum");

        for (int i = 1; i <= maxCount; i++)
        {
            try
            {
                if (!MainWindow.IsProcess())
                {
                    // 如果标志为 false，终止处理
                    return;
                }

                if (max != null && i > 1 && i % int.Parse(max) == 1)
                {
                    MessageBoxResult res = MessageBox.Show("已处理{max}个是否继续?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                int waitTime = Config.GetInt("WaitTime", 600);
                AutomationElement id = Retry.RunAndTry(() => FindElementById(targetWindow, GetConfigValue("ID")),
                    () => Refresh(targetWindow), waitTime, 2000);

                while (id == null || !Regex.IsMatch(id.Current.Name, @"^-?\d+$"))
                {
                    Refresh(targetWindow);
                    Thread.Sleep(1000);
                    Logger.Info($"第{i}个 id找不到刷新重试");
                    id = Retry.RunAndTry(() => FindElementById(targetWindow, GetConfigValue("ID")),
                        () => Refresh(targetWindow), waitTime, 2000);
                    errorTime++;
                    if (errorTime > 10)
                    {
                        break;
                    }
                }

                if (id != null)
                {
                    Logger.Info($"第{i}个: {id.Current.Name}");
                    processed.Add(id.Current.Name);
                    i = processed.Count;

                    MouseSimulator.Click(x, y);
                    Thread.Sleep(100);
                    StockInput.PressEnter();
                    StockInput.PressEnter();


                    if (processed.Count >= maxCount)
                    {
                        Logger.Info($"已处理 {processed.Count} 个商品，达到最大处理数量 终止。");
                        break;
                    }
                    Thread.Sleep(Config.GetInt("WaitMillSeconds"));
                    errorTime = 0;
                   
                }

                
            }
            catch (Exception ex)
            {
                Logger.Error($"第{i}个处理失败 重试【{errorTime}】: {ex.Message}");
                Refresh(targetWindow);
                if (errorTime <= 10)
                {
                    i--;
                    errorTime++;
                }
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
                int success = maxCount == 0 ? 1 : maxCount - notProcess.Count;
                Logger.Info($"所有{maxCount},成功:{success} 未处理:{notProcess.Count}, 请查看{resultFile}");
            }
            else
            {

                Logger.Info($"全部处理成功 {maxCount}");
            }
        }
        catch { }
    }

    private AutomationElement Refresh(AutomationElement targetWindow)
    {
        StockInput.PressEnter();
        AutomationElement refreshButton = FindElementById(targetWindow, GetConfigValue("RefreshBtn"));
        if (refreshButton == null)
        {
            Logger.Info("未找到刷新按钮。");
            return null;
        }

        Point buttonCenter = GetElementCenter(refreshButton);
        MouseSimulator.Click(buttonCenter);
        return null;
    }

    private Point GetElementCenter(AutomationElement element)
    {
        Rect rect = element.Current.BoundingRectangle;
        return new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
    }
}