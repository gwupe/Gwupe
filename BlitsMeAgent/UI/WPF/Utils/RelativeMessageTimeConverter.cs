using System;
using System.Globalization;
using System.Windows.Data;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    public class RelativeMessageTimeConverter : IValueConverter
    {
        private const String TodayFormat = "Today at {0:HH:mm}";
        private const String YesterdayFormat = "Yesterday at {0:HH:mm}";
        private const String DoWFormat = "{0:dddd at HH:mm}";
        private const String ThisYearFormat = "{0:MMMM d HH:mm}";
        private const String PreviousYearsFormat = "{0:yyyy/MM/dd HH:mm}";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String result;
            DateTime dateTime = (DateTime) value;
            // Periods are Today, Yesterday, DoW for the last week, then date for the last year, then date with year
            DateTime nowDate = DateTime.Now;

            // Today
            DateTime referenceDate = new DateTime(nowDate.Year, nowDate.Month, nowDate.Day);
            if(dateTime > referenceDate)
            {
                result = String.Format(TodayFormat, dateTime);
            } else
            {
                // Yesterday
                referenceDate = nowDate.AddDays(-1);
                referenceDate = new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day);
                if(dateTime > referenceDate)
                {
                    result = String.Format(YesterdayFormat, dateTime);
                } else
                {
                    // Week Past
                    referenceDate = nowDate.AddDays(-7);
                    referenceDate = new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day);
                    if (dateTime > referenceDate)
                    {
                        result = String.Format(DoWFormat, dateTime);
                    }
                    else
                    {
                        // Year Past
                        referenceDate = new DateTime(nowDate.Year);
                        if (dateTime > referenceDate)
                        {
                            result = String.Format(ThisYearFormat, dateTime);
                        }
                        else
                        {
                            // Previous Years
                            result = String.Format(PreviousYearsFormat, dateTime);
                        }

                    }
                    
                }
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
