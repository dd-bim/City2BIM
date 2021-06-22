using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Import NetTopologySuite class library.
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;

namespace BIMGISInteropLibs.NTSApi
{
    class NtsApi
    {
        private GeometryFactory geomFactory { get; set; }

        /// <summary>
        /// A constructor creating an interface to the NetTopologySuite class library.
        /// </summary>
        public NtsApi()
        {
            NtsGeometryServices.Instance = new NtsGeometryServices(new PrecisionModel(1000d));
            this.geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
        }

        /// <summary>
        /// A function which turns a list of input point data into a list of triangle data using the NetTopologySuite class library.
        /// </summary>
        /// <param name="dtmPointList">A List of DTM point data.</param>
        /// <returns>A List of triangle data.</returns>
        public List<List<double[]>> MakeTriangleList(List<double[]> dtmPointList)
        {
            List<List<double[]>> dtmTriangleList = new List<List<double[]>>();
            GeometryCollection triangles = this.Triangulate(dtmPointList);
            foreach (NetTopologySuite.Geometries.Geometry geom in triangles.Geometries)
            {
                List<double[]> trianglePointCoordList = new List<double[]>();
                foreach(CoordinateZ coordZ in geom.Coordinates)
                {
                    trianglePointCoordList.Add(new double[] { coordZ.X, coordZ.Y, coordZ.Z });
                }
                dtmTriangleList.Add(trianglePointCoordList);
            }
            return dtmTriangleList;
        }

        /// <summary>
        /// A function which turns a list of input point and line data into a list of triangle data using the NetTopologySuite class library.
        /// </summary>
        /// <param name="dtmPointList">A List of DTM point data.</param>
        /// <param name="dtmLineList">A list of DTM line data (e. g. breaklines).</param>
        /// <returns>A List of triangle data.</returns>
        public List<List<double[]>> MakeTriangleList(List<double[]> dtmPointList, List<List<double[]>> dtmLineList)
        {
            List<List<double[]>> dtmTriangleList = new List<List<double[]>>();
            GeometryCollection triangles = this.Triangulate(dtmPointList, dtmLineList);
            foreach (NetTopologySuite.Geometries.Geometry geom in triangles.Geometries)
            {
                List<double[]> trianglePointCoordList = new List<double[]>();
                foreach (CoordinateZ coordZ in geom.Coordinates)
                {
                    trianglePointCoordList.Add(new double[] { coordZ.X, coordZ.Y, coordZ.Z });
                }
                dtmTriangleList.Add(trianglePointCoordList);
            }
            return dtmTriangleList;
        }

        /// <summary>
        /// A auxiliary function to triangulate the input point data.
        /// </summary>
        /// <param name="dtmPointList">A List of DTM point data.</param>
        /// <returns>A collection of OGC simple feature geometries representing the caclculated triangles.</returns>
        public GeometryCollection Triangulate(List<double[]> dtmPointList)
        {
            ConformingDelaunayTriangulationBuilder triangulationBuilder = new ConformingDelaunayTriangulationBuilder();
            triangulationBuilder.SetSites(this.geomFactory.CreateMultiPoint(MakePointCoordinateSequence(dtmPointList)));
            return triangulationBuilder.GetTriangles(this.geomFactory);
        }

        /// <summary>
        /// A auxiliary function to triangulate the input point and line data.
        /// </summary>
        /// <param name="dtmPointList">A List of DTM point data.</param>
        /// <param name="dtmLineList">A list of DTM line data (e. g. breaklines).</param>
        /// <returns>A collection of OGC simple feature geometries representing the caclculated triangles. The data set implicitly contains breaklines.</returns>
        public GeometryCollection Triangulate(List<double[]> dtmPointList, List<List<double[]>> dtmLineList)
        {
            ConformingDelaunayTriangulationBuilder triangulationBuilder = new ConformingDelaunayTriangulationBuilder();
            triangulationBuilder.Constraints = MakeMultiLineString(dtmLineList);
            triangulationBuilder.SetSites(this.geomFactory.CreateMultiPoint(MakePointCoordinateSequence(dtmPointList)));
            return triangulationBuilder.GetTriangles(this.geomFactory);
        }

        /// <summary>
        /// A auxiliary function to create a OGC simple feature geometry MultiLineString of the input line data.
        /// </summary>
        /// <param name="dtmLineList">A list of DTM line data (e. g. breaklines).</param>
        /// <returns>A OGC simple feature geometry MultiLineString of the input line data.</returns>
        public MultiLineString MakeMultiLineString(List<List<double[]>> dtmLineList)
        {
            List<LineString> lineCoordList = new List<LineString>();
            foreach (List<double[]> lineCoords in dtmLineList)
            {
                lineCoordList.Add(new LineString (MakePointCoordinateArr(lineCoords)));
            }
            return new MultiLineString(lineCoordList.ToArray());
        }

        /// <summary>
        /// A auxiliary function to create a NetTopologySuite coordinate sequence of the input point data.
        /// </summary>
        /// <param name="dtmPointList"></param>
        /// <returns>A sequence of coordinates of the input point data.</returns>
        public CoordinateSequence MakePointCoordinateSequence(List<double[]> dtmPointList)
        {
            return this.geomFactory.CoordinateSequenceFactory.Create(MakePointCoordinateArr(dtmPointList));
        }
        
        /// <summary>
        /// A auxiliary function to map each point of the input data to a NetTopologySuite CoordinateZ object.
        /// </summary>
        /// <param name="dtmPointList"></param>
        /// <returns>An array of NetTopologySuite CoordinateZ objects representing the input point data.</returns>
        public CoordinateZ[] MakePointCoordinateArr(List<double[]> dtmPointList)
        {
            List<CoordinateZ> coordList = new List<CoordinateZ>();
            foreach (double[] pointCoords in dtmPointList)
            {
                coordList.Add(new CoordinateZ(pointCoords[0], pointCoords[1], pointCoords[2]));
            }
            return coordList.ToArray();
        }
    }
}
