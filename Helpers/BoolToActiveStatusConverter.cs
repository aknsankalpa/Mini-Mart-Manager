using System.Globalization;
using System.Windows.Data;

namespace RetailFlow.Helpers;

/// <summary>
/// Displays a Product's IsActive flag as a friendly "Active"/"Inactive" label in the
/// products grid, instead of a raw True/False.
/// </summary>
public class BoolToActiveStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Active" : "Inactive";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
