using System;
using System.Globalization;
using Xamarin.Forms;

namespace OWCE.Converters
{
    public class KilometerToMileConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = "NO";

            if (parameter is string formatString)
            {
                format = formatString;
            }

            if (value is float distanceInKilometersAsFloat)
            {
                if (App.Current.MetricDisplay)
                {
                    return $"{distanceInKilometersAsFloat.ToString(format, culture)} km";
                }

                float distanceInMiles = distanceInKilometersAsFloat * 0.621371f;
                return $"{distanceInMiles.ToString(format, culture)} mi";
            }
            else if (value is int distanceInKilometersAsInt)
            {
                if (App.Current.MetricDisplay)
                {
                    return $"{distanceInKilometersAsInt.ToString(format, culture)} km";
                }

                float distanceInMiles = (float)distanceInKilometersAsInt * 0.621371f;
                return $"{distanceInMiles.ToString(format, culture)} mi";

            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
