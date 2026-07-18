using System;
using System.Globalization;
using System.Text;
using OWCE.Models;
using Xamarin.Forms;

namespace OWCE.Converters
{
    public class BatteryCellsToTextConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Was previously checking for `Dictionary<uint, uint>`, but OWBoard.BatteryCells
            // is actually a Models.BatteryCells - the type check never matched, so this
            // converter always fell through to "Unknown".
            if (value is BatteryCells batteryCells)
            {
                var sb = new StringBuilder();
                for (uint cellID = 0; cellID < batteryCells.CellCount; ++cellID)
                {
                    float voltage = batteryCells.GetCell(cellID);
                    sb.AppendLine($"{cellID}: ({voltage:N2}V)");
                }

                return sb.ToString();
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
