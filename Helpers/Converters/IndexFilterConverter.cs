using System.Globalization;
using System.Collections;
using System.Collections.ObjectModel;

namespace Avalia_.Helpers.Converters;

public class IndexFilterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable list && int.TryParse(parameter?.ToString(), out var index))
        {
            var result = new ObservableCollection<object>();
            int i = 0;
            foreach (var item in list)
            {
                if (i == index)
                {
                    result.Add(item);
                    break;
                }
                i++;
            }
            return result;
        }
        return new ObservableCollection<object>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
