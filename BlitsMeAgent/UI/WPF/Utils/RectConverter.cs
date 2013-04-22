using System;
using System.Windows;
using System.Windows.Data;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double width = values[0] == null || DependencyProperty.UnsetValue.Equals(values[0]) ? 0 : (double)values[0];

            double height = values[1] == null || DependencyProperty.UnsetValue.Equals(values[1]) ? 0 : (double)values[1];
            return new Rect(0, 0, width, height);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
