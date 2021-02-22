using System;
using System.Collections.Generic;
using System.Linq;

using OSGeo.OGR;

using BIMGISInteropLibs.Geometry;

namespace BIMGISInteropLibs.OGR
{
    public class GeoObject
    {
        public string UsageType { get => usageType; }
        private string usageType { get; set; }

        //public enum GeomType { LINE, POLYGON, MULTIPOLYGON, CURVEDLINE }
        public OSGeo.OGR.wkbGeometryType GeomType;

        public OSGeo.OGR.Geometry Geom { get => geom; }
        private OSGeo.OGR.Geometry geom { get; set; }

        public string GmlID { get => gmlId; }
        private string gmlId { get; set; }

        //public List<List<C2BSegment>> Segments { get => segments; }

        //private List<List<C2BSegment>> segments { get; set; }

        public Dictionary<string, string> Properties { get => properties; }
        private Dictionary<string, string> properties { get; set; }

        public GeoObject(string usageType, string gmlID, OSGeo.OGR.wkbGeometryType geomType, OSGeo.OGR.Geometry geom ,Dictionary<string, string> properties)
        {
            this.usageType = usageType;
            this.gmlId = gmlId;
            this.GeomType = geomType;
            this.geom = geom;
            this.properties = properties;
        }

        /*
        public GeoObject(string usageType, string gmlID, OSGeo.OGR.wkbGeometryType GeomTyp, List<List<C2BSegment>> segments, Dictionary<string, string> properties)
        {
            this.usageType = usageType;
            this.gmlId = gmlID;
            this.segments = segments;
            this.properties = properties;
            this.GeomType = GeomTyp;
        }
        */
    }
}
