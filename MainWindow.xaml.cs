using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
namespace stock_tool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private IConfiguration _configuration;
    private Random random = new Random();
    private string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

    private static string CONFIG_FILE = "stock_config.json";

    // 定义全局键盘钩子的常量
    private const int WH_KEYBOARD_LL = 13;
    // 定义键盘按下消息的常量
    private const int WM_KEYDOWN = 0x0100;
    // 定义 ESC 键的虚拟键码
    private const int VK_ESCAPE = 0x1B;

    // 定义低级别键盘钩子处理委托
    private static LowLevelKeyboardProc _proc = HookCallback;
    // 钩子句柄
    private static IntPtr _hookID = IntPtr.Zero;

    // 用于控制按钮点击处理是否继续的标志
    private static volatile bool _isProcessing = true;

    private DateTime lastClickTime = DateTime.MinValue;
    private const int clickInterval = 2000; // 点击间隔时间，单位为毫秒

    private bool debug = false;

    public MainWindow()
    {
        InitializeComponent();
        SetRandomBackgroundImage();
        // 加载配置文件
        LoadConfiguration();
        this.SizeChanged += MainWindow_SizeChanged;

        // 获取日志文件路径
        string logFilePath = _configuration["LogFilePath"];

        Logger.Initialize(RichLogBox, $"{logFilePath}/{DateTime.Today:yyyy-MM-dd}.log");

        Logger.Info($"程序启动.... \n读取配置: {File.ReadAllText(CONFIG_FILE)}");
        // 设置全局键盘钩子
        _hookID = SetHook(_proc);
        // 窗口关闭时卸载钩子
        this.Closed += (sender, e) =>
        {
            UnhookWindowsHookEx(_hookID);
        };
    }

    // 设置全局键盘钩子
    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // 低级别键盘钩子处理委托类型
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // 钩子回调函数
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == VK_ESCAPE)
            {
                // 设置标志为 false，终止按钮处理
                _isProcessing = false;
                Application.Current.Shutdown();
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }


    // 导入 Windows API 函数：设置钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    // 导入 Windows API 函数：卸载钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    // 导入 Windows API 函数：调用下一个钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    // 导入 Windows API 函数：获取模块句柄
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.Space)
        {
            Application.Current.Shutdown();
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile(CONFIG_FILE, optional: false, reloadOnChange: true)
             .Build();
            return;
        }
        catch (Exception e)
        {

            Logger.Info($"读取配置文件失败: {e.Message}");
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

    private void SetRandomBackgroundImage()
    {
        int randomNumber = random.Next(1, 10);
        string imagePath = $"pack://application:,,,/{assemblyName};component/Images/bg{randomNumber}.png";

        try
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            BackgroundImageBrush.ImageSource = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载图片失败：{ex.Message}");
        }
    }

    private void ZipClick(object sender, RoutedEventArgs e)
    {
        // 获取用户输入的源文件夹路径
        string sourceFolder = _configuration["ImagePath"];
        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
       

        try
        {
            if (Directory.Exists(sourceFolder))
            {
                // 获取源文件夹的上一级目录
                DirectoryInfo parentDir = Directory.GetParent(sourceFolder);
                if (parentDir != null)
                {
                    string parentPath = parentDir.FullName;
                    string zipFileName = Path.Combine(parentPath, $"image_{timestamp}.zip");

                    Logger.Info($"开始压缩..{sourceFolder}");
                    ZipFile.CreateFromDirectory(sourceFolder, zipFileName);
                    Logger.Info($"压缩成功: {zipFileName}");
                }
                Directory.Delete(sourceFolder, true);
                Logger.Info($"删除成功: {sourceFolder}");
            }
            else
            {
                Logger.Error($"清理错误: 源文件夹 {sourceFolder} 不存在");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"清理失败: {ex.Message}");
        }
    }

    private int ConvertFromConfig(string config, bool isWidth) {
      
        double width = SystemParameters.PrimaryScreenWidth;   // 逻辑宽度（像素）
        double height = SystemParameters.PrimaryScreenHeight; // 逻辑高度（像素）
        return (int)(isWidth ? int.Parse(_configuration[config]) * 1280 / width
            : int.Parse(_configuration[config]) * 720 / height);
    }
    private void SaveClick(object sender, RoutedEventArgs e)
    {

    }

    private void WhiteClick(object sender, RoutedEventArgs e)
    {

    }

    private void StockClick(object sender, RoutedEventArgs e)
    {
        DateTime now = DateTime.Now;
        if ((now - lastClickTime).TotalMilliseconds < clickInterval)
        {
            Logger.Info($"请{clickInterval / 1000}秒后重试");
            return;
        }

        Logger.Info("开始处理所有....");
        // 获取配置项
        string targetWindowTitle = _configuration["TargetWindowTitle"];
        string targetElementTitle = _configuration["TargetElementTitle"];

        // 查找目标窗口
        AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, targetWindowTitle));

        if (mainWindow == null)
        {
            Logger.Info($"找不到【{targetWindowTitle}】，请运行程序");
            return;
        }
        AutomationSearchHelper.TryActivateWindow(mainWindow);
        max(mainWindow);
        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));

      
        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品");
            return;
        }
        int maxCount = GetMaxCount(mainWindow);
        ProcessSingle(mainWindow, targetWindow, maxCount);
       
    }

    private void ProcessSingle(AutomationElement mainWindow, AutomationElement targetWindow, int time = 0) {
        Logger.Info("开始执行....");
        
        // 假设要点击窗口内的坐标 (100, 200)，可根据实际情况修改

        //删除临时文件
        string resultPath =_configuration["ResultFilePath"];
        string resultFile = resultPath+ DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH-mm-ss")+".txt";
        File.Delete(_configuration["CompareFilePath"]);

        Rect rectangle = targetWindow.Current.BoundingRectangle;
        int x = (int)rectangle.Right - ConvertFromConfig("RefreshRight", true);
        int y = (int)targetWindow.Current.BoundingRectangle.Top + ConvertFromConfig("RefreshTop", false);

        MouseSimulator.Click(x, y);
        Logger.Info($"点击刷新图标 x:{x} y:{y}");
        Thread.Sleep(500);
        //PrintElementInfo(mainWindow);
        AutomationElement productId = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["ID"]), 10, 500);
        AutomationElement subject = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["Subject"]), 10, 500);
        //AutomationElement submit = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["Submit"]), 10, 500);
        if (subject == null)
        {
            Logger.Info($"刷新失败! 请检查配置项【Refresh】");
            return;
        }
        else
        {
            Logger.Info($"刷新成功---ID: {productId.Current.Name}");
        }

     
        Task.Run(() =>
        {
            string last_id = "";
            List<string> processed = new List<string>();
            int errorTime = 0;
            for (int i = 0; i <= time; i++)
            {
                if (!_isProcessing)
                {
                    // 如果标志为 false，终止处理
                    return;
                }
                try
                {


                    AutomationElement id = Retry(() => AutomationSearchHelper.FindFirstElementById(targetWindow, _configuration["ID"]), 20, 100);
                    if (id == null || !Regex.IsMatch(id.Current.Name, @"^-?\d+$")) { 
                        throw new Exception("ID错误");
                    }
          
                    int stock = (int)targetWindow.Current.BoundingRectangle.Right - ConvertFromConfig("StockRight", true);

                    MouseSimulator.Click(stock, y);
                    Logger.Info($"{id.Current.Name},第{i}个,点击库存图标 x:{x} y:{y}");
                    last_id = id.Current.Name;
                   
                    if (debug)
                    {
                        Thread.Sleep(200);
                        StockInput.PressY();
                        Thread.Sleep(100);
                        StockInput.PressEnter();
                        Thread.Sleep(int.Parse(_configuration["WaitMillSeconds"]));
                    }
                    else {
                        StockInput.PressN();
                        Thread.Sleep(100);

                    }


                        processed.Add(id.Current.Name);

                    //AutomationElement element = AutomationSearchHelper.FindFirstElementByName(mainWindow, "错误");
                    //if (element != null)
                    //{
                    //    AutomationElement yes = AutomationSearchHelper.FindFirstElementByName(mainWindow, "确定");
                    //    // 检查按钮是否支持 Invoke 模式（即可点击）
                    //    if (yes.TryGetCurrentPattern(InvokePattern.Pattern, out object invokePatternObject))
                    //    {
                    //        InvokePattern invokePattern = (InvokePattern)invokePatternObject;
                    //        // 点击按钮
                    //        invokePattern.Invoke();

                    //    }
                    //}
                    errorTime = 0;
                }
                catch (Exception ex)
                {
                    if (errorTime < 10)
                    {
                        Logger.Info($"发生错误, 尝试刷新x:{x} y:{y}: {ex.Message}");
                        errorTime++;
                        i--;
                    }
                    else
                    {
                        errorTime = 0;
                        Logger.Info("发生错误, 已经刷新了10次，跳过该商品");
                    }
                   
                    MouseSimulator.Click(x, y);
                    Thread.Sleep(100);
                    StockInput.PressEnter();
                    Thread.Sleep(500);
                }

               
            }

            IEnumerable<string> needProcess = File.ReadAllLines(_configuration["CompareFilePath"]).Distinct();

            List<string> notProcess = new List<string>();
            foreach (string line in needProcess) {

                if (processed.Contains(line)){
                    //File.AppendLines(resultFile, line);
                } else {
                    notProcess.Add(line);
                }
            }

          

            if (notProcess.Count > 0) {
                File.WriteAllLines(resultFile, notProcess);
                int success = time == 0 ? 1 : time - notProcess.Count;
                Logger.Info($"所有{time},成功:{success} 未处理:{notProcess.Count}, 请查看{resultFile}");
            } else {

                Logger.Info($"全部处理成功 {time}");
            }

                //int x = (int)targetWindow.Current.BoundingRectangle.Right - ConvertFromConfig("RefreshRight", true);
                int close_x = (int)targetWindow.Current.BoundingRectangle.Right - ConvertFromConfig("CloseDiff", true);
            //int y = (int)targetWindow.Current.BoundingRectangle.Top + ConvertFromConfig("RefreshTop", false);
            MouseSimulator.Click(close_x, y);
            Logger.Info($"点击关闭图标 x:{x} y:{y}");
        });
    
 
    }


    private int GetMaxCount(AutomationElement mainWindow) {

        AutomationElement total = AutomationSearchHelper.FindFirstElementById(mainWindow, "lblTotal");

        Match match = Regex.Match(total.Current.Name, @"\d+");
        if (match.Success)
        {
            int totalNum = int.Parse(match.Value);
            return totalNum + totalNum / 60;
        }
        return 0;
    }

    private void SaveTest(object sender, RoutedEventArgs e)
    {

    }

    private void WhiteTest(object sender, RoutedEventArgs e)
    {

    }

    private void PageTest(object sender, RoutedEventArgs e)
    {

    }
    private void StockTest(object sender, RoutedEventArgs e)
    {

        DateTime now = DateTime.Now;
        if ((now - lastClickTime).TotalMilliseconds < clickInterval)
        {
            Logger.Info($"请{clickInterval / 1000}秒后重试");
            return;
        }

        lastClickTime = now;

        // 获取配置项
        string targetWindowTitle = _configuration["TargetWindowTitle"];
        string targetElementTitle = _configuration["TargetElementTitle"];

        // 查找目标窗口
        AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, targetWindowTitle));

        if (mainWindow == null)
        {
            Logger.Info($"找不到【{targetWindowTitle}】，请运行程序");
            return;
        }

        AutomationSearchHelper.TryActivateWindow(mainWindow);
        max(mainWindow);
        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));
        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开某商品并点击【库存】");
            return;
        }
        ProcessSingle(mainWindow, targetWindow);
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
               Logger.Info("窗口已最大化。");
            }
            else
            {
                Logger.Info("窗口不支持最大化操作。");
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
            Logger.Info($"点击失败: {ex.Message}");
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
                T t = func();
                if (t == null) {
                    throw new Exception("null");
                }
                return t;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount > maxRetries || (shouldRetry != null && !shouldRetry(ex)))
                {
                    throw;
                }

               Logger.Info($"重试【{retryCount}】，原因: {ex.Message}，等待 {retryDelay} 毫秒...");
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
            Logger.Info("----------------------");
            Logger.Info($"元素名称: {element.Current.Name} 类 {element.Current.ClassName} id {element.Current.AutomationId}");
            Logger.Info($"元素控制类型: {element.Current.ControlType.ProgrammaticName} 是否启用: {element.Current.IsEnabled} 是否可见: {element.Current.IsOffscreen}");

        }
    }
}