using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalia_.Helpers.Converters
{
    // Color: retorna SelectedColor quando item.Value == AttendantScore, senão UnselectedColor
    public class SelectedToColorConverter : IMultiValueConverter
    {
        public Color SelectedColor { get; set; } = Color.FromArgb("#FACC15"); // amarelo
        public Color UnselectedColor { get; set; } = Color.FromArgb("#E5E7EB"); // cinza claro

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = AttendantScore (double); values[1] = item.Value (int)
            if (values is { Length: 2 } &&
                values[0] is double score &&
                TryToInt(values[1], out var itemValue))
            {
                return Math.Abs(score - itemValue) < 0.0001 ? SelectedColor : UnselectedColor;
            }
            return UnselectedColor;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static bool TryToInt(object? v, out int i)
        {
            switch (v)
            {
                case int iv: i = iv; return true;
                case string s when int.TryParse(s, out var parsed): i = parsed; return true;
                default: i = 0; return false;
            }
        }
    }

    // Opacity: retorna SelectedOpacity quando item.Value == AttendantScore, senão UnselectedOpacity
    public class SelectedToOpacityConverter : IMultiValueConverter
    {
        public double SelectedOpacity { get; set; } = 1.0;
        public double UnselectedOpacity { get; set; } = 0.55;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is { Length: 2 } &&
                values[0] is double score &&
                SelectedToColorConverterTryToInt(values[1], out var itemValue))
            {
                return Math.Abs(score - itemValue) < 0.0001 ? SelectedOpacity : UnselectedOpacity;
            }
            return UnselectedOpacity;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        // reuso simples
        private static bool SelectedToColorConverterTryToInt(object? v, out int i)
        {
            switch (v)
            {
                case int iv: i = iv; return true;
                case string s when int.TryParse(s, out var parsed): i = parsed; return true;
                default: i = 0; return false;
            }
        }
        
    }

}
