using System;
using System.Globalization;
using Xamarin.Forms;

namespace OWCE.Converters
{
    public class RpmToSpeedConverter : IValueConverter
    {
        public const float TwoPi = (2f * (float)Math.PI);
        public const float RadConvert = (TwoPi / 60f);
        public const float MillimetersPerMinuteToMetersPerSecond = 0.00001666666667f;

        // V1/Plus/XR/GT wheel circumference in mm, used when no per-board value is available.
        public const float DefaultWheelCircumference = 917.66f;

        public RpmToSpeedConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int rpm)
            {
                var wheelCircumference = (parameter is float wheelCircumfrence && wheelCircumfrence > 0f) ? wheelCircumfrence : DefaultWheelCircumference;
                return ConvertFromRpm(rpm, wheelCircumference);
            }

            return 0.0f;
        }

        public static float ConvertFromRpm(int rpm, float wheelCircumference = DefaultWheelCircumference)
        {
            var metersPerSecond = wheelCircumference * rpm * MillimetersPerMinuteToMetersPerSecond;

            if (App.Current.MetricDisplay)
            {
                return metersPerSecond * 3.6f; // kmph
            }
            else
            {
                return metersPerSecond * 2.23694f; // mph
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
