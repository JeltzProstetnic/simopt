using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Controls;

namespace MatthiasToolbox.Presentation.Converters
{
    public class FileLinkConverter : IValueConverter
    {
        public object Convert(object value,
                           Type targetType,
                           object parameter,
                           CultureInfo culture)
        {
            if (value.GetType() == typeof(string))
            {
                return new Uri(("file:///" + (value as string).Replace("\\\\", "/").Replace("\\", "/")).Replace("////", "//"));
            }
            else
            {
                throw new ArgumentException("Argument must be of type string.", "value");
            }
        }

        public object ConvertBack(object value,
                                  Type targetType,
                                  object parameter,
                                  CultureInfo culture)
        {
            if (value.GetType() == typeof(Uri))
            {
                return (value as Uri).AbsolutePath.Replace("file:///", "").Replace("/","\\").Replace("file:\\\\", "\\\\");
            }
            else
            {
                throw new ArgumentException("Argument must be of type string.", "value");
            }
        }
    }
}