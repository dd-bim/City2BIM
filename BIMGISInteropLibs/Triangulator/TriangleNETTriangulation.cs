using BIMGISInteropLibs.Logging;
using BIMGISInteropLibs.Triangulator;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.QuadEdge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;

//using NetTopologySuite.Triangulate.Tri;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log message
namespace BIMGISInteropLibs.Triangulator
{
    internal class TriangleNETTriangulation
    {
        /// <summary>
        /// geometry factory need to use get functions (e.g. -> get triangles)
        /// </summary>
        private static GeometryFactory geometryFactory { get; set; }

        /// <summary>
        /// set up new instance for processing
        /// </summary>
        /// <param name="precision"></param>
        public static void setUp(double precision)
        {
            geometryFactory = new GeometryFactory(new PrecisionModel(), 25833);
            /*
            NTS.NtsGeometryServices.Instance = new NTS.NtsGeometryServices(
                new PrecisionModel(precision));
            geometryFactory = NTS.NtsGeometryServices.Instance.CreateGeometryFactory();
            */
        }

        public class Vertex3D : TriangleNet.Geometry.Vertex
        {
            public double Z { get; set; }
            public Vertex3D(double x, double y, double z) : base(x, y)
            {
                Z = z;
            }
            public Vertex3D(TriangleNet.Geometry.Vertex vertex, double z) : base(vertex.X, vertex.Y)
            {
                this.ID = vertex.ID;
                this.Label = vertex.Label;
                Z = z;
            }
            public static double Dist2D(TriangleNet.Geometry.Vertex v1, TriangleNet.Geometry.Vertex v2)
            {
                return Math.Sqrt(Math.Pow(v1.X - v2.X, 2.0) + Math.Pow(v1.Y - v2.Y, 2));
            }
        }

        /// <summary>
        /// execute delaunay triangulation
        /// </summary>
        public static void triangulate(IfcTerrain.Result result)
        {
            setUp(100d);

            //init builder
            var builder = new TriangleNet.Geometry.Polygon();

            //
            LogWriter.Add(LogType.verbose, "[Triangle.NET] Delauny builder initalized.");

            //create geometry collections
            GeometryCollection faces;
            GeometryCollection breaklines;
            NetTopologySuite.Geometries.Geometry[] triangles;
            var points = new HashSet<Vertex3D>();
            //switch between different conversion types
            switch (result.currentConversion)
            {
                default:
                    for (int i = 0; i < result.pointList.Count; i++)
                    {
                        points.Add(new Vertex3D((float)result.pointList[i].X, (float)result.pointList[i].Y, (float)result.pointList[i].Z));
                    }
                    //set points
                    builder.Points.AddRange(points);
                    break;

                case IfcTerrain.DtmConversionType.faces:
                    //faces = new GeometryCollection(result.triangleList.ToArray());
                    //builder.SetPoints(faces);
                    break;

                case IfcTerrain.DtmConversionType.faces_breaklines:

                    //faces = new GeometryCollection(result.triangleList.ToArray());
                    //builder.SetSites(faces);

                    //breaklines = new GeometryCollection(result.lines.ToArray());
                    //builder.Constraints = breaklines;
                    break;

                case IfcTerrain.DtmConversionType.points_breaklines:
                    for (int i = 0; i < result.pointList.Count; i++)
                    {
                        points.Add(new Vertex3D((float)result.pointList[i].X, (float)result.pointList[i].Y, (float)result.pointList[i].Z));
                    }
                    //Add Breaklines
                    for (int i = 0; i < result.lines.Count; i++)
                    {
                        for (int j = 0; j < result.lines[i].Count-1; j++)
                        {
                            var Pt1 = new Vertex3D((float)result.lines[i][j].X, (float)result.lines[i][j].Y, (float)result.lines[i][j].Z);
                            var Pt2 = new Vertex3D((float)result.lines[i][j+1].X, (float)result.lines[i][j+1].Y, (float)result.lines[i][j + 1].Z);
                            points.Add(Pt1);
                            points.Add(Pt2);
                            builder.Segments.Add(new Segment(Pt1,Pt2));
                        }
                    }
                    //set points
                    builder.Points.AddRange(points);
                    break;
            }

            //Triangulate this one is a processing step (may take some time)
            var options = new ConstraintOptions() { Convex = true,ConformingDelaunay = true };
            var mesh = builder.Triangulate(options);
            if (mesh.Vertices.Count == 0)
            {
                LogWriter.Add(LogType.error, "[Triangle.NET] " + "Result mesh has no vertices");
            }

            //create empty list
            CoordinateList cList = new CoordinateList();

            //loop through every coord
            foreach (var vert in mesh.Vertices)
            {
                var pt = vert as Vertex3D; //Check for 3D
                if (pt == null) //No Z from input (Vertex was added by Triangulation)
                {
                    double newZ = double.NaN;
                    if (result.lines is not null)  // Try to interpolate from nearest Breakline
                    {
                        //loop through each line
                        double minDist = double.MaxValue;
                        LineString nearestLine = null;
                        foreach (var line in result.lines)
                        {
                            //check point is on line
                            double dist = line.Distance(new NetTopologySuite.Geometries.Point(vert.X, vert.Y));
                            if (dist < minDist && dist < 0.5) //using 0.5m we enshure only not to use very far breakline
                            {
                                minDist = dist;
                                nearestLine = line;
                            }
                        }
                        if (nearestLine != null)
                        {
                            //Get nearest Segment of nearest Line
                            for (int i = 0; i < nearestLine.NumPoints - 1; i++)
                            {
                                var segment = new LineSegment(nearestLine.GetCoordinateN(i), nearestLine.GetCoordinateN(i + 1));
                                double distance = segment.Distance(nearestLine.Coordinate);

                                if (distance <= minDist)
                                {
                                    newZ = NetTopologySuite.Triangulate.QuadEdge.Vertex.InterpolateZ(new Coordinate(vert.X, vert.Y), segment.P0, segment.P1);
                                }
                            }
                        }
                    }
                    // fallback if no breaklines set or not close enough. Searching for connected points with Z and calculating distance weightened mean.
                    if (double.IsNaN(newZ)) 
                    {
                        var trianglesWithVertex = mesh.Triangles.Where(triangle => 
                            triangle.GetVertex(0).ID == vert.ID || 
                            triangle.GetVertex(1).ID == vert.ID || 
                            triangle.GetVertex(2).ID == vert.ID)
                            .ToList();
                        double SumDistancesToConnectedVerticesWithZ = 0;
                        double SumHeightsOfConnectedVerticesWithZ = 0;
                        foreach (var triangle in trianglesWithVertex)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                var v = triangle.GetVertex(i) as Vertex3D;
                                if (v != null)
                                {
                                    double dist = Vertex3D.Dist2D(v, vert);
                                    SumHeightsOfConnectedVerticesWithZ += v.Z * dist;
                                    SumDistancesToConnectedVerticesWithZ += dist;
                                }
                            }
                        }
                        newZ = SumDistancesToConnectedVerticesWithZ > 0 ? SumHeightsOfConnectedVerticesWithZ / SumDistancesToConnectedVerticesWithZ : 0.0;
                    }
                    cList.Add(new CoordinateZ(vert.X, vert.Y, newZ));
                }
                else
                {
                    cList.Add(new CoordinateZ(vert.X, vert.Y, pt.Z));
                }
            }
            //set coord list to result class
            result.coordinateList = cList;

            var map = new HashSet<triangleMap>();
            triangles = new NetTopologySuite.Geometries.Geometry[mesh.Triangles.Count];
            int num = 0;
            foreach (var triangle in mesh.Triangles)
            {
                triangles[num] = geometryFactory.CreatePolygon([
                    new CoordinateZ(triangle.GetVertex(0).X, triangle.GetVertex(0).Y, triangle.GetVertex(0) as Vertex3D != null ? ((Vertex3D)triangle.GetVertex(0)).Z : cList[triangle.GetVertexID(0)].Z),
                    new CoordinateZ(triangle.GetVertex(1).X, triangle.GetVertex(1).Y, triangle.GetVertex(1) as Vertex3D != null ? ((Vertex3D)triangle.GetVertex(1)).Z : cList[triangle.GetVertexID(1)].Z),
                    new CoordinateZ(triangle.GetVertex(2).X, triangle.GetVertex(2).Y, triangle.GetVertex(2) as Vertex3D != null ? ((Vertex3D)triangle.GetVertex(2)).Z : cList[triangle.GetVertexID(2)].Z),
                    new CoordinateZ(triangle.GetVertex(0).X, triangle.GetVertex(0).Y, triangle.GetVertex(0) as Vertex3D != null ? ((Vertex3D)triangle.GetVertex(0)).Z : cList[triangle.GetVertexID(0)].Z)
                ]);
                num++;
                map.Add(new triangleMap()
                {
                    triNumber = triangle.ID,
                    triValues = [triangle.GetVertexID(0), triangle.GetVertexID(1), triangle.GetVertexID(2)]
                });
            }
            result.geomStore = new GeometryCollection(triangles);
            result.triMap = map;

            //
            LogWriter.Add(LogType.verbose, "[Triangulate.NET] Number of unique coordinates: " + cList.Count);

            LogWriter.Add(LogType.info, "Points read: " + cList.Count + "; Triangles read: " + map.Count);

            return;

        }
    }

}
