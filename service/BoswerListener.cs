
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using stock_tool.utils;
using stock_tool.common;

namespace stock_tool.service;

class BoswerListener
{

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private bool isListening = false;
    private AutomationElement eBrowserElement;
    private StructureChangedEventHandler structureChangedHandler;
    private Button listenButton;

    public BoswerListener(Button btn)
    {
        listenButton = btn;
        listenButton.Click += ListenButton_Click;
        structureChangedHandler = new StructureChangedEventHandler(StructureChangedHandler);
    }


    private void ListenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isListening)
        {
            // 启动监听
            StartListening();
            isListening = true;
            listenButton.Content = "停止";
        }
        else
        {
            // 停止监听
            StopListening();
            isListening = false;
            listenButton.Content = "监听";
        }
    }

    private void StartListening()
    {
        // 注册根元素
        //AutomationElement rootElement = AutomationElement.RootElement;
        Logger.Info("开始处理所有....");
        // 获取配置项
        string targetWindowTitle = Config.Get("TargetWindowTitle");
        string targetElementTitle = Config.GetDefault("Target2ElementTitle", "我的产品");

        // 查找目标窗口
        AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, targetWindowTitle));

        if (mainWindow == null)
        {
            Logger.Info($"找不到【{targetWindowTitle}】，请运行程序");
            return;
        }

        // 查找右边pannel
        AutomationElement targetWindow = mainWindow.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, targetElementTitle));


        if (targetWindow == null)
        {
            Logger.Info($"找不到 【{targetElementTitle}】,请打开批量采集");
            return;
        }

        // 查找 AutomationId 为 "EBrowser" 的组件
        eBrowserElement = targetWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "EBrowser"));

        if (eBrowserElement != null)
        {
            StructureChangedHandler(eBrowserElement, null);
            // 监听 EBrowser 组件内的结构变化
            Automation.AddStructureChangedEventHandler(eBrowserElement, TreeScope.Children, structureChangedHandler);
        }
    }

    private void StopListening()
    {
        if (eBrowserElement != null)
        {
            // 移除监听
            Automation.RemoveStructureChangedEventHandler(eBrowserElement, structureChangedHandler);
        }
    }

    private void StructureChangedHandler(object sender, StructureChangedEventArgs e)
    {
        try
        {
            AutomationElement eBrowserElement = sender as AutomationElement;
            if (eBrowserElement == null)
            {
                return;
            }

            AutomationElement cefBrowserWindow = eBrowserElement.FindFirst(TreeScope.Descendants
                , new PropertyCondition(AutomationElement.ClassNameProperty, "CefBrowserWindow"));

            if (cefBrowserWindow == null) {
                return;
            }

            AutomationElement document = cefBrowserWindow.FindFirst(TreeScope.Descendants
                , new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, "文档"));

            if (document == null)
            {
                return;
            }

              AutomationElement baxia = document.FindFirst(TreeScope.Descendants
                , new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, "组"));

            if (baxia == null)
            {
                return;
            }

            AutomationElement nocaptcha = baxia.FindFirst(TreeScope.Descendants
                , new PropertyCondition(AutomationElement.AutomationIdProperty, "nocaptcha"));

            if (nocaptcha == null)
            {
                return;
            }

            AutomationElement nc_1_n1t = nocaptcha.FindFirst(TreeScope.Descendants
           , new PropertyCondition(AutomationElement.AutomationIdProperty, "nc_1_n1t"));

            if (nc_1_n1t == null)
            {
                return;
            }


            // 查找名为 "滑块" 的组件
            AutomationElement sliderElement = nc_1_n1t.FindFirst(TreeScope.Descendants
                , new PropertyCondition(AutomationElement.NameProperty, "滑块"));

            if (sliderElement == null)
            {
                return;
            }

            AutomationSearchHelper.TryActivateWindow(eBrowserElement);
            Logger.Info("发现滑块组件");
            // 获取滑块父组件
            AutomationElement parentElement = nc_1_n1t;
            // 获取滑块和父组件的矩形区域
            Rect sliderRect = (Rect)sliderElement.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            Rect parentRect = (Rect)parentElement.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);

            // 计算滑块中心位置
            int sliderCenterX = (int)(sliderRect.Left + sliderRect.Width / 2);
            int sliderCenterY = (int)(sliderRect.Top + sliderRect.Height / 2);

            // 计算父组件右侧边缘位置
            int parentRightX = (int)parentRect.Right - 3;


            MouseSimulator.MouseClick(true, sliderCenterX, sliderCenterY);
            Thread.Sleep(100);

            // 按下鼠标左键
            mouse_event(MOUSEEVENTF_LEFTDOWN, sliderCenterX, sliderCenterY, 0, 0);
            Thread.Sleep(100);

            // 拖动鼠标到父组件右侧边缘
            for (int x = sliderCenterX; x <= parentRightX; x++)
            {
                MouseSimulator.Move(x, sliderCenterY);
                Thread.Sleep(1);
            }

            // 释放鼠标左键
            mouse_event(MOUSEEVENTF_LEFTUP, parentRightX, sliderCenterY, 0, 0);
                
                
            
        }
        catch (Exception ex) {
            Logger.Info($"监听过程中出现错误{ex.Message}");
        }
    }

    internal void Stop()
    {
        StopListening();
    }
}
