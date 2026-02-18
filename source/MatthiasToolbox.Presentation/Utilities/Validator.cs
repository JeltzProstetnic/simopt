using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;

namespace MatthiasToolbox.Presentation.Utilities
{
    public static class Validator
    {
        private static Dictionary<Type, List<DependencyProperty>> PropertiesReflectionChace = new Dictionary<Type, List<DependencyProperty>>();

        private static List<DependencyProperty> GetDPs(Type t)
        {
            if (PropertiesReflectionChace.ContainsKey(t))
                return PropertiesReflectionChace[t];
            FieldInfo[] properties = t.GetFields(BindingFlags.Public | BindingFlags.GetProperty |
                 BindingFlags.Static | BindingFlags.FlattenHierarchy);
            // we cycle and store only the dependency properties
            List<DependencyProperty> dps = new List<DependencyProperty>();

            foreach (FieldInfo field in properties)
                if (field.FieldType == typeof(DependencyProperty))
                    dps.Add((DependencyProperty)field.GetValue(null));
            PropertiesReflectionChace.Add(t, dps);

            return dps;
        }

        /// <summary>
        /// checks all the validation rule associated with objects,
        /// forces the binding to execute all their validation rules
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool IsValid(DependencyObject parent)
        {
            // Validate all the bindings on the parent
            bool valid = true;
            // get the list of all the dependency properties, we can use a level of caching to avoid to use reflection
            // more than one time for each object
            foreach (DependencyProperty dp in GetDPs(parent.GetType()))
            {
                if (BindingOperations.IsDataBound(parent, dp))
                {
                    Binding binding = BindingOperations.GetBinding(parent, dp);
                    if (binding.ValidationRules.Count > 0)
                    {
                        BindingExpression expression = BindingOperations.GetBindingExpression(parent, dp);
                        switch (binding.Mode)
                        {
                            case BindingMode.OneTime:
                            case BindingMode.OneWay:
                                expression.UpdateTarget();
                                break;
                            default:
                                expression.UpdateSource();
                                break;
                        }
                        if (expression.HasError) valid = false;
                    }
                }
            }

            // Validate all the bindings on the children
            for (int i = 0; i != VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (!IsValid(child)) { valid = false; }
            }

            return valid;
        }
    }
}