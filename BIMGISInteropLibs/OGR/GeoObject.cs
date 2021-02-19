using System;
using System.Collections.Generic;
using System.Linq;

using BIMGISInteropLibs.Geometry;

namespace BIMGISInteropLibs.OGR
{
    class GeoObject
    {
        public string UsageType { get => usageType; }
        private string usageType { get; set; }

        //public enum GeomType { LINE, POLYGON, MULTIPOLYGON, CURVEDLINE }
        public OSGeo.OGR.wkbGeometryType GeomType;

        public string GmlID { get => gmlId; }
        private string gmlId { get; set; }

        public List<List<C2BSegment>> Segments { get => segments; }

        private List<List<C2BSegment>> segments { get; set; }

        public Dictionary<string, string> Properties { get => properties; }
        private Dictionary<string, string> properties { get; set; }

        public GeoObject(string usageType, string gmlID, OSGeo.OGR.wkbGeometryType GeomTyp, List<List<C2BSegment>> segments, Dictionary<string, string> properties)
        {
            this.usageType = usageType;
            this.gmlId = gmlID;
            this.segments = segments;
            this.properties = properties;
            this.GeomType = GeomTyp;
        }
    }
}
