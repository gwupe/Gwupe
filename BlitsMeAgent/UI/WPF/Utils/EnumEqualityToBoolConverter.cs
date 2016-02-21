using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Gwupe.Agent.UI.WPF.Utils
{
    class EnumEqualityToBoolConverter : IValueConverter
    {
        public Enum FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return DependencyProperty.UnsetValue;

            return Equals((Enum) value, (Enum) parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool && (bool) value)
            {
                return (Enum) parameter;
            }
            return FalseValue;
        }

    }
}
