using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl
    {
        private int _savedRating = 0;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RatingControl));
        private Dictionary<string, int> _ratings;

        public RatingControl()
        {
            this.InitializeComponent();
        }

        private void StarMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var star = sender as Button;
            if (star != null)
            {
                if ("Star1".Equals(star.Name))
                {
                    RatingCover.Width = 60;
                }
                else if ("Star2".Equals(star.Name))
                {
                    RatingCover.Width = 45;
                }
                else if ("Star3".Equals(star.Name))
                {
                    RatingCover.Width = 30;
                }
                else if ("Star4".Equals(star.Name))
                {
                    RatingCover.Width = 15;
                }
                else if ("Star5".Equals(star.Name))
                {
                    RatingCover.Width = 0;
                }
            }
        }

        private void StarClick(object sender, RoutedEventArgs routedEventArgs)
        {
            _savedRating = (int)(100 - (RatingCover.Width * 100 / 75));
            MethodInfo methodInfo = DataContext.GetType().GetMethod("SetRating");
            if(methodInfo != null)
            {
                methodInfo.Invoke(DataContext, new object[] {Name, _savedRating});
            } else
            {
                Logger.Warn("Cannot set rating on server for " + Name + ", no SetRating method on DataContext");
            }
        }

        private void StarMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RatingCover.Width = (100 - _savedRating) * 75 / 100;
        }
    }

}