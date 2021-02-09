using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Include localization
using System.Globalization;

namespace BIMGISInteropLibs.IFCTerrain
{
    public static class Common
    {
        public static readonly Dictionary<string, double> ToMeter = new Dictionary<string, double>()
        {
            ["millimeter"] = 0.001,
            ["centimeter"] = 0.01,
            ["kilometer"] = 1000.0,
            ["foot"] = 0.3048,
            ["inch"] = 0.0254,
            ["mile"] = 1609.34,
            ["ussurveyfoot"] = 1200.0 / 3937.0
        };

        public static double SaveParse(string str, double defaultDbl = double.NaN) => double.TryParse(str.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double dbl) ? dbl : defaultDbl;
    }
}
