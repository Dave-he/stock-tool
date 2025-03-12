using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

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
        log.info($"开始执行....");
        // 获取配置项
        string targetWindowTitle = _configuration["TargetWindowTitle"];
        string targetElementTitle = _configuration["TargetElementTitle"];
  
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
        // 假设要点击窗口内的坐标 (100, 200)，可根据实际情况修改
        int x = (int)targetWindow.Current.BoundingRectangle.Right - int.Parse(_configuration["RefreshRight"]);
        int y = (int)targetWindow.Current.BoundingRectangle.Top + int.Parse(_configuration["RefreshTop"]);

        MouseSimulator.Click(x, y);
        log.info($"点击刷新图标 x:{x} y:{y}");
        Thread.Sleep(500);
        //PrintElementInfo(mainWindow);
        AutomationElement productId = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["ID"]), 5, 500);
        AutomationElement subject = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["Subject"]), 5, 500);
        AutomationElement submit = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["Submit"]), 5, 500);
        if (submit == null)
        {
            log.info($"刷新失败! 请检查配置项【Refresh】");
            return;
        }
        else
        {
            log.info($"刷新成功---ID: {productId.Current.Name}");
        }
        // 在目标窗口中查找 AutomationId 为 vtbl 的表格控件
        //AutomationElement productControl = AutomationSearchHelper.FindFirstElementById(targetWindow, productId.Current.Name);
        AutomationElement tableControl = AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["TableId"]);
        if (tableControl == null)
        {
            log.info($"未找到 ID 为 {productId.Current.Name} 的表格控件。");
       
        } else {
            max(mainWindow);
            Thread.Sleep(500);
            ScrollToControl scroll = new ScrollToControl();
            System.Drawing.Point? matchResult = scroll.FindImageInElement(targetWindow, "stock.png");
            if (matchResult.HasValue)
            {
                log.info($"匹配成功，坐标：{matchResult.Value}");
            }
            else
            {
                log.info("未找到匹配图像");
            }

          
            //)
            //try
            //{
            //    // 执行滑动屏幕操作
            //    ScrollToControl.ScrollToShowElement(targetWindow, tableControl);
            //}
            //catch (Exception ex)
            //{
            //    log.info($"滚动时发生错误: {ex.Message}");
            //}
        }
    }

    private void max(AutomationElement targetWindow) {
        // 检查窗口是否支持窗口模式切换
        if (targetWindow.TryGetCurrentPattern(WindowPattern.Pattern, out object windowPatternObject))
        {
            WindowPattern windowPattern = (WindowPattern)windowPatternObject;

            // 检查窗口是否可以最大化
            if (windowPattern.Current.CanMaximize)
            {
                // 执行最大化操作
                windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
               log.info("窗口已最大化。");
            }
            else
            {
                log.info("窗口不支持最大化操作。");
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

    // 同步重试方法
    public T Retry<T>(Func<T> func, int maxRetries, int retryDelay, Func<Exception, bool> shouldRetry = null)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount > maxRetries || (shouldRetry != null && !shouldRetry(ex)))
                {
                    throw;
                }

               log.info($"第 {retryCount} 次重试，原因: {ex.Message}，等待 {retryDelay} 毫秒后再次尝试...");
                Thread.Sleep(retryDelay);
            }
        }
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