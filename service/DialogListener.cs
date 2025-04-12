using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using stock_tool.common;
using stock_tool.utils;
using System.Windows.Navigation;

namespace stock_tool.service;

class DialogListener
{

    // 导入 Windows API 函数
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    // 定义回调函数委托
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // 存储已检测到的对话框句柄
    private List<IntPtr> detectedDialogs = new List<IntPtr>();
    private Thread listenerThread;
    private bool isListening = false;

    private Button _btn;

    private static DialogListener? _instance;

    public static DialogListener Instance => _instance;

    internal static void Init(Button dialogBtn)
    {
        _instance ??= new DialogListener(dialogBtn);
    }

    public DialogListener(Button button)
    {
        _btn = button;
        _btn.Click += StartStopButton_Click;
        UpdateButtonState();
    }


    private void UpdateButtonState()
    {
        _btn.Content = isListening ? "停止" : "监听";
        _btn.Background = isListening ? Brushes.Red : Brushes.Green;
        _btn.Foreground = isListening ? Brushes.White : Brushes.Black;
    }

    public void Stop() {
        isListening = false;
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (isListening)
        {
            // 停止监听
            isListening = false;
            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join();
            }
         
        }
        else
        {
            // 开始监听
            isListening = true;
            listenerThread = new Thread(ListenForDialogs);
            listenerThread.Start();
        }

        UpdateButtonState();
    }

    private void ListenForDialogs()
    {
        while (isListening)
        {
            EnumWindows(EnumWindowsCallback, IntPtr.Zero);
            Thread.Sleep(1000);
        }
    }

    private static PropertyCondition textCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);

    private bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        if (IsWindowVisible(hWnd))
        {
           StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);

            // 检查是否为对话框类名
            if (className.ToString() == "#32770")
            {

                StringBuilder windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);
                if (windowText.ToString().Contains("确认")) {
                    return true;
                }

                // 使用 UIAutomation 获取对话框内容
                AutomationElement dialogElement = AutomationElement.FromHandle(hWnd);
                    AutomationElementCollection textElements = dialogElement.FindAll(TreeScope.Children, textCondition);

                    foreach (AutomationElement textElement in textElements)
                    {
                        Logger.Debug($"对话框: {windowText.ToString()}  {textElement.Current.Name}");
                      
                    }
                    StockInput.PressEnter();
            }
        }
        return true;
    }

}
