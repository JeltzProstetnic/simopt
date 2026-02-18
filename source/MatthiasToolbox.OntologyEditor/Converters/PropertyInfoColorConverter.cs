using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;

namespace MatthiasToolbox.OntologyEditor.Views
{
    public class PropertyInfoColorConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        /// <summary>
        /// Converts Color to PropertyInfo. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color)
            {
                if (targetType == typeof(Color))
                    return value;

                if (targetType == typeof(PropertyInfo))
                    return ColorToPropertyInfo((Color)value);
            }
            else if (value is PropertyInfo)
            {
                if (targetType == typeof(Color))
                    return PropertyInfoToColor((PropertyInfo)value);
                else if (targetType == typeof(PropertyInfo))
                    return value;
            }

            return value;
        }



        /// <summary>
        /// Converts PropertyInfo into System.Windows.Media.Color. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PropertyInfo)
            {
                PropertyInfo info = value as PropertyInfo;
                return PropertyInfoToColor(info);
            }
            else if (value is Color)
                return value;

            return value;
        }

        #endregion
        #region static

        /// <summary>
        /// Converts Colors.PropertyInfo to Color
        /// </summary>
        /// <param name="info">The Colors.PropertyInfo.</param>
        /// <returns></returns>
        public static Color PropertyInfoToColor(PropertyInfo info)
        {
            return (System.Windows.Media.Color) System.Windows.Media.ColorConverter.ConvertFromString(info.Name);
        }

        /// <summary>
        /// Converts a Color into the PropertyInfo of Colors.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public static PropertyInfo ColorToPropertyInfo(Color color)
        {
            // get Colors.xxx Property Info
            Type colors = typeof(Colors);

            foreach (var info in colors.GetProperties())
            {
                Color c = (Color)info.GetValue(colors, null);

                if (color == c)
                    return info;
            }

            return null;
        }

        #endregion
    }
}
