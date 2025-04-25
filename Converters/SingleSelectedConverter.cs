using System;
using System.Globalization;
using System.Windows.Data;

namespace BookSteward
{
    public class SingleSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
            {
                return false;
            }
            string currentView = values[0].ToString();
            string tag = values[1].ToString();

            // 直接比较当前视图和标签值
            return currentView == tag;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}