using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
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
        string refresh = _configuration["Refresh"];
        string id = _configuration["ID"];
        string tableId = _configuration["TableId"];
        log.info($"开始执行....");

        // 查找目标窗口
        AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, targetWindowTitle));

        if (mainWindow == null)
        {
            log.info($"找不到【{targetWindowTitle}】，请运行程序");
            return;
        }
        AutomationSearchHelper.TryActivateWindow(mainWindow, log);

        // 查找右边pannel
        AutomationElement targetWindow = AutomationSearchHelper.FindFirstElement(mainWindow,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));
        if (targetWindow == null)
        {
            log.info($"找不到 【{targetElementTitle}】,请打开某商品并点击【一键】");
            return;
        }

         //PrintElementInfo(mainWindow);

        AutomationElement refreshElement = AutomationSearchHelper.FindFirstElementById(targetWindow, refresh);
        AutomationElement subjectId = AutomationSearchHelper.FindFirstElementById(targetWindow, id);

        if (refreshElement == null)
        {
            log.info($"刷新失败! 【{subjectId.Current.Name}】请检查配置项【Refresh】");
            return;
        }
        else if (click(refreshElement))
        {
            log.info($"刷新成功---ID: {subjectId.Current.Name}");
        }

        Thread.Sleep(1000);
        // 在目标窗口中查找 AutomationId 为 vtbl 的表格控件
        AutomationElement tableControl = AutomationSearchHelper.FindFirstElementById(targetWindow, tableId);
        if (tableControl == null)
        { 
            log.info($"未找到 ID 为 {tableId} 的表格控件。");
            return;
        }

        // 获取表格中的所有行
        AutomationElementCollection rows = tableControl.FindAll(
            TreeScope.Children,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem));

        // 遍历每一行
        foreach (AutomationElement row in rows)
        {
            // 获取行中的所有单元格
            AutomationElementCollection cells = row.FindAll(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

            // 遍历每个单元格
            foreach (AutomationElement cell in cells)
            {
                // 获取单元格的文本内容
                string cellText = cell.Current.Name;
                log.info(cellText);
            }
        }

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
            log.info($"点击失败: {ex.Message}");
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
            log.file("----------------------");
            log.file($"元素名称: {element.Current.Name} 类 {element.Current.ClassName} id {element.Current.AutomationId}");
            log.file($"元素控制类型: {element.Current.ControlType.ProgrammaticName} 是否启用: {element.Current.IsEnabled} 是否可见: {element.Current.IsOffscreen}");

        }
    }
}