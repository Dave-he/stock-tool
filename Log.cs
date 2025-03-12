using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace stock_tool
{

    public class Log
    {
        protected Window _window;
        protected TextBox _logBox;
        protected string _filePath;

        public Log(Window window, TextBox logBox, string filePath)
        {
            _window = window;
            _logBox = logBox;
            _filePath = filePath;
            EnsureLogDirectoryExists(filePath);
        }


        // 确保日志目录存在
        private void EnsureLogDirectoryExists(string logFilePath)
        {
            string logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        // 记录日志的方法
        public void info(string message)
        {
            // 获取当前时间
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            // 组合时间戳和日志消息
            string logEntry = $"[{timestamp}] {message}";

            // 在 UI 线程中更新 TextBox 的内容
            _window.Dispatcher.Invoke(() =>
            {
                // 将日志信息添加到 TextBox 中
                _logBox.Text += logEntry + Environment.NewLine;
                // 滚动到 TextBox 的底部，确保最新日志可见
                _logBox.ScrollToEnd();
            });

            file(message);
        }

        // 记录日志的方法
        public void file(string message)
        {
            // 获取当前时间
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            // 组合时间戳和日志消息
            string logEntry = $"[{timestamp}] {message}";

            // 将日志信息写入文件
            try
            {
                File.AppendAllTextAsync(_filePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // 处理文件写入异常
                MessageBox.Show($"写入日志文件时出错: {ex.Message}");
            }
        }
    }
}
