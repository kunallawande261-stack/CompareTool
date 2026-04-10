using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FolderComparerUI.Converters
{
    /// <summary>
    /// Returns Visible when the int value is greater than zero, Collapsed otherwise.
    /// Used to show/hide the XML error list based on XmlResults.Count.
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
