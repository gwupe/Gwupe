using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using log4net;
using System;

namespace BlitsMe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl
    {
        private int _savedRating = 0;
        private int _starNo = 0;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RatingControl));
        private Dictionary<string, int> _ratings;

        public RatingControl()
        {
            this.InitializeComponent();
            RatingCover.Width = 0;
            poly1.Style = this.FindResource("StarEmpty") as Style;
            poly2.Style = this.FindResource("StarEmpty") as Style;
            poly3.Style = this.FindResource("StarEmpty") as Style;
            poly4.Style = this.FindResource("StarEmpty") as Style;
            poly5.Style = this.FindResource("StarEmpty") as Style;
        }

        private void StarMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var star = sender as Button;
            if (star != null)
            {
                if ("Star1".Equals(star.Name))
                {
                    RatingCover.Width = 60;
                    _starNo = 1;
                }
                else if ("Star2".Equals(star.Name))
                {
                    RatingCover.Width = 45;
                    _starNo = 2;
                }
                else if ("Star3".Equals(star.Name))
                {
                    RatingCover.Width = 30;
                    _starNo = 3;
                }
                else if ("Star4".Equals(star.Name))
                {
                    RatingCover.Width = 15;
                    _starNo = 4;
                }
                else if ("Star5".Equals(star.Name))
                {
                    RatingCover.Width = 0;
                    _starNo = 5;
                }
            }
        }

        private void StarClick(object sender, RoutedEventArgs routedEventArgs)
        {
            _savedRating = (int)(100 - (RatingCover.Width * 100 / 75));
            //MethodInfo methodInfo = DataContext.GetType().GetMethod("SetRating");
            //if(methodInfo != null)
            //{
            //    methodInfo.Invoke(DataContext, new object[] {Name, _savedRating});
            //} else
            //{
            //    Logger.Warn("Cannot set rating on server for " + Name + ", no SetRating method on DataContext");
            //}

            for (int i = 0; i < 5; i++)
            {
                switch (_starNo)
                {
                    case 1:
                        poly1.Style = this.FindResource("StarFull") as Style;
                        poly2.Style = this.FindResource("StarEmpty") as Style;
                        poly3.Style = this.FindResource("StarEmpty") as Style;
                        poly4.Style = this.FindResource("StarEmpty") as Style;
                        poly5.Style = this.FindResource("StarEmpty") as Style;
                        break;
                    case 2:
                        poly1.Style = this.FindResource("StarFull") as Style;
                        poly2.Style = this.FindResource("StarFull") as Style;
                        poly3.Style = this.FindResource("StarEmpty") as Style;
                        poly4.Style = this.FindResource("StarEmpty") as Style;
                        poly5.Style = this.FindResource("StarEmpty") as Style;
                        break;
                    case 3:
                        poly1.Style = this.FindResource("StarFull") as Style;
                        poly2.Style = this.FindResource("StarFull") as Style;
                        poly3.Style = this.FindResource("StarFull") as Style;
                        poly4.Style = this.FindResource("StarEmpty") as Style;
                        poly5.Style = this.FindResource("StarEmpty") as Style;
                        break;
                    case 4:
                        poly1.Style = this.FindResource("StarFull") as Style;
                        poly2.Style = this.FindResource("StarFull") as Style;
                        poly3.Style = this.FindResource("StarFull") as Style;
                        poly4.Style = this.FindResource("StarFull") as Style;
                        poly5.Style = this.FindResource("StarEmpty") as Style;
                        break;
                    case 5:
                        poly1.Style = this.FindResource("StarFull") as Style;
                        poly2.Style = this.FindResource("StarFull") as Style;
                        poly3.Style = this.FindResource("StarFull") as Style;
                        poly4.Style = this.FindResource("StarFull") as Style;
                        poly5.Style = this.FindResource("StarFull") as Style;
                        break;
                }
            }
        }

        private void StarMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RatingCover.Width = 0;
        }
    }

}