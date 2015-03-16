using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using log4net;

namespace Gwupe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for GetRating.xaml
    /// </summary>
    public partial class GetRating : UserControl
    {
        private int _savedRating = 0;
        private int _starNo = 0;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RatingControl));
        private Dictionary<string, int> _ratings;

        public GetRating(int value)
        {
            this.InitializeComponent();
            int starRating = 75 - (value * 75 / 100);

            if (starRating >= 75)
            {
                poly1.Style = this.FindResource("StarEmpty") as Style;
                poly2.Style = this.FindResource("StarEmpty") as Style;
                poly3.Style = this.FindResource("StarEmpty") as Style;
                poly4.Style = this.FindResource("StarEmpty") as Style;
                poly5.Style = this.FindResource("StarEmpty") as Style;
            }
            else if (starRating >= 60)
            {
                poly1.Style = this.FindResource("StarFull") as Style;
                poly2.Style = this.FindResource("StarEmpty") as Style;
                poly3.Style = this.FindResource("StarEmpty") as Style;
                poly4.Style = this.FindResource("StarEmpty") as Style;
                poly5.Style = this.FindResource("StarEmpty") as Style;
            }
            else if (starRating >= 45)
            {
                poly1.Style = this.FindResource("StarFull") as Style;
                poly2.Style = this.FindResource("StarFull") as Style;
                poly3.Style = this.FindResource("StarEmpty") as Style;
                poly4.Style = this.FindResource("StarEmpty") as Style;
                poly5.Style = this.FindResource("StarEmpty") as Style;
                
            }
            else if (starRating >= 30)
            {
                poly1.Style = this.FindResource("StarFull") as Style;
                poly2.Style = this.FindResource("StarFull") as Style;
                poly3.Style = this.FindResource("StarFull") as Style;
                poly4.Style = this.FindResource("StarEmpty") as Style;
                poly5.Style = this.FindResource("StarEmpty") as Style;
            }
            else if (starRating >= 15)
            {
                poly1.Style = this.FindResource("StarFull") as Style;
                poly2.Style = this.FindResource("StarFull") as Style;
                poly3.Style = this.FindResource("StarFull") as Style;
                poly4.Style = this.FindResource("StarFull") as Style;
                poly5.Style = this.FindResource("StarEmpty") as Style;
            }
            else if (starRating >= 0)
            {
                poly1.Style = this.FindResource("StarFull") as Style;
                poly2.Style = this.FindResource("StarFull") as Style;
                poly3.Style = this.FindResource("StarFull") as Style;
                poly4.Style = this.FindResource("StarFull") as Style;
                poly5.Style = this.FindResource("StarFull") as Style;
            }
            else
            {
                poly1.Style = this.FindResource("StarFull") as Style;
                poly2.Style = this.FindResource("StarFull") as Style;
                poly3.Style = this.FindResource("StarFull") as Style;
                poly4.Style = this.FindResource("StarFull") as Style;
                poly5.Style = this.FindResource("StarFull") as Style;
            }
        }
    }
}
