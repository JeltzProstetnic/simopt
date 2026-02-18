using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace MatthiasToolbox.Presentation.Converters
{
    public class TicksConverter : IValueConverter
    {
        public object Convert(object value,
                           Type targetType,
                           object parameter,
                           CultureInfo culture)
        {
            if (value.GetType() == typeof(long))
            {
                long ticks = (long)value;
                return new DateTime(ticks);
            }
            else if (value.GetType() == typeof(double))
            {
                double ticks = (double)value;
                return new DateTime((long)ticks);
            }
            else
            {
                throw new ArgumentException("Argument must be of type long.", "value");
            }
        }

        public object ConvertBack(object value,
                                  Type targetType,
                                  object parameter,
                                  CultureInfo culture)
        {
            if (value.GetType() != typeof(DateTime)) throw new ArgumentException("Argument must be of type DateTime.", "value");
            DateTime date = (DateTime)value;
            return date.Ticks;
        }
    }
}