
using System;
using System.Drawing;
using System.Runtime.InteropServices;


namespace stock_tool;

public class StockInput
{

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    // 键盘事件标志
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void Input(int x, int y, string numberStr, int y_offset, int x_offset)
    {

        MouseSimulator.Click(x, y + y_offset);
   
        foreach (char c in numberStr)
        {
            byte keyCode = (byte)CharToVirtualKey(c);
            PressKey(keyCode);
        }

        // 按下回车键
        PressKey(0x0D);

        MouseSimulator.ClickRight(x, y + y_offset);

        MouseSimulator.Click(x + x_offset, y + y_offset);
    }

    public static void PressY() {
        PressKey(CharToVirtualKey('y'));
    }

    public static void PressEnter() {
        PressKey(0x0D);
    }

    // 模拟按键
     static void PressKey(byte keyCode)
    {
        keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, 0);
        Thread.Sleep(100);
        keybd_event(keyCode, 0, KEYEVENTF_KEYUP, 0);
        Thread.Sleep(100);
    }

    // 将字符转换为虚拟键码
    // 将字符转换为虚拟键码
     static byte CharToVirtualKey(char c)
    {
        switch (c)
        {
            case '0': return 0x30;
            case '1': return 0x31;
            case '2': return 0x32;
            case '3': return 0x33;
            case '4': return 0x34;
            case '5': return 0x35;
            case '6': return 0x36;
            case '7': return 0x37;
            case '8': return 0x38;
            case '9': return 0x39;
            case 'y': return 0x59;
            default: return 0;
        }
    }
}