using System.IO;
using System.Windows;
using System.Windows.Automation;
using Microsoft.Extensions.Configuration;

namespace stock_tool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private IConfiguration _configuration;
    private Log log;

    private static string CONFIG_FILE = "config.json";

    public MainWindow()
    {
        InitializeComponent();

        // 加载配置文件
        LoadConfiguration();

        // 获取日志文件路径
        string logFilePath = _configuration["LogFilePath"];
        log = new Log(this, LogTextBox, logFilePath);
        log.info($"程序启动.... \n读取配置: {File.ReadAllText(CONFIG_FILE)}");
    }

    private void LoadConfiguration()
    {
        _configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile(CONFIG_FILE, optional: false, reloadOnChange: true)
           .Build();
    }

 
    // 开始按钮点击事件处理方法
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        // 获取配置项
        string targetWindowTitle = _configuration["TargetWindowTitle"];
        string targetElementTitle = _configuration["TargetElementTitle"];
        log.info($"开始执行....");

        // 获取桌面元素作为起始点
        AutomationElement desktop = AutomationElement.RootElement;
        AutomationElement mainWindow= AutomationSearchHelper.FindWindowByTitle(desktop, targetWindowTitle);
        if (mainWindow == null)
        {
            log.info($"找不到【{targetWindowTitle}】，请运行程序");
            return;
        }
      
        PrintElementInfo(AutomationSearchHelper.FindElements(mainWindow, 
            new PropertyCondition(AutomationElement.IsEnabledProperty, true)));

        AutomationElement targetWin = AutomationSearchHelper.FindFirstElement(mainWindow,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));
        if (targetWin == null)
        {
            log.info($"找不到 【{targetElementTitle}】,请打开某商品并点击【一键】");
            return;
        }


        PrintElementInfo(AutomationSearchHelper.FindElements(targetWin));

    }


    /// <summary>
    /// 打印元素的关键参数
    /// </summary>
    /// <param name="elements">元素列表</param>
    public void PrintElementInfo(List<AutomationElement> elements)
    {
        foreach (AutomationElement element in elements)
        {
            log.info("----------------------");
            log.info($"元素名称: {element.Current.Name}");
            log.info($"元素类名: {element.Current.ClassName}");
            log.info($"元素控制类型: {element.Current.ControlType.ProgrammaticName}");
            log.info($"元素自动化 ID: {element.Current.AutomationId}");
            log.info($"元素是否启用: {element.Current.IsEnabled}");
            log.info($"元素是否可见: {element.Current.IsOffscreen}");
        }
    }
}