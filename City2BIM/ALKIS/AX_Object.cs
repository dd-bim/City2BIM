using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.Alkis
{
    public class AX_Object
    {
        public enum AXGroup { parcel, building, usage};
        private string usageType;
        private List<C2BPoint[]> segments;
        private List<List<C2BPoint[]>> innerSegments;
        private AXGroup group;
        private Dictionary<Xml_AttrRep, string> attributes;

        public string UsageType { get => usageType; set => usageType = value; }
        public List<C2BPoint[]> Segments { get => segments; set => segments = value; }
        public AXGroup Group { get => group; set => group = value; }
        public Dictionary<Xml_AttrRep, string> Attributes { get => attributes; set => attributes = value; }
        public List<List<C2BPoint[]>> InnerSegments { get => innerSegments; set => innerSegments = value; }
    }
}
