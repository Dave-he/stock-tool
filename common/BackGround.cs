using System.Reflection;
using System.Windows.Media.Imaging;

namespace stock_tool.common;

class BackGround
{

    public static BitmapImage GetRandomBackgroundImage()
    {
        int randomNumber = new Random().Next(1, 10);
        string imagePath = $"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Images/bg{randomNumber}.png";

        try
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
        catch (Exception ex)
        {
            Logger.Error($"加载图片失败：{ex.Message}");
        }
        return new BitmapImage();
    }
}
