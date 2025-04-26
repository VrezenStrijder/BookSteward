using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BookSteward
{
    /// <summary>
    /// 多值转换器，用于将多个值转换为Visibility
    /// </summary>
    public class MultiValueToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 默认值为Visible
            if (values == null || values.Length == 0)
                return Visibility.Visible;

            // 检查所有值是否满足条件
            bool allTrue = true;
            foreach (var value in values)
            {
                if (value is bool boolValue)
                {
                    if (!boolValue)
                    {
                        allTrue = false;
                        break;
                    }
                }
                else if (value is string strValue)
                {
                    if (string.IsNullOrEmpty(strValue))
                    {
                        allTrue = false;
                        break;
                    }
                }
                else if (value == null || value == DependencyProperty.UnsetValue)
                {
                    allTrue = false;
                    break;
                }
            }

            // 检查是否需要反转结果
            bool invert = false;
            if (parameter != null && (parameter.ToString().ToLower() == "invert" || parameter.ToString().ToLower() == "true"))
            {
                invert = true;
            }

            // 根据结果和反转参数返回可见性
            if (invert)
            {
                return allTrue ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return allTrue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}