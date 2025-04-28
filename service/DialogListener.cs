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
using System.Text.RegularExpressions;

namespace stock_tool.service;

class DialogListener
{
    public event EventHandler StopEvent;
    // 导入 Windows API 函数
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);


    // 定义回调函数委托
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // 存储已检测到的对话框句柄
    private List<IntPtr> detectedDialogs = new List<IntPtr>();
    private Thread listenerThread;
    private bool isListening = false;

    private Button _btn;
    private int _count = 100; // 最大提交数量

    public int Count
    {
        get => _count;
        set
        {
            if (value > 0)
            {
                _count = value;
            }
        }
    }


    private static DialogListener? _instance;

    public static DialogListener Instance => _instance;

    internal static void Init(Button dialogBtn)
    {
        _instance ??= new DialogListener(dialogBtn);
    }

    public static bool needCheck = false;


    public DialogListener(Button button)
    {
        _btn = button;
        _btn.Click += StartStopButton_Click;
        UpdateButtonState();
        mode = Config.Enable("DialogDebugMode");
    }


    private void UpdateButtonState()
    {
        _btn.Content = isListening ? "停止" : "监听";
        _btn.Background = isListening ? Brushes.Red : Brushes.Green;
        _btn.Foreground = isListening ? Brushes.White : Brushes.Black;
    }

    public void Stop() {
        ids.Clear();
        StopEvent?.Invoke(this, EventArgs.Empty);
        isListening = false;
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join();
        }
    }

    public void Start(int count=100)
    {
        Count = count;
        ids.Clear();
        isListening = true;
        listenerThread = new Thread(ListenForDialogs);
        listenerThread.Start();
    }


    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (isListening)
        {
            Stop();
        }
        else
        {
            Start();
        }
        UpdateButtonState();
    }

    private volatile int errTime = 0;

    private volatile HashSet<string> ids = new HashSet<string>();

    private void ListenForDialogs()
    {
        while (isListening)
        {
            try
            {
                // 获取配置项
                string targetWindowTitle = Config.Get("TargetWindowTitle");
                IntPtr mainWindowHandle = WindowApi.FindAndActivateWindow(targetWindowTitle);
                if (mainWindowHandle == IntPtr.Zero)
                {
                    Logger.Info($"找不到窗口: {targetWindowTitle}");
                    errTime++;
                }
                else {
                    EnumWindows(EnumWindowsCallback, mainWindowHandle);
                    errTime = 0;
                }
                Thread.Sleep(1000);
                if (errTime > 10) {
                    isListening = false;
                    return;
                }
            }
            catch (Exception e) { 
                Logger.Error($"监听对话框时出错: {e.Message}");
            }
        }
    }

    private bool mode = false;

    private static PropertyCondition textCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);

    private bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        if (WindowApi.IsWindowVisible(hWnd))
        {
            StringBuilder className = new StringBuilder(256);
            WindowApi.GetClassName(hWnd, className, className.Capacity);

            // 检查是否为对话框类名
            if (className.ToString() == "#32770")
            {

                StringBuilder windowText = new StringBuilder(256);
                WindowApi.GetWindowText(hWnd, windowText, windowText.Capacity);
                if (!windowText.ToString().Contains("确认") &&
                    !windowText.ToString().Contains("信息") &&
                    !windowText.ToString().Contains("错误"))
                {
                    return true;
                }

                // 使用 UIAutomation 获取对话框内容
                AutomationElement dialogElement = AutomationElement.FromHandle(hWnd);
                AutomationElementCollection textElements = dialogElement.FindAll(TreeScope.Children, textCondition);
                string id = null;
                foreach (AutomationElement textElement in textElements)
                {
                    string text = textElement.Current.Name;
                    Logger.Debug($"对话框: {text}");
                    if (text.StartsWith("提交完成;")) {
                        id = text.Split("[")[1].Split("]")[0].Trim();
                    }

                    if (text.EndsWith("读取下一个")) {
                        needCheck = true;
                    }
                    if (text.StartsWith("结束-已完成"))
                    {
                        isListening = false;
                        Stop();
                        return true;
                    }
                }
                WindowApi.SetForegroundWindow(hWnd);
                if (mode)
                {
                    StockInput.PressN();
                    WindowApi.ClickBtn(hWnd, "关闭");
                }
                else {
                    WindowApi.ClickBtn(hWnd, "确认");
                    WindowApi.ClickBtn(hWnd, "是");
                    StockInput.PressEnter();
                }

                if (id != null && Regex.IsMatch(id, @"^-?\d+$")) {
                    ids.Add(id);
                }
                if (ids.Count >= Count)
                {
                    Logger.Info($"全部提交成功结束: {ids.Count}");
                    isListening = false;
                    Stop();
                    return true;
                }
            }
        }
        return true;
    }

}
