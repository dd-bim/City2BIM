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
                points.Add(new Vertex3D(result.pointList[i].X, result.pointList[i].Y, result.pointList[i].Z) { ID = currentIndex++, Label = (int)VertexLabel.Input });
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
                    var contours = GetEdgeLoopsFromTriMap(result.triMap, ref points);
                    foreach (var contour in contours)
                    {
                        builder.Add(contour, true); //We add as hole to prevent triangulation inside (keeping existing triangles)
                    }
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

            // Filter (parallel, chunked + adjacency)
            if (filterZ >= 0.0)
            {
                var tmpPointList = result.pointList.ToList();
                var meshVertices = mesh.Vertices.OfType<Vertex3D>().ToList();
                var meshEdges = mesh.Edges.ToList();

                // Fast Lookup ID -> Vertex
                var vertexById = meshVertices.Where(v => v != null).ToDictionary(v => v.ID, v => v);

                // build Adjazenzlist (VertexID -> List of neighbour-IDs)
                var adjacency = new Dictionary<int, List<int>>(meshVertices.Count);
                foreach (var e in meshEdges)
                {
                    if (!adjacency.TryGetValue(e.P0, out var list0))
                    {
                        list0 = new List<int>();
                        adjacency[e.P0] = list0;
                    }
                    list0.Add(e.P1);

                    if (!adjacency.TryGetValue(e.P1, out var list1))
                    {
                        list1 = new List<int>();
                        adjacency[e.P1] = list1;
                    }
                    list1.Add(e.P0);
                }

                var toRemoveIndices = new ConcurrentBag<int>();

                var maxThreads = Math.Max(1, Environment.ProcessorCount - 1); // default number CPU-Cores - 1
                var po = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

                int chunkSize = 64;
                var rangePartitioner = Partitioner.Create(0, meshVertices.Count, chunkSize);

                Parallel.ForEach(rangePartitioner, po, range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var vert3D = meshVertices[i];
                        if (vert3D is null) continue;
                        if (vert3D.Label != (int)VertexLabel.Input) continue;
                        if (vert3D.ID < 0) continue;

                        if (!adjacency.TryGetValue(vert3D.ID, out var neighIds) || neighIds.Count < 3) continue;

                        // collect neighbours als Vertex3D (fast Lookup)
                        var connectedVerts = new List<Vertex3D>(neighIds.Count);
                        foreach (var nid in neighIds)
                {
                            if (vertexById.TryGetValue(nid, out var otherVert) && otherVert is Vertex3D vv)
                    {
                                connectedVerts.Add(vv);
                            }
                    }
                    if (connectedVerts.Count() < 3) continue;

                    var plane = FitPlane(connectedVerts);
                    double dist = plane.OrientedDistance(new CoordinateZ(vert3D.X, vert3D.Y, vert3D.Z));
                    if (Math.Abs(dist) <= filterZ)
                    {
                            if (vert3D.ID >= 0 && vert3D.ID < tmpPointList.Count)
                            {
                                toRemoveIndices.Add(vert3D.ID);
                            }
                    }
                }
                });

                if (!toRemoveIndices.IsEmpty)
                {
                    var removeSet = new HashSet<int>(toRemoveIndices); // deduplicate
                    result.pointList = tmpPointList.Where((p, idx) => !removeSet.Contains(idx)).ToList();
                    LogWriter.Add(LogType.info, "[Triangle.NET] Filtered points from " + tmpPointList.Count + " to " + result.pointList.Count);
                    triangulate(result, -1.0, envelope); // re-triangulate if filter applied
                    return;
                }
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
   
            LogWriter.Add(LogType.verbose, "[Triangulate.NET] Number of unique coordinates: " + cList.Count);

            LogWriter.Add(LogType.info, "Points read: " + cList.Count + "; Triangles read: " + result.triMap.Count);

            return;

        }
    }
}
