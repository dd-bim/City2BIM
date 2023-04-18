using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Xbim.Ifc4.Interfaces;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    class RefEntityGUIDConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var product = (IIfcProduct)value;
            var guid = product.GlobalId.ToString();
            return guid;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
