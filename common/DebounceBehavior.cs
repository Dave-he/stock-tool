using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;

namespace stock_tool.common;

public static class DebounceBehavior
{
    public static readonly DependencyProperty DebounceTimeProperty =
            DependencyProperty.RegisterAttached("DebounceTime", typeof(int), typeof(DebounceBehavior), new PropertyMetadata(300, OnDebounceTimeChanged));

    public static int GetDebounceTime(DependencyObject obj)
    {
        return (int)obj.GetValue(DebounceTimeProperty);
    }

    public static void SetDebounceTime(DependencyObject obj, int value)
    {
        obj.SetValue(DebounceTimeProperty, value);
    }

    private static void OnDebounceTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button button)
        {
            button.Click -= ButtonOnClick;
            button.Click += ButtonOnClick;
        }
    }

    private static DispatcherTimer _timer;
    private static void ButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            int debounceTime = GetDebounceTime(button);
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(debounceTime)
            };
            _timer.Tick += (s, args) =>
            {
                _timer.Stop();
                _timer = null;
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            };
            _timer.Start();
        }
    }
}
