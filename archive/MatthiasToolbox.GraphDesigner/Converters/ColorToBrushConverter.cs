using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace MatthiasToolbox.Presentation.Converters
{
    /// <summary>
    /// Converts a System.Windows.Media.Color to System.Windows.Media.SolidColorBrush and back.
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is Color)) 
                throw new InvalidOperationException("Value must be a Color"); 
            
            return new SolidColorBrush((Color)value);
        }      
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(!(value is SolidColorBrush))
                throw new InvalidOperationException("Value must be a SolidColorBrush");

            SolidColorBrush brush = value as SolidColorBrush;
            return brush.Color;
        }

        #endregion
    }
}
