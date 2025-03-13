using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Windows.Forms;

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

    private int ConvertFromConfig(string config, bool isWidth) {
      
        double width = SystemParameters.PrimaryScreenWidth;   // 逻辑宽度（像素）
        double height = SystemParameters.PrimaryScreenHeight; // 逻辑高度（像素）
        return (int)(isWidth ? int.Parse(_configuration[config]) * 1920 / width
            : int.Parse(_configuration[config]) * 1080 / height);
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


        int x = (int)targetWindow.Current.BoundingRectangle.Right - ConvertFromConfig("RefreshRight", true);
        int y = (int)targetWindow.Current.BoundingRectangle.Top + ConvertFromConfig("RefreshTop", false); 

        MouseSimulator.Click(x, y);
        log.info($"点击刷新图标 x:{x} y:{y}");
        Thread.Sleep(500);
        //PrintElementInfo(mainWindow);
        AutomationElement productId = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["ID"]), 5, 500);
        AutomationElement subject = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["Subject"]), 5, 500);
        AutomationElement submit = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["Submit"]), 5, 500);
        if (submit == null || subject == null)
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
            Point? matchResult = scroll.FindImageInElement(targetWindow, "stock.png");
            if (matchResult.HasValue)
            {
                Point p =  matchResult.Value;
                log.info($"匹配成功，坐标：{p}");
              
                PutStock((int)p.X, (int)p.Y);
            }
            else
            {
                log.info("未找到匹配图像");
            }
        }

        //MouseSimulator.ClickElementCenter(submit);
        log.info($"点击提交图标 {submit.Current.BoundingRectangle}");
        Thread.Sleep(500);
        StockInput.PressY();
        Thread.Sleep(500);
        StockInput.PressY();
        Thread.Sleep(500);

        int close_x = (int)subject.Current.BoundingRectangle.Right + ConvertFromConfig("CloseDiff", false);
        MouseSimulator.Click(close_x, y);
        log.info($"点击刷新图标 x:{x} y:{y}");
    }

    private void PutStock(int x, int y) {
        double width = SystemParameters.PrimaryScreenWidth;   // 逻辑宽度（像素）
        double height = SystemParameters.PrimaryScreenHeight; // 逻辑高度（像素）
        int x_offset = ConvertFromConfig("XOffset", true); 
        int y_offset = ConvertFromConfig("YOffset", false);

        // 生成20 - 100之间的随机数
        string num = _configuration["Num"];
             int randomNumber = 100;
        if (num.Contains("-"))
        {
           string[] split =  num.Split("-");
            int min = int.Parse(split[0]);
            int max = int.Parse(split[1]);
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }
            randomNumber = new Random().Next(min, max);
            num = randomNumber.ToString();
        }
        StockInput.Input(x, y, num, y_offset, x_offset);
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