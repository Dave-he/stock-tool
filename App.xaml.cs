using stock_tool.common;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace stock_tool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    public App()
    {
        // 注册未处理异常事件
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        // 注册应用程序退出事件
        this.Exit += App_Exit;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        // 记录未处理异常信息到日志文件
        string logMessage = $"未处理的异常: {e.Exception.Message}\n堆栈跟踪: {e.Exception.StackTrace}";
        WriteLog(logMessage);

        // 标记异常已处理，避免应用程序崩溃
        e.Handled = true;
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        // 记录应用程序退出信息到日志文件
        string logMessage = "应用程序已退出:" + e.ToString();
        WriteLog(logMessage);
        try
        {
            // 获取当前进程的名称
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            // 获取所有同名的进程
            Process[] processes = Process.GetProcessesByName(currentProcessName);

            foreach (Process process in processes)
            {
                // 避免结束当前正在执行此代码的进程
                if (process.Id != Process.GetCurrentProcess().Id)
                {
                    // 尝试强制结束进程
                    process.Kill();
                    process.WaitForExit();
                }
            }

            // 最后结束当前进程
            Process.GetCurrentProcess().Kill();
        }
        catch (Exception ex)
        {
           // MessageBox.Show($"结束进程时出错: {ex.Message}");
        }

    }

    public static void WriteLog(string message)
    {
        try
        {
            string logFilePath = "log/app.error.log";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }
        catch (Exception ex)
        {
            // 处理写入日志时的异常
            Console.WriteLine($"写入日志时发生异常: {ex.Message}");
        }
    }
}

