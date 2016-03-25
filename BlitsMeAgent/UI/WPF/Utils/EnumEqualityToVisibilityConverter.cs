using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Data;
using log4net;

namespace Gwupe.Agent.UI.WPF.Utils
{
    class EnumEqualityToVisibilityConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EnumEqualityToVisibilityConverter));
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return DependencyProperty.UnsetValue;
            //Logger.Debug("Checking " + value + " against " + parameter);
            return Equals((Enum) value, (Enum) parameter) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
