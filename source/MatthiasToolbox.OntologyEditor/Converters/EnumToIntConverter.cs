using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MatthiasToolbox.GraphDesigner.Enumerations;

namespace MatthiasToolbox.OntologyEditor.Converters
{
    /// <summary>
    /// Convertes Enumerations to int and int to Enumeration.
    /// </summary>
    public class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type type = value.GetType();
            if (targetType == typeof(Enum))
                return Enum.ToObject(targetType, value);
            if (type.IsEnum)
                return (int)value;
            if(parameter != null)
            {
                Type enumType = Type.GetType((string) parameter);
                if(enumType != null && enumType.IsEnum)
                    return Enum.ToObject(enumType, value);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type type = value.GetType();
            if (targetType == typeof(Enum))
                return Enum.ToObject(targetType, value);
            if (type.IsEnum)
                return (int)value;
            return value;
        }
    }
}
