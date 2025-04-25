using System.Globalization;

namespace BookSteward
{
    /// <summary>
    /// 用于判断是否只选中了单个项目的转换器
    /// </summary>
    public class SingleSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // 只有当选中一个项目时返回true
                return count == 1;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 自定义布尔值到可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter != null && parameter.ToString() == "Invert";
            bool result = value is bool boolValue && boolValue;
            return invert ? (result ? Visibility.Collapsed : Visibility.Visible) : (result ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter != null && parameter.ToString() == "Invert";
            bool result = value is Visibility visibility && visibility == Visibility.Visible;
            return invert ? !result : result;
        }
    }
}