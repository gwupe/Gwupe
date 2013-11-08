using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BoolToVisibilityConverter));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visible = Visibility.Visible;

            if ((value is bool && ((bool)value)))
            {
                visible = Visibility.Collapsed;
            }
            return visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BoolToStringConverter));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String str = String.Empty;

            if ((value is bool && ((bool)value)))
            {
                str = value.ToString();
            }
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
