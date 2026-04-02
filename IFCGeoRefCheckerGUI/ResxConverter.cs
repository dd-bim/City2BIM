using IFCGeoRefCheckerGUI.Properties;
using IFCGeorefShared;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IFCGeoRefCheckerGUI
{
    public class ResxConverter : IValueConverter, ITranslator
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value as string;
            if (key != null)
            {
                return Resources.ResourceManager.GetString(key, culture);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public string Translate(string key, CultureInfo culture)
        {
            return Resources.ResourceManager.GetString(key, culture);
        }
    }
    public class BoolToSymbolConverter : IValueConverter
    {
        // Converts bool? (boxed as bool or null) to symbols: true -> ✔, false -> ✖, null -> ?
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "?";
            if (value is bool b) return b ? "✔" : "✖";
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
