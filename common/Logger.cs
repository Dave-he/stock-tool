using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Windows.Documents;

namespace stock_tool.common;
public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

public class Logger
{
    private static Logger _instance;
    private readonly RichTextBox _logTextBox;
    private readonly string _logFilePath;

    private readonly object _lockObject = new object();

    private Logger(RichTextBox logTextBox, string logFilePath)
    {
        EnsureLogDirectoryExists(logFilePath);
        _logTextBox = logTextBox;
        _logFilePath = logFilePath;
    }

    public static void Initialize(RichTextBox logTextBox, string logFilePath)
    {

        if (_instance == null)
        {
            _instance = new Logger(logTextBox, logFilePath);
        }
    }


    // 确保日志目录存在
    public static void EnsureLogDirectoryExists(string logFilePath)
    {
        string logDirectory = Path.GetDirectoryName(logFilePath);
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }
    public static void Debug(string message)
    {
        Log(message, LogLevel.Debug);
    }


    public static void Info(string message) {
        Log(message, LogLevel.Info);
    }

    public static void Error(string message)
    {
        Log(message, LogLevel.Error);
    }

    public static void Log(string message, LogLevel level)
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("Logger 未初始化，请先调用 Initialize 方法。");
        }
        _instance.InternalLog(message, level);
    }

    private void InternalLog(string message, LogLevel level)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        // 使用 lock 语句对方法加锁
        lock (_lockObject)
        {
            // 写入日志文件
            try
            {

                File.AppendAllLinesAsync(_logFilePath, [logEntry]);

            }
            catch (Exception ex)
            {
                App.WriteLog($"写入日志出错{message},{ex.Message}");
       
            }

            // 在 RichTextBox 中显示日志
            Application.Current.Dispatcher.Invoke(() =>
            {
                var doc = _logTextBox.Document;
                var paragraph = new Paragraph();

                var timeRun = new Run($"[{DateTime.Now:HH:mm:ss}] ")
                {
                    Foreground = Brushes.Gray
                };
                paragraph.Inlines.Add(timeRun);

                var levelRun = new Run($"[{level}] ")
                {
                    Foreground = GetLevelColor(level)
                };
                paragraph.Inlines.Add(levelRun);

                var messageRun = new Run(message)
                {
                    Foreground = GetLevelColor(level)
                };
                paragraph.Inlines.Add(messageRun);

                doc.Blocks.Add(paragraph);
                _logTextBox.ScrollToEnd();
            });
        }
    }

    private Brush GetLevelColor(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Debug:
                return Brushes.Blue;
            case LogLevel.Info:
                return Brushes.Black;
            case LogLevel.Warn:
                return Brushes.Orange;
            case LogLevel.Error:
                return Brushes.Red;
            default:
                return Brushes.Black;
        }
    }
}