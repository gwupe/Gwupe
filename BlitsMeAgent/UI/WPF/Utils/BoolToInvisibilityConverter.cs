using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class BoolToInvisibilityConverter : IValueConverter
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
}