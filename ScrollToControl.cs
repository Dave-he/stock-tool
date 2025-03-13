
using System.Windows.Automation;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Windows;
using System.Drawing;

namespace stock_tool
{
    public class ScrollToControl
    {

        // 匹配参数
        private const double MatchThreshold = 0.8;
        private const int MaxScrollAttempts = 40;
        private const int ScrollWaitMs = 500;

        public void ScrollWindowUntilTargetVisible(AutomationElement window, AutomationElement target)
        {
            try
            {

                // 获取窗口和目标元素的矩形区域
                Rect windowRect = window.Current.BoundingRectangle;
                Rect targetRect = target.Current.BoundingRectangle;

                int attempts = 0;
                // 检查目标元素是否已经在窗口内
                while (!IsElementVisibleInWindow(windowRect, targetRect) && attempts < MaxScrollAttempts)
                {
                    // 执行滚动操作
                    ScrollElement(window, false);
                    // 更新窗口和目标元素的矩形区域
                    windowRect = window.Current.BoundingRectangle;
                    targetRect = target.Current.BoundingRectangle;
                    
                }

                if (!IsElementVisibleInWindow(windowRect, targetRect))
                {
                    Console.WriteLine("目标元素已在窗口内可见。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"滚动过程中出现错误: {ex.Message}");
            }
        }

        static bool IsElementVisibleInWindow(Rect windowRect, Rect elementRect)
        {
            return windowRect.IntersectsWith(elementRect);
        }

        public System.Windows.Point? FindImageInElement(AutomationElement element, string filePath)
        {
            int attempts = 0;
            bool isScrollingDown = true;
            Image<Bgr, byte> template = new Image<Bgr, byte>(filePath);

            while (attempts < MaxScrollAttempts)
            {
                // 获取元素当前屏幕区域
                Rectangle elementRect = ConvertWpfRectToDrawingRectangle(element.Current.BoundingRectangle);
                if (elementRect.IsEmpty) return null;

                // 截取元素区域屏幕
                Image<Bgr, byte> screenShot = CaptureScreen(elementRect);
                if (screenShot == null) return null;

                // 执行模板匹配
                System.Windows.Point? matchPoint = PerformTemplateMatch(screenShot, template);
                if (matchPoint.HasValue)
                {
                    // 释放资源
                    screenShot.Dispose();
                 
                    template.Dispose();
                    return new System.Windows.Point(
                        (int)(matchPoint.Value.X + elementRect.X),
                        (int)(matchPoint.Value.Y + elementRect.Y)
                    );
                }

                screenShot.Dispose();
                attempts++;

                // 执行滚动操作
                ScrollElement(element, isScrollingDown);
                Thread.Sleep(ScrollWaitMs);

                // 检查位置是否变化
                //Rectangle newRect = ConvertWpfRectToDrawingRectangle(element.Current.BoundingRectangle);
                if (attempts > MaxScrollAttempts/2)
                {
                    isScrollingDown = !isScrollingDown;
                    Console.WriteLine("Scroll direction reversed");
                }
            }
            template.Dispose();
            return null;
        }

        static Image<Bgr, byte> CaptureScreen(Rectangle rect)
        {
            try
            {
                // 创建一个 Bitmap 对象用于存储屏幕截图
                Bitmap screenBitmap = new Bitmap( rect.Width, rect.Height);

                // 创建一个 Graphics 对象，用于从屏幕复制图像到 Bitmap 中
                using (Graphics g = Graphics.FromImage(screenBitmap))
                {
                    // 从指定的屏幕区域复制图像到 Bitmap 中
                    g.CopyFromScreen(rect.Top, rect.Left, 0, 0, rect.Size);
                }
                // 将 Bitmap 转换为三维字节数组
                byte[,,] byteArray = BitmapToByteArray(screenBitmap);
                // 2. 转换为 EmguCV 图像
                return new Image<Bgr, byte>(byteArray);
               
            }
            catch (Exception ex)
            {
                // 捕获并输出可能出现的异常信息
                Console.WriteLine($"截图过程中出现错误: {ex.Message}");
         
            }
            return null;
        }

        static byte[,,] BitmapToByteArray(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            byte[,,] byteArray = new byte[height, width, 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    byteArray[y, x, 0] = pixelColor.B; // 蓝色通道
                    byteArray[y, x, 1] = pixelColor.G; // 绿色通道
                    byteArray[y, x, 2] = pixelColor.R; // 红色通道
                }
            }

            return byteArray;
        }

        private System.Windows.Point? PerformTemplateMatch(Image<Bgr, byte> source, Image<Bgr, byte> template)
        {
            Mat result = new Mat();
            CvInvoke.MatchTemplate(source, template, result, TemplateMatchingType.CcoeffNormed);

            double minVal= 0.0, maxVal = 0.0;
            System.Drawing.Point minLoc = new System.Drawing.Point(), maxLoc = new System.Drawing.Point();
            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            if (maxVal >= MatchThreshold)
            {
                int centerX = (int)( maxLoc.X + template.Width / 2);
                int centerY = (int)(maxLoc.Y + template.Height / 2);
                return new System.Windows.Point(centerX, centerY);
            }

            return null;
        }


        private void ScrollElement(AutomationElement element, bool isScrollingDown)
        {
            // 获取元素中心点
            Rect rect = element.Current.BoundingRectangle;
            int centerX = (int)(rect.X + rect.Width / 2);
            int centerY = (int)(rect.Y + rect.Height / 2);

            // 设置鼠标位置并滚动
            // 将鼠标移动到面板中心点
            MouseSimulator.Move(centerX, centerY);
            // 模拟鼠标滚轮滚动
            MouseSimulator.ScrollWindow(isScrollingDown);
        }


        // 执行面板滚动操作
        private static async Task ScrollPanelAsync(System.Windows.Point panelCenter, bool isScrollingDown)
        {
            await Task.Run(() =>
            {
                // 将鼠标移动到面板中心点
                MouseSimulator.Move((int) panelCenter.X, (int)panelCenter.Y);
                // 模拟鼠标滚轮滚动
                MouseSimulator.ScrollWindow(isScrollingDown);
            });
        }

        static System.Drawing.Rectangle ConvertWpfRectToDrawingRectangle(System.Windows.Rect wpfRect)
        {
            // 转换为整数类型，因为 System.Drawing.Rectangle 的参数是整数
            int x = (int)Math.Round(wpfRect.X);
            int y = (int)Math.Round(wpfRect.Y);
            int width = (int)Math.Round(wpfRect.Width);
            int height = (int)Math.Round(wpfRect.Height);

            // 创建并返回 System.Drawing.Rectangle 对象
            return new System.Drawing.Rectangle(x, y, width, height);
        }

        static System.Windows.Point ConvertToWpfPoint(System.Drawing.Point drawingPoint)
        {
            // 创建一个新的 System.Windows.Point 实例，并将 System.Drawing.Point 的 X 和 Y 值赋给它
            return new System.Windows.Point(drawingPoint.X, drawingPoint.Y);
        }

    }
}
