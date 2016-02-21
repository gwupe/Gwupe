using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Gwupe.Agent.Properties;

namespace Gwupe.Agent.UI.WPF.Utils
{
    class ImageStreamReader : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CreateBitmapImage((byte[])value);
        }

        public String DefaultImageUri { get; set; }

        internal BitmapImage CreateBitmapImage(byte[] value)
        {
            var image = new BitmapImage();
            image.BeginInit();
            if (value == null)
            {
                // default image
                if (DefaultImageUri != null)
                {
                    image.UriSource = new Uri(DefaultImageUri, UriKind.RelativeOrAbsolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                }
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
