using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using BlitsMe.Agent.Properties;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    class ImageStreamReader : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CreateBitmapImage((byte[])value);
        }

        internal static BitmapImage CreateBitmapImage(byte[] value)
        {
            BitmapImage image;
            image = new BitmapImage();
            image.BeginInit();
            if (value == null)
            {
                // default image
                image.UriSource = new Uri("pack://application:,,,/UI/WPF/Images/silhoette.png", UriKind.RelativeOrAbsolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
            }
            else
            {
                image.StreamSource = new MemoryStream(value);
            }
            image.EndInit();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
