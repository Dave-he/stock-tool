﻿using stock_tool.common;
using stock_tool.service;
using stock_tool.utils;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;

namespace stock_tool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private static string CONFIG_FILE = "stock_config.json";

    private KeyboardHookHelper _keyboardHook;

    // 用于控制按钮点击处理是否继续的标志
    private static volatile bool _isProcessing = true;
    private SubmitService submitService;
    private BoswerListener boswerListener;
    private MyUploadService uploadService;
    private const string RegistryFilePath = "modify_registry.reg";
    private const string IsFirstRunKey = "IsFirstRun";
    private readonly Microsoft.Win32.RegistryKey _appKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\YourAppName");


    public MainWindow()
    {
        InitializeComponent();
        CheckAndExecuteRegistry();
        BackgroundImageBrush.ImageSource = BackGround.GetRandomBackgroundImage();
        // 加载配置文件
        Config.LoadConfiguration(CONFIG_FILE);
        this.SizeChanged += MainWindow_SizeChanged;
        Logger.Initialize(RichLogBox, $"{Config.Get("LogFilePath")}/{DateTime.Today:yyyy-MM-dd}.log");
        Logger.Info($"程序启动.... \n读取配置: {File.ReadAllText(CONFIG_FILE)}");

        KeyboardHookHelper.SetHook();
        StockConfigService.Init(stockTextBox);
        ClearService.Init(ClearBtn);
        WhiteService.Init(WhiteBtn);
        DialogListener.Init(DialogBtn);
        DialogBtn.Visibility = Visibility.Hidden;
        submitService = new SubmitService();
        submitService.StopEvent += SubmitStop;
        uploadService = new MyUploadService();
        uploadService.StopEvent += SubmitStop;
        DialogListener.Instance.StopEvent += SubmitStop; 
        if (!Config.Enable("SaveEnable")) { 
            SaveGrid.Visibility = Visibility.Hidden;
        }

        if (!Config.Enable("StockEnable"))
        {
            StockGrid.Visibility = Visibility.Hidden;
        }
        KillBtn.Click += Kill_Click;
        boswerListener = new BoswerListener(BoswerBtn);
        SaveBtn.IsEnabled = false;
        if (Config.Enable("Save2") || Config.Enable("Save3")) {
            SaveBtn.IsEnabled = true;
            SaveBtn.Content = "预处理";
        }
    }

    private void CheckAndExecuteRegistry()
    {
        if (_appKey != null && _appKey.GetValue(IsFirstRunKey) == null)
        {
            if (File.Exists(RegistryFilePath))
            {
                try
                {
                    Process regProcess = new Process();
                    regProcess.StartInfo.FileName = "regedit.exe";
                    regProcess.StartInfo.Arguments = $"/s {RegistryFilePath}";
                    regProcess.StartInfo.UseShellExecute = false;
                    regProcess.StartInfo.CreateNoWindow = true;
                    regProcess.Start();
                    regProcess.WaitForExit();

                    _appKey.SetValue(IsFirstRunKey, false);
                    ShowRestartPrompt();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"执行注册表文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"未找到注册表文件: {RegistryFilePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ShowRestartPrompt()
    {
        int remainingSeconds = 10;
        string message = $"已执行注册表文件，计算机将在 {remainingSeconds} 秒后自动重启。";
        MessageBoxResult result = MessageBox.Show(message, "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);

        if (result == MessageBoxResult.OK)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) =>
            {
                remainingSeconds--;
                if (remainingSeconds > 0)
                {
                    message = $"已执行注册表文件，计算机将在 {remainingSeconds} 秒后自动重启。";
                    MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    timer.Stop();
                    RestartMachine();
                }
            };
            timer.Start();
        }
    }

    private void RestartMachine()
    {
        try
        {
            Process.Start("shutdown", "/r /t 0");
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"重启计算机时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Kill_Click(object sender, RoutedEventArgs e)
    {
        FileUtil.CloseProcessByName("ZYing");
    }

    bool isSubmit = false;

    private void SubmitStop(object? sender, EventArgs e)
    {
        SubmitBtn.Content = "图片提交";
        SubmitBtn2.Content = "库存提交";
    }



    private void SubmitBtn_Click(object sender, RoutedEventArgs e)
    {
        SubmitBtn.IsEnabled = false;
        SubmitBtn2.IsEnabled = false;
        e.Handled = true;
        if (isSubmit)
        {
            SubmitBtn.Content = "图片提交";
            SubmitBtn2.Content = "库存提交";
            if ((sender == SubmitBtn) && Config.Enable("Submit3"))
            {
                uploadService.Stop();
            }
            else if (sender == SaveBtn && Config.Enable("Save2"))
            {
                uploadService.Stop();
            }
            else { 
                submitService.Stop();
            
            }
               
        }
        else {

          
            if (sender == SubmitBtn && Config.Enable("Submit3"))
            {
                uploadService.UploadClick();
            } else if (sender == SaveBtn && Config.Enable("Save2")) {
                uploadService.UploadClick("SaveBtn");
            }
            else
            {
                if (sender == SubmitBtn || sender == SubmitBtn2) {
                    SubmitBtn.Content = "结束提交";
                    SubmitBtn2.Content = "结束提交";
                }
                submitService.SubmitClick(sender, e);
            }
        }
        isSubmit = !isSubmit;
        try
        {
            // 模拟一个耗时操作，例如网络请求
             Task.Delay(1000).Wait();
        }
        finally
        {
            SubmitBtn.IsEnabled = true;
            SubmitBtn2.IsEnabled = true;
        }
    }

    internal static bool IsProcess() { return _isProcessing; }

    protected override void OnClosed(System.EventArgs e)
    {
        _isProcessing = false;
        DialogListener.Instance?.Stop();
        DialogService.Instance?.stopListening();
        base.OnClosed(e);
        // 卸载全局键盘钩子
        KeyboardHookHelper.UnHook();
        if (boswerListener != null) { 
            boswerListener.Stop();
        }
    }


    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 当前窗口的实际可用宽度（排除边框）
        double availableWidth = this.ActualWidth - SystemParameters.WindowResizeBorderThickness.Left - SystemParameters.WindowResizeBorderThickness.Right;

        // 计算目标高度
        double targetHeight = availableWidth / 1;

        // 调整窗口高度（保持宽高比）
        this.Height = targetHeight + SystemParameters.WindowResizeBorderThickness.Top + SystemParameters.WindowResizeBorderThickness.Bottom;
    }


    public bool click(AutomationElement element) {
        try {
             //模拟鼠标点击（UI 自动化点击失败时）
            if (!AutomationSearchHelper.TryClickElement(element)) {
                MouseSimulator.ClickElementCenter(element);
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.Info($"点击失败: {ex.Message}");
        }
        return false;
    }


    /// <summary>
    /// 打印元素的关键参数
    /// </summary>
    /// <param name="elements">元素列表</param>
    public void PrintElementInfo(AutomationElement parent)
    {
        List<AutomationElement> elements = Task.Run(async () => await AutomationSearchHelper.FindAllElementsAsync(parent)).Result;
        foreach (AutomationElement element in elements)
        {
            AutomationSearchHelper.FindTableInPanel(element);
            Logger.Info("----------------------");
            Logger.Info($"元素名称: {element.Current.Name} 类 {element.Current.ClassName} id {element.Current.AutomationId}");
            Logger.Info($"元素控制类型: {element.Current.ControlType.ProgrammaticName} 是否启用: {element.Current.IsEnabled} 是否可见: {element.Current.IsOffscreen}");

        }
    }

    private void DialogBtn_Click(object sender, RoutedEventArgs e)
    {

    }
}