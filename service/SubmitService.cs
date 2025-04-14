using stock_tool.common;
using stock_tool.utils;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Automation;

namespace stock_tool.service;

class SubmitService
{
    // Windows API 函数声明
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const int SW_MAXIMIZE = 3;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public void SubmitClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Logger.Info("开始处理所有....");

        try
        {
            // 获取配置项
            string targetWindowTitle = GetConfigValue("TargetWindowTitle");
            string targetElementTitle = GetConfigValue("TargetElementTitle");

            // 查找目标窗口
            IntPtr mainWindowHandle = FindWindow(null, targetWindowTitle);
            if (mainWindowHandle == IntPtr.Zero)
            {
                Logger.Info($"找不到【{targetWindowTitle}】，请运行程序");
                return;
            }

            SetForegroundWindow(mainWindowHandle);
            //ActivateAndMaximizeWindow(mainWindowHandle);

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

            // 删除临时文件
            string resultPath = GetConfigValue("ResultFilePath");
            string resultFile = resultPath + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
            DeleteFile(GetConfigValue("CompareFilePath"));

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

    private void ActivateAndMaximizeWindow(IntPtr hWnd)
    {
        SetForegroundWindow(hWnd);
        //ShowWindow(hWnd, SW_MAXIMIZE);
        //Logger.Info("窗口已最大化。");
    }

    private AutomationElement FindElement(AutomationElement rootElement, string elementTitle)
    {
        return rootElement.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, elementTitle));
    }

    private void DeleteFile(string filePath)
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
      

        Point buttonCenter = GetElementCenter(targetButton);

        int x = (int)buttonCenter.X;
        int y = (int)buttonCenter.Y;
        MouseSimulator.Move(x, y);
        MouseSimulator.Click(x, y);
        Thread.Sleep(100);
        StockInput.PressY();
        StockInput.PressEnter();
        //StockInput.PressEnter();

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
                    MessageBox.Show($"已处理{max}个是否继续?");
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

                    MouseSimulator.Move(x,y);
                    MouseSimulator.Click(x, y);
                    Thread.Sleep(100);
                    StockInput.PressY();
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
    }

    private AutomationElement Refresh(AutomationElement targetWindow)
    {
        keybd_event(0x0D, 0, 0, 0); // 模拟按下 Enter 键
        Thread.Sleep(100);
        keybd_event(0x0D, 0, KEYEVENTF_KEYUP, 0); // 模拟释放 Enter 键

        AutomationElement refreshButton = FindElementById(targetWindow, GetConfigValue("RefreshBtn"));
        if (refreshButton == null)
        {
            Logger.Info("未找到刷新按钮。");
            return null;
        }

        Point buttonCenter = GetElementCenter(refreshButton);
        SetCursorPos((int)buttonCenter.X, (int)buttonCenter.Y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, (int)buttonCenter.X, (int)buttonCenter.Y, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, (int)buttonCenter.X, (int)buttonCenter.Y, 0, 0);
        return null;
    }

    private Point GetElementCenter(AutomationElement element)
    {
        System.Windows.Rect rect = element.Current.BoundingRectangle;
        return new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
    }

    private bool IsValidId(AutomationElement element)
    {
        return Regex.IsMatch(element.Current.Name, @"^-?\d+$");
    }
}