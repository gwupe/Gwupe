using System;
using System.Globalization;
using System.Windows.Data;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class BoolToStringConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BoolToStringConverter));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String str = String.Empty;

            if (value is bool)
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