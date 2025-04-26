using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BookSteward
{
    public class ExtensionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string extension)
            {
                // 移除可能的点号前缀
                string ext = extension.TrimStart('.').ToLower();

                // 根据扩展名返回对应颜色
                switch (ext)
                {
                    case "txt": return new SolidColorBrush(Colors.Black);
                    case "pdf": return new SolidColorBrush(Colors.Brown);
                    case "mobi": return new SolidColorBrush(Colors.DodgerBlue);
                    case "epub": return new SolidColorBrush(Colors.DarkGreen);
                    case "azw3": return new SolidColorBrush(Colors.BlueViolet);
                    default: return new SolidColorBrush(Colors.Gray);
                }
            }

            return new SolidColorBrush(Colors.Gray); // 默认颜色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
