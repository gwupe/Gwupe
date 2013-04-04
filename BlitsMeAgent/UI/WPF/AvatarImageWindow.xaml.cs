using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlitsMe.Agent.UI.WPF.Utils;
using Microsoft.Win32;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for AvatarImageWindow.xaml
    /// </summary>
    public partial class AvatarImageWindow : Window
    {
        private readonly BlitsMeClientAppContext _appContext;
        private InputValidator validator;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AvatarImageWindow));
        internal bool ImageReady = false;
        private BitmapImage _profileImage;
        internal BitmapImage ProfileImage
        {
            get { return _profileImage; }
            set { _profileImage = value; ImageContainer.Fill = new ImageBrush() { ImageSource = value, Stretch = Stretch.Uniform }; }
        }

        internal AvatarImageWindow(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            this.InitializeComponent();
            validator = new InputValidator(StatusText, ErrorText);
            // Insert code required on object creation below this point.
        }

        private void WebcamButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ResetStatus();
        }

        private void FileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ResetStatus();
            var fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            fileDialog.Filter = "All Graphics Types|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff";
            bool? result = fileDialog.ShowDialog(this);
            if (result == true)
            {
                string filename = fileDialog.FileName;
                try
                {
                    // Get its dimensions first
                    using (System.Drawing.Image img = System.Drawing.Image.FromFile(filename))
                    {
                        int height = img.Height;
                        int width = img.Width;
                        int larger = height > width ? height : width;
                        BitmapImage newImage = new BitmapImage();
                        newImage.BeginInit();
                        newImage.DecodePixelHeight = height/larger * 300;
                        newImage.DecodePixelWidth = width/larger * 300;
                        newImage.UriSource = new Uri(filename);
                        newImage.EndInit();
                        ProfileImage = newImage;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to load the image from file " + filename + " : " + ex.Message, ex);
                    validator.setError("Could not load image from file");
                }
            }
        }

        private void ResetStatus()
        {
            validator.ResetStatus(new Control[] { }, new Label[] { });
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ResetStatus();
            try
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(ProfileImage));
                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    _appContext.CurrentUserManager.CurrentUser.Avatar = ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save the image : " + ex.Message, ex);
                validator.setError("Failed to save the image");
                return;
            }
            Close();
        }

    }
}