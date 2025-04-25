using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BookSteward
{
    /// <summary>
    /// 将布尔值转换为可见性的转换器，支持反转结果
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = false;
            if (value is bool)
            {
                bValue = (bool)value;
            }
            else if (value is bool?)
            {
                bool? tmp = (bool?)value;
                bValue = tmp.GetValueOrDefault();
            }
            
            // 检查是否需要反转结果
            if (parameter != null)
            {
                if (parameter.ToString().ToLower() == "invert" || parameter.ToString().ToLower() == "true")
                {
                    bValue = !bValue;
                }
            }
            
            return bValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            bool result = visibility == Visibility.Visible;
            
            // 检查是否需要反转结果
            if (parameter != null)
            {
                if (parameter.ToString().ToLower() == "invert" || parameter.ToString().ToLower() == "true")
                {
                    result = !result;
                }
            }
            
            return result;
        }
    }
}