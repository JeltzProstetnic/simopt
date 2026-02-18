using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MatthiasToolbox.Presentation.Utilities
{
    public class IsNotNullOrEmptyValidation : ValidationRule
    {
        #region Overrides of ValidationRule

        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Windows.Controls.ValidationResult"/> object.
        /// </returns>
        /// <param name="value">The value from the binding target to check.</param><param name="cultureInfo">The culture to use in this rule.</param>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is string))
                return new ValidationResult(false, "Value is not a String.");

            if (string.IsNullOrWhiteSpace((string)value))
                return new ValidationResult(false, "Empty concept name not allowed");

            return new ValidationResult(true, null);
        }

        #endregion
    }
}
