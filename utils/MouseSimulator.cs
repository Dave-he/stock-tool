﻿
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;

namespace stock_tool.utils;

public static class MouseSimulator
{
    // Windows API 声明
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    // 鼠标事件标志
    // 鼠标事件标志
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const int WHEEL_DELTA = 300;


    public static void Click(Point point)
    {
        Click((int) point.X, (int)point.Y);
    }

    /// <summary>
    /// 模拟鼠标左键点击指定坐标
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        Thread.Sleep(10);
        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        Thread.Sleep(100);
    }

    public static void ClickRight(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        Thread.Sleep(100);
    }

    public static void MouseClick(bool left, int x, int y)
    {
        if(left) { 
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        } 
        else { 
          
            mouse_event(MOUSEEVENTF_RIGHTDOWN, x, y, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_RIGHTUP, x, y, 0, 0);
             
        }
        // 稍微延迟一下，确保操作生效
        Thread.Sleep(100);
    }

    static void RightClickAtPoint(int x, int y)
    {
        // 设置鼠标位置到指定点
        SetCursorPos(x, y);
        // 模拟鼠标右键按下
        mouse_event(MOUSEEVENTF_RIGHTDOWN, x, y, 0, 0);
        // 模拟鼠标右键释放
        mouse_event(MOUSEEVENTF_RIGHTUP, x, y, 0, 0);
        Thread.Sleep(100);
    }

    public static void Move(Point point)
    {
       Move((int)point.X, (int)point.Y);
    }



    public static void Move(int x, int y) {
        SetCursorPos(x, y);
        Thread.Sleep(100);
    }

    public static void ScrollWindow(bool isScrollingDown)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, isScrollingDown ? WHEEL_DELTA : -WHEEL_DELTA, 0);
        Thread.Sleep(100);
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