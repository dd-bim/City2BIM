using System;
using System.Collections.Generic;
using System.Linq;

using BIMGISInteropLibs.Geometry;
using BIMGISInteropLibs.Semantic;

namespace BIMGISInteropLibs.XPlanung
{
    public class XPlanungObject
    {
        public enum XGruop { BP_Bereich, BP_Plan, other}
        public enum geomType {Polygon, LineString, Curve, other}

        private XGruop group;
        private string usageType;
        private List<C2BPoint[]> segments;
        private List<List<C2BPoint[]>> innerSegments;
        private Dictionary<string, string> attributes;
        private string gmlid;
        private geomType geom;

        /// <summary>
        /// Stores object type ("AX_xy") for later differentation
        /// </summary>
        public string UsageType { get => usageType; set => usageType = value; }

        /// <summary>
        /// Stores attributes (currently only for parcels)
        /// </summary>
        public Dictionary<string, string> Attributes { get => attributes; set => attributes = value; }

        public XGruop Group { get => group; set => group = value; }

        public geomType Geom { get => geom; set => geom = value; }

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
                    polyInt.Add(segInt[0][0]);                                    //convert Segments to LinearRing

                    polysInt.Add(polyInt);
                }
                return polysInt;
            }
            return polysInt;
        }

    }

}
