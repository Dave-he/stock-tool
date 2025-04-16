using System.Runtime.InteropServices;
using System.Text;

namespace stock_tool.utils;

class WindowApi
{

    // Windows API 函数声明
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // 导入 Windows API 函数：卸载钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    // 导入 Windows API 函数：调用下一个钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    // 导入 Windows API 函数：获取模块句柄
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern nint GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint FindWindowEx(nint hwndParent, nint hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumChildWindows(nint hwndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    // 枚举窗口的回调函数委托
    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    // 目标控件句柄
    private static nint targetControlHandle = nint.Zero;

    [DllImport("user32.dll")]
    public static extern nint SendMessage(nint hWnd, uint Msg, nint wParam, nint lParam);


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

  
    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);


    // 鼠标点击消息常量
    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP = 0x0202;


    public static IntPtr FindAndActivateWindow(string title) {

        // 查找目标窗口
        IntPtr mainWindowHandle = FindWindow(null, title);
        if (mainWindowHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        SetForegroundWindow(mainWindowHandle);
        return mainWindowHandle;
    }


    public static nint GetControl(string windowTitle, string controlWindowName) {
        // 查找目标窗体
        nint hWindow = FindWindow(null, windowTitle);
        if (hWindow == nint.Zero)
        {
            //Console.WriteLine("未找到指定标题的窗体。");
            return nint.Zero;
        }
        // 枚举子控件
        EnumChildWindows(hWindow, (hWnd, lParam) =>
        {
            StringBuilder windowText = new StringBuilder(256);
            GetWindowText(hWnd, windowText, windowText.Capacity);

            if (windowText.ToString() == controlWindowName)
            {
                targetControlHandle = hWnd;
                return false; // 找到目标控件，停止枚举
            }
            return true; // 继续枚举
        }, nint.Zero);

        return targetControlHandle;
    }

    public static void SendClick(nint hControl, int x) {

        // 点击位置的坐标（相对于控件左上角）
        int clickX = x;
        int clickY = 1;

        // 计算 lParam 参数，包含点击位置的坐标
        int lParam = clickY << 16 | clickX;

        // 模拟鼠标左键按下消息
        SendMessage(hControl, WM_LBUTTONDOWN, 1, lParam);
        // 稍微延迟一下，确保操作生效
        Thread.Sleep(10);
        // 模拟鼠标左键释放消息
        SendMessage(hControl, WM_LBUTTONUP, 0, lParam);
        // 稍微延迟一下，确保操作生效
        Thread.Sleep(100);
    }
    private const uint WM_CLICK = 0x00F5;



    private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        if (IsWindowVisible(hWnd))
        {
            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);

            // 检查是否为对话框类名
            if (className.ToString() == "#32770")
            {
                // 激活对话框
                SetForegroundWindow(hWnd);

                ClickBtn(hWnd, "提交");
                ClickBtn(hWnd, "关闭");
                
            }
        }
        return true;
    }

    public static void ClickBtn(IntPtr hWnd, string name) {
        // 查找“提交”按钮，假设按钮文本为“提交”
        IntPtr submitButton = FindWindowEx(hWnd, IntPtr.Zero, "Button", name);
        if (submitButton != IntPtr.Zero)
        {
            // 模拟点击“提交”按钮
            SendMessage(submitButton, WM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

    }

}
