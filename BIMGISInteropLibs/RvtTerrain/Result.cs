using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimGisCad.Representation.Geometry.Elementary; //Point3

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
        public List<Point3> dtmPoints { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<DtmFace> terrainFaces { get; set; }

        public int numPoints { set; get; }

        public int numFacets { set; get; }

        public static conversionEnum processingEnum
        {
            set;
            get;
        }

        public enum conversionEnum
        {
            ConversionViaPoints = 1,
            ConversionViaFaces = 2,
            TriangulationViaPoints = 3,
            TriangulationViaPointsAndBreaklines = 4,
            TriangulationViaFacesAndBreaklines = 5
        }
    }
}
