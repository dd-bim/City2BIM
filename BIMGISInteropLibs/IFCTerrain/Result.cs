using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IfcTerrain
{
    /// <summary>
    /// Exchange class for processing tins, break edges and error messages
    /// </summary>
    public class Result
    {
        /// <summary>
        /// internale use
        /// </summary>
        public DtmConversionType currentConversion { get; set; }

        /// <summary>
        /// [FILE-READING] transfer point list (for NTS only)
        /// </summary>
        public List<Point> pointList { get; set; }

        /// <summary>
        /// [FILE-READING] transfer triangles (NTS)
        /// </summary>
        public List<Polygon> triangleList { get; set; }

        /// <summary>
        /// [FILE-READING] transfer breaklines (NTS)
        /// </summary>
        public List<LineString> lines { get; set; }

        /// <summary>
        /// [FILE-WRITING] unqiue list of coordinates in a dtm
        /// </summary>
        public CoordinateList coordinateList { get; set; } = null;

        /// <summary>
        /// [FILE - WRITING] txt export (only internal support)
        /// </summary>
        public GeometryCollection geomStore { get; set; } = null;

        /// <summary>
        /// [FILE-WRITING] mapped int values (point indicies) 
        /// </summary>
        public HashSet<Triangulator.triangleMap> triMap { get; set; } = new HashSet<Triangulator.triangleMap>();

        /// <summary>
        /// [IFCTerrain] exchange origin
        /// </summary>
        public Coordinate origin { get; set; } = null;
    }

    /// <summary>
    /// different szenarios for dtm conversion
    /// </summary>
    public enum DtmConversionType
    {
        /// <summary>
        /// dtm contains points --> need to do a delauny triangulation
        /// </summary>
        points,

        /// <summary>
        /// dtm contains faces --> conversion of the given faces
        /// </summary>
        faces,

        /// <summary>
        /// dtm contains points & breaklines --> need to do a conforming delauny triangulation
        /// </summary>
        points_breaklines,

        /// <summary>
        /// dtm contains faces & breaklines --> need to do a conforming delauny triangulation
        /// </summary>
        faces_breaklines,

        /// <summary>
        /// dtm contains index map (delauany triangulation is not necessary) (no breakline processing)
        /// </summary>
        conversion
    }
}
