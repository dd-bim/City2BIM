using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    public class Level50UI
    {
        public double? Eastings { get; set; }
        public double? Northings { get; set; }
        public double? OrhtogonalHeight { get; set; }
        public double? XAxisAbscissa { get; set; }
        public double? XAxisOrdinate { get; set; }
        public double? Scale { get; set; }

        public string? Name { get; set; }
        public string? GeodeticDatum { get; set; }
        public string? VerticalDatum { get; set; }
        public string? MapZone { get; set; }
        public string? MapProjection { get; set; }
    }
}
