using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//include for data exchange (revit)
using C2BPoint = BIMGISInteropLibs.Geometry.C2BPoint;

namespace BIMGISInteropLibs.RvtTerrain
{
    /// <summary>
    /// DTM2BIM exchange class <para/>
    /// Data exchange between ConnectionInterface & DTM writer
    /// </summary>
    public class Result
    {
        /// <summary>
        /// 
        /// </summary>
        public List<C2BPoint> dtmPoints { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<DtmFace> terrainFaces { get; set; }

        public int numPoints { set; get; }

        public int numFacets { set; get; }
    }
}
