using BIMGISInteropLibs.Logging;
using BIMGISInteropLibs.Triangulator;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.QuadEdge;
using System;
using System.Collections.Concurrent;
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
using static BIMGISInteropLibs.Triangulator.TriangulationHelpers;
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
            geometryFactory = new GeometryFactory(new PrecisionModel(precision), 25833);
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
        /// <param name="result">In- Output</param>
        /// <param name="filterZ">Treshold for removing points, with influence to Height is lower than value</param>
        /// <param name="envelope">Boundingbox to cut Inputdata</param>
        public static void triangulate(IfcTerrain.Result result, double filterZ = -1.0, Envelope envelope = null)
        {
            setUp(100d);

            //init builder
            var builder = new TriangleNet.Geometry.Polygon();
            var options = new ConstraintOptions() { Convex = true, ConformingDelaunay = true, SegmentSplitting = 1 };
            LogWriter.Add(LogType.verbose, "[Triangle.NET] Delauny builder initalized.");
  
            //get all existing points
            var points = new List<TriangleNet.Geometry.Vertex>();
            int currentIndex = 0; //counter for point IDs
            //add existing points
            for (int i = 0; i < result.pointList.Count; i++)
            {
                if (double.IsNaN(result.pointList[i].Z))
                {
                    points.Add(new TriangleNet.Geometry.Vertex(result.pointList[i].X, result.pointList[i].Y) { ID = currentIndex++, Label = (int)VertexLabel.Input });
                }
                else
                {
                    points.Add(new Vertex3D(result.pointList[i].X, result.pointList[i].Y, result.pointList[i].Z) { ID = currentIndex++, Label = (int)VertexLabel.Input });
                }
            }
            // switch between different conversion types
            switch (result.currentConversion)
            {
                default:
                case IfcTerrain.DtmConversionType.points:
                case IfcTerrain.DtmConversionType.points_breaklines:
                    builder.Points.AddRange(points); // set points to builder
                    points.Clear(); //clear points as they are added now to builder
                    break;

                case IfcTerrain.DtmConversionType.faces:
                case IfcTerrain.DtmConversionType.faces_breaklines:
                    var isolatedPoints = GetIsolatedPoints(result.triMap, ref points);
                    builder.Points.AddRange(isolatedPoints); // add isolated (unconnected) points for triangulation
                    var contours = GetEdgeLoopsFromTriMap(result.triMap, ref points);
                    HashSet<TriangleNet.Geometry.Vertex> contourVertices = new HashSet<TriangleNet.Geometry.Vertex>();
                    foreach (var contour in contours)
                    {
                        builder.Holes.Add(contour.FindInteriorPoint());//We add as hole to prevent triangulation inside (keeping existing triangles)
                        contour.Points.ForEach(v => contourVertices.Add(v)); // keep track of contour vertices to avoid duplicates with isolated points
                        builder.Segments.AddRange(contour.GetSegments());
                    }
                    builder.Points.AddRange(contourVertices); // add contour points to builder
                    break;
            }
            // Add Envelope as outer boundary
            if (envelope != null && !envelope.IsNull)
            {
                // Set Envelope as Breakline
                List<TriangleNet.Geometry.Vertex> envPoints = new List<TriangleNet.Geometry.Vertex>([
                new TriangleNet.Geometry.Vertex(envelope.MinX,envelope.MinY){ID = currentIndex++, Label = (int)VertexLabel.Envelope},
                new TriangleNet.Geometry.Vertex(envelope.MinX,envelope.MaxY){ID = currentIndex++, Label = (int)VertexLabel.Envelope},
                new TriangleNet.Geometry.Vertex(envelope.MaxX,envelope.MaxY){ID = currentIndex++, Label = (int)VertexLabel.Envelope},
                new TriangleNet.Geometry.Vertex(envelope.MaxX,envelope.MinY){ID = currentIndex++, Label = (int)VertexLabel.Envelope}]
                );
                builder.Add(new Contour(envPoints, 0));
            }

            // Add Breaklines if needed
            if (result.currentConversion == IfcTerrain.DtmConversionType.faces_breaklines
                || result.currentConversion == IfcTerrain.DtmConversionType.points_breaklines)
            {
                bool useEnvelope = envelope != null && !envelope.IsNull;
                NetTopologySuite.Geometries.Polygon envelopePoly = null;
                if (useEnvelope)
                {
                    envelopePoly = geometryFactory.CreatePolygon(new Coordinate[] {
                    new Coordinate(envelope.MinX, envelope.MinY),
                    new Coordinate(envelope.MinX, envelope.MaxY),
                    new Coordinate(envelope.MaxX, envelope.MaxY),
                    new Coordinate(envelope.MaxX, envelope.MinY),
                    new Coordinate(envelope.MinX, envelope.MinY)
                    });
                }
                for (int i = 0; i < result.lines.Count; i++)
                {
                    // for all segments in breakline
                    for (int j = 0; j < result.lines[i].Count - 1; j++)
                    {
                        var Pt1 = new Vertex3D(result.lines[i][j].X, result.lines[i][j].Y, result.lines[i][j].Z)
                        {
                            ID = currentIndex++,
                            Label = (int)VertexLabel.Breakline
                        };
                        var Pt2 = new Vertex3D(result.lines[i][j + 1].X, result.lines[i][j + 1].Y, result.lines[i][j + 1].Z)
                        {
                            ID = currentIndex++,
                            Label = (int)VertexLabel.Breakline
                        }; 
                        // Envelope and clipping
                        if (useEnvelope && !(envelope.Contains(Pt1.X, Pt1.Y) && envelope.Contains(Pt2.X, Pt2.Y)))
                        {
                            var line = geometryFactory.CreateLineString(new[]
                            {
                                new CoordinateZ(Pt1.X, Pt1.Y, Pt1.Z),
                                new CoordinateZ(Pt2.X, Pt2.Y, Pt2.Z)
                            });
                            var inter = envelopePoly.Intersection(line);
                            if (inter is NetTopologySuite.Geometries.LineString ls && !ls.IsEmpty)
                            {
                                Pt1.X = ls.StartPoint.X;
                                Pt1.Y = ls.StartPoint.Y;
                                Pt1.Z = InterpolateZOnSegment(ls.StartPoint.Coordinate, line.StartPoint.Coordinate, line.EndPoint.Coordinate);
                                Pt2.X = ls.EndPoint.X;
                                Pt2.Y = ls.EndPoint.Y;
                                Pt2.Z = InterpolateZOnSegment(ls.EndPoint.Coordinate, line.StartPoint.Coordinate, line.EndPoint.Coordinate);
                            }
                            else
                            { 
                                currentIndex -= 2;
                                continue; // Segment complete outside envelope
                            }
                        }
                        builder.Add(new Segment(Pt1, Pt2), true);
                    }
                }
            }

            // Triangulate
            var mesh = builder.Triangulate(options);
            if (mesh.Vertices.Count == 0)
            {
                LogWriter.Add(LogType.error, "[Triangle.NET] " + "Result mesh has no vertices");
            }

            // Filter
            if (filterZ >= 0.0 && FilterByZInfluence(result, mesh, filterZ))
            {
                LogWriter.Add(LogType.info, "[Triangle.NET] " + "Filtered " + (mesh.Vertices.Count-result.pointList.Count) + " Vertices");
                triangulate(result, -1.0, envelope); // re-triangulate if filter applied
                return;
            }

            // process results
            var mergeList = mesh.Vertices.ToList(); // add excluded points. "points" is empty for points-only triangulation (as all points were used), for faces triangulation it contains inner face points (not part of contours)
            mergeList.AddRange(points);

            CoordinateList cList = new CoordinateList();
            var orderedVerts = mergeList.OrderBy(v => v.ID >= 0 ? v.ID : int.MaxValue);
            foreach (var vert in orderedVerts)
            {
                double z = ComputeVertexZ(vert, mesh, result.lines);
                cList.Add(new CoordinateZ(vert.X, vert.Y, z));
            }
            result.coordinateList = cList;

            var polygons = new List<NetTopologySuite.Geometries.Polygon>();
            foreach (var tri in mesh.Triangles)
            {
                if(cList.Count <= Math.Max(tri.GetVertexID(0), Math.Max(tri.GetVertexID(1), tri.GetVertexID(2))))
                {
                    LogWriter.Add(LogType.error, "[Triangle.NET] Triangle vertex index out of range: " + tri.GetVertexID(0) + ", " + tri.GetVertexID(1) + ", " + tri.GetVertexID(2));
                    continue;
                }
                LinearRing linearRing = new LinearRing([
                    cList[tri.GetVertexID(0)],
                    cList[tri.GetVertexID(1)],
                    cList[tri.GetVertexID(2)],
                    cList[tri.GetVertexID(0)]
                    ]);
                polygons.Add(new NetTopologySuite.Geometries.Polygon(linearRing));

                result.triMap.Add(new triangleMap()
                {
                    triNumber = tri.ID,
                    triValues = [tri.GetVertexID(0), tri.GetVertexID(1), tri.GetVertexID(2)]
                });
            }
            result.geomStore = new NetTopologySuite.Geometries.GeometryCollection(polygons.ToArray());
   
            LogWriter.Add(LogType.verbose, "[Triangle.NET] Number of unique coordinates: " + cList.Count);

            LogWriter.Add(LogType.info, "Points read: " + cList.Count + "; Triangles read: " + result.triMap.Count);

            return;

        }
    }
}
