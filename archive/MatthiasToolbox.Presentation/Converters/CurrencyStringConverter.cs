using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace MatthiasToolbox.Presentation.Converters
{
    public class CurrencyStringConverter : IValueConverter
    {
        private bool invert;

        public CurrencyStringConverter(bool invert = false)
        {
            this.invert = invert;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (invert) return DoConvertBack(value, targetType, parameter, culture);
            else return DoConvert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (invert) return DoConvert(value, targetType, parameter, culture);
            else return DoConvertBack(value, targetType, parameter, culture);
        }

        private object DoConvert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.GetType() != typeof(float)) throw new ArgumentException("Argument must be of type float.", "value");
            float amount = (float)value;
            return amount.ToString("c");
        }

        private object DoConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.GetType() != typeof(string)) throw new ArgumentException("Argument must be of type string.", "value");
            float amount = 0;
            if (!float.TryParse((string)value, NumberStyles.Currency, CultureInfo.CurrentCulture, out amount))
                throw new ArgumentException("Unable to convert argument \"" + (string)value + "\" to float.", "value");
            return amount;
        }
    }
}