using System;
using System.Globalization;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OWCE.Converters
{
    public class RotationsToDistanceConverter : IValueConverter
    {
        public const float TwoPi = (2f * (float)Math.PI);
        public const float RadConvert = (TwoPi / 60f);

        // V1/Plus/XR/GT wheel circumference in mm, used when no per-board value is available.
        public const float DefaultWheelCircumference = 917.66f;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ushort rotations)
            {
                float wheelCircumference = (parameter is float wheelCircumfrence && wheelCircumfrence > 0f) ? wheelCircumfrence : DefaultWheelCircumference;
                return ConvertRotationsToDistance(rotations, wheelCircumference);
            }

            return 0.0f;
        }

        public static string ConvertRotationsToDistance(ushort rotations, float wheelCircumference = DefaultWheelCircumference)
        {
            float kilometers = wheelCircumference * rotations * 0.001f * 0.001f;

            if (App.Current.MetricDisplay)
            {
                return $"{kilometers.ToString("N1", CultureInfo.CurrentCulture)} km"; // kmph
            }
            else
            {
                return $"{UnitConverters.KilometersToMiles(kilometers).ToString("N1", CultureInfo.CurrentCulture)} mi";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
