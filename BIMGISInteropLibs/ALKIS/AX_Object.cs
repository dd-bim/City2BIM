using BIMGISInteropLibs.Geometry;
using BIMGISInteropLibs.Semantic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.Alkis
{
    /// <summary>
    /// Class for an ALKIS object (AX_Object like ALKIS schema)
    /// </summary>
    public class AX_Object
    {
        public enum AXGroup { parcel, building, usage};
        private string usageType;
        private List<C2BPoint[]> segments;
        private List<List<C2BPoint[]>> innerSegments;
        private AXGroup group;
        private Dictionary<Xml_AttrRep, string> attributes;
        private string gmlid;

        /// <summary>
        /// Stores object type ("AX_xy") for later differentation
        /// </summary>
        public string UsageType { get => usageType; set => usageType = value; }

        /// <summary>
        /// Stores overall group (Parcel, Usage, Building)
        /// </summary>
        public AXGroup Group { get => group; set => group = value; }

        /// <summary>
        /// Stores attributes (currently only for parcels)
        /// </summary>
        public Dictionary<Xml_AttrRep, string> Attributes { get => attributes; set => attributes = value; }

        /// <summary>
        /// Stores exterior line segments
        /// </summary>
        public List<C2BPoint[]> Segments { get => segments; set => segments = value; }

        /// <summary>
        /// Stores interior line segments (if applicable, could bw 
        /// </summary>
        public List<List<C2BPoint[]>> InnerSegments { get => innerSegments; set => innerSegments = value; }

        /// <summary>
        /// Stores interior line segments (if applicable, could bw 
        /// </summary>
        public string Gmlid { get => gmlid; set => gmlid = value; }

        public List<C2BPoint> getOuterRing()
        {
            List<C2BPoint> polyExt = this.Segments.Select(j => j[0]).ToList();
            polyExt.Add(Segments[0][0]);
            return polyExt;
        }

        public List<List<C2BPoint>> getInnerRings()
        {
            List<List<C2BPoint>> polysInt = new List<List<C2BPoint>>();
            if (this.InnerSegments != null)
            {
                foreach (var segInt in this.InnerSegments)
                {
                    List<C2BPoint> polyInt = segInt.Select(j => j[0]).ToList();

                    polyInt.Add(segInt[0][0]);
                    polysInt.Add(polyInt);
                }
                return polysInt;
            }
            return polysInt;
        }
    }
}
