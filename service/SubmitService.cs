

using stock_tool.common;
using stock_tool.utils;
using System.Windows.Automation;
using System.Windows;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Windows.Input;

namespace stock_tool.service;

class SubmitService
{

    public void SubmitClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
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

        //删除临时文件
        string resultPath = Config.Get("ResultFilePath");
        string resultFile = resultPath + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
        File.Delete(Config.Get("CompareFilePath"));
        int maxCount = GetMaxCount(mainWindow);

        //DialogService.Instance.startListening();
        Task.Run(() => {
            try
            {
                Submit(maxCount, targetWindow);
            }
            catch (Exception e) { 
                Logger.Error($"终止处理: {e.Message}");
            }

           // DialogService.Instance.stopListening();
        });
    }

    private AutomationElement refresh(AutomationElement targetWindow)
    {
        AutomationElement refreshButton = targetWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, Config.Get("RefreshBtn")));
        if (refreshButton == null)
        {
            Logger.Info("未找到刷新按钮。");
            return null;
        }
        Rect rect = refreshButton.Current.BoundingRectangle;
        // 计算元素的中心位置
        int x = (int)(rect.Left + rect.Width / 2);
        int y = (int)(rect.Top + rect.Height / 2);
        MouseSimulator.Move(x, y);
        MouseSimulator.MouseClick(true, x, y);
        return null;
    }

    private void Submit(int maxCount, AutomationElement targetWindow)
    {
        int errorTime = 1;
        HashSet<string> processed = new HashSet<string>();
        AutomationElement targetButton = targetWindow.FindFirst(TreeScope.Descendants,
       new PropertyCondition(AutomationElement.AutomationIdProperty, Config.Get("SubmitBtn")));

        if (targetButton == null)
        {
            Logger.Info("未找到提交按钮。");
            return;
        }
        Rect rect = targetButton.Current.BoundingRectangle;
        // 计算元素的中心位置
        int x = (int)(rect.Left + rect.Width / 2);
        int y = (int)(rect.Top + rect.Height / 2);

string max =Config.Get("maxNum")
        for (int i = 1; i <= maxCount; i++)
        {
            try
            {
                if (!MainWindow.IsProcess())
                {
                    // 如果标志为 false，终止处理
                    return;
                }
;
                if (max != null && i>1 && i % int.Parse(max)==1)
                {
                    MessageBox.Show($"已处理{max}个是否继续?");
                }

                int waitTime = int.Parse(Config.GetDefault("WaitTime", "600"));
                AutomationElement id = Retry.RunAndTry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, Config.Get("ID")),
                    () => refresh(targetWindow), waitTime, 2000);
                while (id == null || !Regex.IsMatch(id.Current.Name, @"^-?\d+$"))
                {

                    refresh(targetWindow);
                    Thread.Sleep(1000);
                    Logger.Info($"第{i}个 id找不到刷新重试");
                    id = Retry.RunAndTry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, Config.Get("ID")),
                        () => refresh(targetWindow), waitTime, 2000);
                    errorTime++;
                    if (errorTime > 10) {
                        break;
                    }
                }
                Logger.Info($"第{i}个: {id.Current.Name}");
                processed.Add(id.Current.Name);
                i = processed.Count;
                MouseSimulator.Move(x, y);
                AutomationElement submit = targetWindow.FindFirst(TreeScope.Descendants,
                 new PropertyCondition(AutomationElement.NameProperty, "确认"));
                if (submit != null)
                {
                    StockInput.PressEnter();
                    Thread.Sleep(300);
                }
                MouseSimulator.MouseClick(true, x, y);
                Thread.Sleep(200);
                StockInput.PressY();
                Thread.Sleep(100);
                StockInput.PressEnter();
                Thread.Sleep(int.Parse(Config.Get("WaitMillSeconds")));
                errorTime = 0;

                if (processed.Count >= maxCount)
                {
                    Logger.Info($"已处理 {processed.Count} 个商品，达到最大处理数量 终止。");
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"第{i}个处理失败 重试【{errorTime}】: {ex.Message}");
                refresh(targetWindow);
                if (errorTime <= 10) {
                    i++;
                    errorTime++;
                }
               
            }
        }
    }




    private int GetMaxCount(AutomationElement mainWindow)
    {

        AutomationElement total = AutomationSearchHelper.FindFirstElementById(mainWindow, "lblTotal");

        Match match = Regex.Match(total.Current.Name, @"\d+");
        if (match.Success)
        {
            return int.Parse(match.Value);
         
        }
        return 0;
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

}
