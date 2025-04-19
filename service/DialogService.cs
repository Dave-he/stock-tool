using stock_tool.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace stock_tool.service;

class DialogService
{

    private static DialogService? _instance;
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 1000;

    private bool isReversed = false;
    private Button btn;

    public static DialogService Instance => _instance;

    internal static void Init(Button dialogBtn)
    {
        _instance ??= new DialogService(dialogBtn);
    }

    public DialogService(Button button)
    {
        btn = button;
        btn.Click += Click;
        UpdateButtonState();
    }

    internal void Click(object sender, RoutedEventArgs e)
    {
  
        isReversed = isReversed ? stopListening(): startListening();  
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        btn.Content = isReversed ? "关闭监听" : "开始监听";
        btn.Background = isReversed ? Brushes.Red : Brushes.Green;
        btn.Foreground = isReversed ? Brushes.White : Brushes.Black;
    }

    public bool startListening()
    {
        AutomationElement mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            Logger.Error($"开启监听失败,未找到窗口!");
            return false;
        }

        // 注册窗口打开事件
        Automation.AddAutomationEventHandler(
            WindowPattern.WindowOpenedEvent,
            mainWindow,
            TreeScope.Children,
            HandleWindowOpened);
        return true;
    }

    public static AutomationElement GetMainWindow() {
        // 获取配置项
        string targetWindowTitle = Config.Get("TargetWindowTitle");
        return AutomationElement.RootElement.FindFirst(
                     TreeScope.Children,
                     new PropertyCondition(AutomationElement.NameProperty, targetWindowTitle));
    }

    public bool stopListening()
    {

        AutomationElement mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            Logger.Error($"移除监听失败,未找到窗口!");
            return true;
        }

       
        // 移除事件监听
        Automation.RemoveAutomationEventHandler(
            WindowPattern.WindowOpenedEvent,
            mainWindow,
            HandleWindowOpened);
        return false;
    }

    private static void HandleWindowOpened(object sender, AutomationEventArgs e)
    {
        var dialog = sender as AutomationElement;
        if (dialog != null && IsTargetDialog(dialog))
        {

            AutomationElement text = dialog.FindFirst(TreeScope.Children, 
                new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, "文本"));
            string content = text == null ? "文本未找到" : text.Current.Name;
            Logger.Info($"对话框 {dialog.Current.Name} {content}");
            TryCloseDialog(dialog);
        }
    }

    private static bool IsTargetDialog(AutomationElement element)
    {
        // 这里可以根据对话框的类名、标题等特征来判断是否为目标对话框
        return element.Current.ClassName == "Dialog"
            || element.Current.LocalizedControlType == "对话框"
            || element.Current.Name.Contains("错误")
            || element.Current.Name.Contains("信息")
            || element.Current.Name.Contains("确认");
    }

    private static void TryCloseDialog(AutomationElement dialog)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            if (CloseDialog(dialog))
            {
                Logger.Info("对话框已关闭。");
                return;
            }
            Logger.Info($"第 {i + 1} 次尝试关闭对话框失败，等待 {RetryDelayMs} 毫秒后重试。");
            Thread.Sleep(RetryDelayMs);
        }
        Logger.Info("所有尝试均失败，无法关闭对话框。");
    }

    private static bool CloseDialog(AutomationElement dialog)
    {
        try
        {
            //// 尝试查找关闭按钮（叉掉）
            //var closeButton = FindCloseButton(dialog);
            
            //if (closeButton != null)
            //{
            //    var invokePattern = closeButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            //    invokePattern?.Invoke();
            //    return true;
            //}

            // 若未找到关闭按钮，尝试按下回车键
            PressEnter();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"关闭对话框时出错：{ex.Message}");
            return false;
        }
    }

    private static AutomationElement FindCloseButton(AutomationElement dialog)
    {
        var condition = new PropertyCondition(AutomationElement.NameProperty, "关闭");
        return dialog.FindFirst(TreeScope.Children, condition);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private static void PressEnter()
    {
        const byte VK_RETURN = 0x0D;
        const uint KEYEVENTF_KEYUP = 0x0002;

        // 按下回车键
        keybd_event(VK_RETURN, 0, 0, 0);
        // 释放回车键
        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
    }

  
}
