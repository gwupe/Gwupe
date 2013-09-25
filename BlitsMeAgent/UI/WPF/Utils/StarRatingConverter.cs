using System;
using System.Globalization;
using System.Windows.Data;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class StarRatingConverter : IValueConverter
    {
        private const int MaxCoverLength = 75;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (StarRatingConverter));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                int rating = (int) value;
                var widthOfCover = MaxCoverLength - (rating*MaxCoverLength/100);
                return widthOfCover;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
