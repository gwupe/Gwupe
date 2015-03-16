using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using log4net;

namespace Gwupe.Agent.UI.WPF.Utils
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BoolToVisibilityConverter));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visible = Visibility.Collapsed;

            if ((value is bool && ((bool)value)))
            {
                visible = Visibility.Visible;
            }
            return visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
