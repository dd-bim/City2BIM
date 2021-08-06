using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//integration of the BimGisCad library
using BimGisCad.Collections;                        //MESH --> will be removed
using BimGisCad.Representation.Geometry.Composed;   //TIN --> will be removed

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
        /// REMOVE
        /// </summary>
        public Mesh Mesh { get; set; } = null;

        /// <summary>
        /// REMOVE
        /// </summary>
        public Tin Tin { get; set; }

        /// <summary>
        /// [FILE-READING] transfer point list (for NTS only)
        /// </summary>
        public List<NetTopologySuite.Geometries.Point> pointList { get; set; }

        /// <summary>
        /// [FILE-READING] transfer triangles (NTS)
        /// </summary>
        public List<NetTopologySuite.Geometries.Polygon> triangleList { get; set; }

        /// <summary>
        /// [FILE-READING] transfer breaklines (NTS)
        /// </summary>
        public List<NetTopologySuite.Geometries.LineString> lines { get; set; }

        /// <summary>
        /// [FILE-WRITING] unqiue list of coordinate in a dtm
        /// </summary>
        public NetTopologySuite.Geometries.CoordinateList coordinateList { get; set; } = null;

        /// <summary>
        /// [FILE-WRITING] mapped int values (point indicies) 
        /// </summary>
        public HashSet<Triangulator.triangleMap> triMap { get; set; } = null;

        /// <summary>
        /// [FILE - WRITING] - > TXT export (only internal support)
        /// </summary>
        public NetTopologySuite.Geometries.GeometryCollection geomStore { get; set; } = null;
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
    }
}
