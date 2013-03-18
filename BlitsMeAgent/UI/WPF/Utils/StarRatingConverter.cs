using System;
using System.Globalization;
using System.Windows.Data;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class StarRatingConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           int rating = (int) value;
            return 75 - (rating * 75 / 100);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
