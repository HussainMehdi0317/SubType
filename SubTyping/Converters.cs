using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SubTyping;

public sealed class CharacterStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is CharacterStatus status
            ? status switch
            {
                CharacterStatus.Correct => new SolidColorBrush(Color.FromRgb(37, 150, 95)),
                CharacterStatus.Wrong => new SolidColorBrush(Color.FromRgb(205, 65, 75)),
                _ => Application.Current.Resources["PrimaryTextBrush"]
            }
            : Application.Current.Resources["PrimaryTextBrush"];
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolean && !boolean;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolean && !boolean ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
