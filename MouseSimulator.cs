using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace stock_tool;

public static class MouseSimulator
{
    // Windows API 声明
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

    // 鼠标事件标志
    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    /// <summary>
    /// 模拟鼠标左键点击指定坐标
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
    }

    /// <summary>
    /// 模拟鼠标左键点击元素中心
    /// </summary>
    /// <param name="element">目标 AutomationElement</param>
    public static void ClickElementCenter(AutomationElement element)
    {
        var rect = element.Current.BoundingRectangle;
        if (rect.IsEmpty) throw new InvalidOperationException("元素无有效坐标");

        // 计算元素中心点（考虑 DPI 缩放）
        var centerX = (int)(rect.Left + rect.Width / 2);
        var centerY = (int)(rect.Top + rect.Height / 2);

        Click(centerX, centerY);
    }
}