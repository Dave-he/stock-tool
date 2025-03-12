using System;
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
    private string _logFilePath;

    public MainWindow()
    {
        InitializeComponent();

        // 加载配置文件
        LoadConfiguration();

        // 获取日志文件路径
        _logFilePath = _configuration["LogFilePath"];
        EnsureLogDirectoryExists();
    }

    private void LoadConfiguration()
    {
        _configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("config.json", optional: false, reloadOnChange: true)
           .Build();
    }

    // 确保日志目录存在
    private void EnsureLogDirectoryExists()
    {
        string logDirectory = Path.GetDirectoryName(_logFilePath);
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }

    // 根据窗口标题查找窗口
    private AutomationElement FindWindowByTitle(string windowTitle)
    {
        System.Windows.Automation.Condition condition = new PropertyCondition(AutomationElement.NameProperty, windowTitle);
        return AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
    }

    // 查找子窗口和窗口元素
    private void FindChildElements(AutomationElement parentElement)
    {
        System.Windows.Automation.Condition allCondition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
        AutomationElementCollection childElements = parentElement.FindAll(TreeScope.Children, allCondition);

        foreach (AutomationElement childElement in childElements)
        {
            string elementName = childElement.Current.Name;
            string elementClass = childElement.Current.ClassName;
            LogMessage($"找到子元素：名称={elementName}，类名={elementClass}");
        }
    }

    // 记录日志的方法
    private void LogMessage(string message)
    {
        // 获取当前时间
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // 组合时间戳和日志消息
        string logEntry = $"[{timestamp}] {message}";

        // 在 UI 线程中更新 TextBox 的内容
        this.Dispatcher.Invoke(() =>
        {
            // 将日志信息添加到 TextBox 中
            LogTextBox.Text += logEntry + Environment.NewLine;
            // 滚动到 TextBox 的底部，确保最新日志可见
            LogTextBox.ScrollToEnd();
        });

        // 将日志信息写入文件
        try
        {
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // 处理文件写入异常
            MessageBox.Show($"写入日志文件时出错: {ex.Message}");
        }
    }

    // 开始按钮点击事件处理方法
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        // 获取配置项
        string targetWindowTitle = _configuration["TargetWindowTitle"];

        LogMessage($"开始搜索目标窗口：{targetWindowTitle}");

        // 查找目标窗口
        AutomationElement targetWindow = FindWindowByTitle(targetWindowTitle);
        if (targetWindow != null)
        {
            LogMessage("找到目标窗口！");

            // 获取子窗口和窗口元素
            FindChildElements(targetWindow);
        }
        else
        {
            LogMessage("未找到目标窗口！");
        }
    }
}