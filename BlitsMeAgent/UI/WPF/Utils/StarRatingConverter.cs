using System;
using System.Globalization;
using System.Windows.Data;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class StarRatingConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (StarRatingConverter));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                int rating = (int) value;
                return 75 - (rating*75/100);
            }
            Logger.Error("Failed to convert null value for star rating");
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
