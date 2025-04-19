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
    public event EventHandler StopEvent;

    private volatile bool processd = false;
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
                StopEvent?.Invoke(this, EventArgs.Empty);
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
                StopEvent?.Invoke(this, EventArgs.Empty);
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
                    processd = true;
                    Submit(maxCount, targetWindow);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"终止处理: {ex.Message}");
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

    private void click(AutomationElement targetWindow) {
        AutomationElement targetButton = FindElementById(targetWindow, GetConfigValue("SubmitBtn"));
        if (targetButton == null)
        {
            Logger.Info("未找到提交按钮。");
            return;
        }
        MouseSimulator.Click(GetElementCenter(targetButton));
    }


    private void Submit(int maxCount, AutomationElement targetWindow)
    {
        int errorTime = 1;
        HashSet<string> processed = new HashSet<string>();


        // 删除临时文件
        string resultPath = GetConfigValue("ResultFilePath");
        string resultFile = resultPath + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
  
        AutomationElement id = FindElementById(targetWindow, GetConfigValue("ID"));
        if ((id != null && Regex.IsMatch(id.Current.Name, @"^-?\d+$")) && errorTime < 15)
        {
            processed.Add(id.Current.Name);
        }

        DialogListener.Instance.Start();
        
        click(targetWindow);
        if (Config.Enable("FileSubmit")) {
            File.Delete(Config.GetDefault("FileSubmitPath", "submit.txt"));
            Thread.Sleep(2000 * (maxCount / 60));
        }
     
        string max = GetConfigValue("maxNum");
        int waitMill = Config.GetInt("WaitMillSeconds");
        int cycleTime = Config.GetInt("CycleTime", 20);
        while(processed.Count() < maxCount)
        {
           
            try
            {


                if (!MainWindow.IsProcess() || !processd)
                {
                    Stop();
                    // 如果标志为 false，终止处理
                    return;
                }


                if (DialogListener.needCheck && false)
                {
                    click(targetWindow);
                    DialogListener.needCheck = false;
                    Thread.Sleep(waitMill);
                    continue;
                }
                else {

                    //AutomationElement ok = FindElementById(targetWindow, Config.GetDefault("OK", "loaderOK"));
                    //if ((ok == null || ok.Current.Name != Config.GetDefault("OK_Result", "修改成功！")) && errorTime <= cycleTime)
                    //{
                    //    errorTime++;
                    //    Thread.Sleep(waitMill);
                    //    continue;
                    //}
                }
                 
                id = FindElementById(targetWindow, GetConfigValue("ID"));
                if ((id == null || !Regex.IsMatch(id.Current.Name, @"^-?\d+$")) && errorTime <= cycleTime)
                {
                    if (errorTime < cycleTime)
                    {

                        errorTime++;
                    }
                    else {
                        errorTime = 0;
                        Logger.Info($"多次未找到ID，已等待{cycleTime * waitMill /1000}秒，尝试刷新页面");
                        Refresh(targetWindow);
                    }
                    Thread.Sleep(waitMill);
                    //Refresh(targetWindow);
                    continue;
                }

                if (max != null && processed.Count() > 5 && processed.Count() % int.Parse(max) == 1)
                {
                    MessageBoxResult res = MessageBox.Show("已处理{max}个是否继续?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.No)
                    {
                        Stop();
                        return;
                    }
                }

                click(targetWindow);
                Thread.Sleep(50);
                StockInput.PressEnter();
                errorTime = 0;
                if (id != null)
                {
                    processed.Add(id.Current.Name);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"第{processed.Count()}个处理失败 重试【{errorTime}】: {ex.Message}");
                if (errorTime <= cycleTime)
                {
                    errorTime++;
                }
                else {
                    Refresh(targetWindow);
                }
            }
            Thread.Sleep(waitMill);
        }

        click(targetWindow);
        Logger.Info($"已处理 {processed.Count} 个商品，达到最大处理数量 终止。");


        try
        {
            string resText = null;
            string compare = Config.Enable("FileSubmit") ?  Config.GetDefault("FileSubmitPath", "submit.txt") : Config.Get("CompareFilePath");
            List<string> needProcess = !File.Exists(compare) ? new List<string>() : File.ReadAllLines(compare).Distinct().ToList();
            List<string> notProcess = new List<string>();
            foreach (string line in needProcess)
            {

                if (processed.Contains(line) || line.StartsWith("[Page"))
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
                resText = $"所有{maxCount},成功:{success} 未处理:{notProcess.Count}, 请查看{resultFile}";
                Logger.Info(resText);
            }
            else
            {
                resText = $"全部处理成功 {maxCount} ";
                Logger.Info(resText);
            }
            Stop();
            MessageBox.Show(resText);
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

    internal void Stop()
    {
        StopEvent?.Invoke(this, EventArgs.Empty);
        processd = false;
        DialogListener.Instance.Stop();
    }
}