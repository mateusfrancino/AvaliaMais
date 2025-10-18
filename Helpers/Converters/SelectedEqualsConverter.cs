using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalia_.Helpers.Converters
{
    public class SelectedEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = AttendantScore (double), values[1] = item.Value (int)
            if (values.Length == 2 && values[0] is double score)
            {
                var item = values[1] is int i ? i :
                           (values[1] is string s && int.TryParse(s, out var p) ? p : -1);
                return Math.Abs(score - item) < 0.0001;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
