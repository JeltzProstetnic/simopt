using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace MatthiasToolbox.Presentation.Converters
{
    public class CurrencyColorConverter : IValueConverter
    {
        private Color negativeColor;
        private Color positiveColor;

        public CurrencyColorConverter(Color negativeColor, Color positiveColor)
        {
            this.negativeColor = negativeColor;
            this.positiveColor = positiveColor;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.GetType() != typeof(float)) throw new ArgumentException("Argument must be of type float.", "value");
            float amount = (float)value;
            if (amount < 0) return new SolidColorBrush(negativeColor);
            return new SolidColorBrush(positiveColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("The color cannot be converted back to an amount.");
        }
    }
}