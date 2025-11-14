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
        public static void triangulate(IfcTerrain.Result result, Envelope envelope = null)
        {
            setUp(100d);

            //init builder
            var builder = new TriangleNet.Geometry.Polygon();

            //
            LogWriter.Add(LogType.verbose, "[Triangle.NET] Delauny builder initalized.");

            NetTopologySuite.Geometries.Geometry[] triangles;
            var options = new ConstraintOptions() { Convex = true, ConformingDelaunay = true };
            //add all existing points
            var points = new List<TriangleNet.Geometry.Vertex>();
            int currentIndex = 0; //counter for point IDs
            //add existing points
            for (int i = 0; i < result.pointList.Count; i++)
            {
                points.Add(new Vertex3D(result.pointList[i].X, result.pointList[i].Y, result.pointList[i].Z) { ID = currentIndex++ });
            }

            //switch between different conversion types
            switch (result.currentConversion)
            {
                default:
                case IfcTerrain.DtmConversionType.points:
                case IfcTerrain.DtmConversionType.points_breaklines:
                    builder.Points.AddRange(points); // set points to builder
                    break;

                case IfcTerrain.DtmConversionType.faces:
                case IfcTerrain.DtmConversionType.faces_breaklines:
                    var contours = GetEdgeLoopsFromTriMap(result.triMap, ref points);
                    foreach (var contour in contours)
                    {
                        builder.Add(contour, true);
                    }
                    builder.Points.AddRange(points); // set points to builder

                    options.ConformingDelaunay = false; // disable Delaunay to consistency to existing triangles
                    break;
            }
            // Add Envelope as outer boundary
            if (envelope != null && !envelope.IsNull)
            {
                // Set Envelope as Breakline
                List<TriangleNet.Geometry.Vertex> envPoints = new List<TriangleNet.Geometry.Vertex>([
                new TriangleNet.Geometry.Vertex(envelope.MinX,envelope.MinY){ID = currentIndex++},
                new TriangleNet.Geometry.Vertex(envelope.MinX,envelope.MaxY){ID = currentIndex++},
                new TriangleNet.Geometry.Vertex(envelope.MaxX,envelope.MaxY){ID = currentIndex++},
                new TriangleNet.Geometry.Vertex(envelope.MaxX,envelope.MinY){ID = currentIndex++}]
                );
                builder.Add(new Contour(envPoints, 0));
                builder.Regions.Add(new RegionPointer(envelope.MinX + 0.5, envelope.MinY + 0.5, 1));
            }

            switch (result.currentConversion)
            {
                //case IfcTerrain.DtmConversionType.faces_breaklines: //currently not supported
                case IfcTerrain.DtmConversionType.points_breaklines:
                    points.Clear();
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
                        for (int j = 0; j < result.lines[i].Count - 1; j++)
                        {
                            var Pt1 = new Vertex3D(result.lines[i][j].X, result.lines[i][j].Y, result.lines[i][j].Z);
                            var Pt2 = new Vertex3D(result.lines[i][j + 1].X, result.lines[i][j + 1].Y, result.lines[i][j + 1].Z);
                            //no Envelope or both points are within envelope
                            if (!useEnvelope || (envelope.Contains(Pt1.X, Pt1.Y) & envelope.Contains(Pt2.X, Pt2.Y)))
                            {
                                Pt1.ID = currentIndex++;
                                Pt2.ID = currentIndex++;
                                builder.Add(new Segment(Pt1, Pt2), true);
                            }
                            else // we need to intersect with envelope
                            {
                                LineString line = geometryFactory.CreateLineString(new Coordinate[] {
                                    new Coordinate(Pt1.X, Pt1.Y),
                                    new Coordinate(Pt2.X, Pt2.Y)
                                });
                                var inter = envelopePoly.Intersection(line);
                                if (inter != null && inter is NetTopologySuite.Geometries.LineString ls && !ls.IsEmpty)
                                {
                                    Pt1 = new Vertex3D(ls.StartPoint.X, ls.StartPoint.Y, result.lines[i][j].Z);
                                    Pt2 = new Vertex3D(ls.EndPoint.X, ls.EndPoint.Y, result.lines[i][j + 1].Z);
                                    Pt1.ID = currentIndex++;
                                    Pt2.ID = currentIndex++;
                                    builder.Add(new Segment(Pt1, Pt2), true);
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }
            //Triangulate this one is a processing step (may take some time)
            var mesh = builder.Triangulate(options);
            if (mesh.Vertices.Count == 0)
            {
                LogWriter.Add(LogType.error, "[Triangle.NET] " + "Result mesh has no vertices");
            }

            //create empty list
            CoordinateList cList = new CoordinateList();
            var orderedVerts = mesh.Vertices.OrderBy(v => v.ID >= 0 ? v.ID : int.MaxValue).ToList();
            foreach (var vert in orderedVerts)
            {
                var pt = vert as Vertex3D; //Check for 3D
                if (pt == null || double.IsNaN(pt.Z)) //No Z from input (Vertex was added by Triangulation)
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
                                if (v != null && !double.IsNaN(v.Z))
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
                result.triMap.Add(new triangleMap()
                {
                    triNumber = triangle.ID,
                    triValues = [triangle.GetVertexID(0), triangle.GetVertexID(1), triangle.GetVertexID(2)]
                });
            }
            result.geomStore = new GeometryCollection(triangles);
   
            LogWriter.Add(LogType.verbose, "[Triangulate.NET] Number of unique coordinates: " + cList.Count);

            LogWriter.Add(LogType.info, "Points read: " + cList.Count + "; Triangles read: " + map.Count);

            return;

        }

        /// <summary>
        /// Find all edge loops from triangle map.
        /// 1. finding all edges with only one triangle
        /// 2. create edge-loops
        /// 3. Convert to Contours and remove Contour-points from pointList as these point are added by adding the contour afterwards
        /// </summary>
        /// <param name="triMap"></param>
        /// <param name="pointList"></param>
        /// <returns>Contours </returns>
        private static List<Contour> GetEdgeLoopsFromTriMap(HashSet<triangleMap> triMap, ref List<TriangleNet.Geometry.Vertex> pointList)
        {
            if (triMap == null || pointList == null) return new List<Contour>();

            // 1) build undirected edge counts + collect directed edges
            var undirected = new Dictionary<(int a, int b), int>();
            var directed = new List<(int a, int b)>();

            foreach (var t in triMap)
            {
                var v = t.triValues;
                if (v == null || v.Length < 3) continue;
                for (int i = 0; i < 3; i++)
                {
                    int a = v[i];
                    int b = v[(i + 1) % 3];
                    var key = a < b ? (a, b) : (b, a);
                    undirected.TryGetValue(key, out int cnt);
                    undirected[key] = cnt + 1;
                    directed.Add((a, b));
                }
            }

            // 2) select directed edges that are boundary edges (undirected count == 1)
            var boundaryDirected = directed
                .Where(e =>
                {
                    var key = e.a < e.b ? (e.a, e.b) : (e.b, e.a);
                    return undirected.TryGetValue(key, out int c) && c == 1;
                })
                .ToList();

            if (boundaryDirected.Count == 0) return new List<Contour>(); // nothing to fill

            // 3) build adjacency for following edges to create ordered loops
            var adj = new Dictionary<int, List<int>>();
            foreach (var e in boundaryDirected)
            {
                if (!adj.TryGetValue(e.a, out var lst))
                {
                    lst = new List<int>();
                    adj[e.a] = lst;
                }
                lst.Add(e.b);
            }

            var loops = new List<List<int>>();
            var visitedEdge = new HashSet<(int a, int b)>();

            // build loops by walking directed boundary edges
            foreach (var start in adj.Keys.ToList())
            {
                // if this start has no outgoing edges left, skip
                if (!adj.TryGetValue(start, out var outs) || outs.Count == 0) continue;

                var loop = new List<int>();
                int curr = start;
                int safety = 0;
                do
                {
                    loop.Add(curr);
                    if (!adj.TryGetValue(curr, out var outs2) || outs2.Count == 0) break;
                    int next = outs2[0];
                    // mark edge visited and remove from adjacency to avoid reuse
                    visitedEdge.Add((curr, next));
                    outs2.RemoveAt(0);
                    if (outs2.Count == 0) adj.Remove(curr);
                    curr = next;
                    safety++;
                    if (safety > 100000) break; // safety guard
                } while (curr != start);

                // close loop only if closed
                if (loop.Count >= 3 && curr == start)
                {
                    loops.Add(loop);
                }
            }

            if (loops.Count == 0) return new List<Contour>();

            List<Contour> contours = new List<Contour>();
            int n = 0;
            List<TriangleNet.Geometry.Vertex> tmp_pointList = new List<TriangleNet.Geometry.Vertex>(pointList);
            foreach (var loop in loops)
            {
                var vertList = new List<TriangleNet.Geometry.Vertex>(loop.Count);
                foreach (var pi in loop)
                {
                    if (pi < 0 || pi >= pointList.Count) { vertList.Clear(); break; }
                    vertList.Add(tmp_pointList[pi]);
                    pointList.Remove(tmp_pointList[pi]);
                }
                contours.Add(new Contour(vertList, n++));
            }
            return contours;
        }

    }
}
