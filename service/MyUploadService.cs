using stock_tool.common;
using stock_tool.utils;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using System.Windows;

namespace stock_tool.service;

internal class MyUploadService
{

    public event EventHandler StopEvent;

    private volatile bool processd = false;

    public void UploadClick(string buttonName = "RefreshBtn")
    {
       
        Logger.Info("开始处理所有....");

        try
        {
            // 获取配置项
            string targetWindowTitle = GetConfigValue("TargetWindowTitle");
            string targetElementTitle = GetConfigValue("TargetElementTitle");

            nint mainWindowHandle = WindowApi.FindAndActivateWindow(targetWindowTitle);
            if (mainWindowHandle == nint.Zero)
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

                    DialogListener.Instance.Start(maxCount);
                    Thread.Sleep(200);
                    Click(targetWindow, buttonName);

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"终止处理: {ex.Message}", "错误");
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

    private AutomationElement Click(AutomationElement targetWindow, string buttonName)
    {
        StockInput.PressEnter();
        AutomationElement refreshButton = FindElementById(targetWindow, GetConfigValue(buttonName));
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