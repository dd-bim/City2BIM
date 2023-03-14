using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    class IfcLabelConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value.ToString()) ? "" : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var info = string.IsNullOrEmpty((string)value) ? null : (string)value;
            var label = new IfcLabel(info);
            return label;
        }
    }
}