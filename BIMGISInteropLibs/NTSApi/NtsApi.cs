using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Import NetTopologySuite class library.
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
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
            NtsGeometryServices.Instance = new NtsGeometryServices(new PrecisionModel(100d));
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
        /// <param name="constraintList">A list of DTM line data (e. g. breaklines).</param>
        /// <returns>A List of triangle data.</returns>
        public List<List<double[]>> MakeTriangleList(List<double[]> dtmPointList, List<double[]> constraintList)
        {
            List<List<double[]>> dtmTriangleList = new List<List<double[]>>();
            GeometryCollection triangles = this.Triangulate(dtmPointList, constraintList);
            foreach(NetTopologySuite.Geometries.Geometry geom in triangles.Geometries)
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
        /// An auxiliary function to triangulate the input point data.
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
        /// An auxiliary function to triangulate the input point and line data.
        /// </summary>
        /// <param name="dtmPointList">A List of DTM point data.</param>
        /// <param name="constraintList">A list of DTM line data (e. g. breaklines).</param>
        /// <returns>A collection of OGC simple feature geometries representing the caclculated triangles. The data set implicitly contains breaklines.</returns>
        public GeometryCollection Triangulate(List<double[]> dtmPointList, List<double[]> constraintList)
        {
            ConformingDelaunayTriangulationBuilder triangulationBuilder = new ConformingDelaunayTriangulationBuilder();
            triangulationBuilder.SetSites(MakeMultiPoint(dtmPointList));
            triangulationBuilder.Constraints = MakeMultiPoint(constraintList);
            return triangulationBuilder.GetTriangles(this.geomFactory);
        }

        /// <summary>
        /// An auxiliary function to create a OGC simple feature geometry MultiPoint of the input point data.
        /// </summary>
        /// <param name="pointList">A list of DTM point data.</param>
        /// <returns>A OGC simple feature geometry MultiPoint of the input point data.</returns>
        public MultiPoint MakeMultiPoint(List<double[]> pointList)
        {
            return this.geomFactory.CreateMultiPoint(MakePointCoordinateSequence(pointList));
        }

        /// <summary>
        /// An auxiliary function to create a NetTopologySuite coordinate sequence of the input point data.
        /// </summary>
        /// <param name="pointList"></param>
        /// <returns>A sequence of coordinates of the input point data.</returns>
        public CoordinateSequence MakePointCoordinateSequence(List<double[]> pointList)
        {
            return this.geomFactory.CoordinateSequenceFactory.Create(MakePointCoordinateArr(pointList));
        }
        
        /// <summary>
        /// An auxiliary function to map each point of the input data to a NetTopologySuite CoordinateZ object.
        /// </summary>
        /// <param name="pointList"></param>
        /// <returns>An array of NetTopologySuite CoordinateZ objects representing the input point data. Each CoordinateZ object is unique.</returns>
        public CoordinateZ[] MakePointCoordinateArr(List<double[]> pointList)
        {
            bool addPoint;
            List<CoordinateZ> coordList = new List<CoordinateZ>();
            foreach (double[] pointCoords in pointList)
            {
                addPoint = true;
                CoordinateZ pointCoordZ = new CoordinateZ(pointCoords[0], pointCoords[1], pointCoords[2]);
                foreach (CoordinateZ coordZ in coordList)
                {
                    if (coordZ.Equals3D(pointCoordZ))
                    {
                        addPoint = false;
                        break;
                    }
                }
                if (addPoint)
                {
                    coordList.Add(pointCoordZ);
                }
            }
            return coordList.ToArray();
        }
    }
}
