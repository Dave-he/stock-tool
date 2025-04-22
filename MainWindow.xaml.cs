using stock_tool.common;
using stock_tool.service;
using stock_tool.utils;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

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


    public MainWindow()
    {
        InitializeComponent();
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
            submitService.Stop();
        }
        else {

            SubmitBtn.Content = "结束提交";
            SubmitBtn2.Content = "结束提交";
            submitService.SubmitClick(sender, e);
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