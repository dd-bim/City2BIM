using BIMGISInteropLibs.IfcTerrain;
using BIMGISInteropLibs.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using OSGeo.OGR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using static BIMGISInteropLibs.Triangulator.TriangleNETTriangulation;
using static BIMGISInteropLibs.Triangulator.TriangulationHelpers;

namespace BIMGISInteropLibs.Triangulator
{
    public class triangleMap
    {
        public int triNumber { get; set; }
        public int[] triValues { get; set; }
    }

    internal class TriangulationHelpers
    {
        /// <summary>
        /// Label to identify vertex types (source-types)
        /// </summary>
        public enum VertexLabel
        {
            Steiner = 0,
            Input = 1,
            Contour = 2,
            Breakline = 3,
            Envelope = 4
        }

        internal static List<TriangleNet.Geometry.Vertex> GetIsolatedPoints(HashSet<triangleMap> triMap, ref List<TriangleNet.Geometry.Vertex> pointList)
        {
            if (triMap == null || pointList == null) return new List<TriangleNet.Geometry.Vertex>();
            // collect used vertex IDs
            var usedVertexIds = new HashSet<int>();
            foreach (var t in triMap)
            {
                var v = t.triValues;
                if (v == null || v.Length < 3) continue;
                for (int i = 0; i < 3; i++)
                {
                    usedVertexIds.Add(v[i]);
                }
            }
            // identify isolated points: indices not used in any triangle
            var isolatedPoints = new List<TriangleNet.Geometry.Vertex>();
            var tmp_pointList = new List<TriangleNet.Geometry.Vertex>(pointList);
            for (int i = 0; i < tmp_pointList.Count; i++)
            {
                var pt = tmp_pointList[i];
                if (pt == null) continue;

                // Defensive: check if ID is valid and not used in triMap; if not used, remove from pointList and add to isolatedPoints
                if (usedVertexIds.Contains(pt.ID)) continue;

                // Save remove per ID (first find Index)
                int idx = pointList.FindIndex(x => x != null && x.ID == pt.ID);
                if (idx >= 0)
                {
                    isolatedPoints.Add(pointList[idx]);
                    pointList.RemoveAt(idx);
                }
            }
            return isolatedPoints;
        }
        /// <summary>
        /// Find all edge loops from triangle map.
        /// 1. finding all edges with only one triangle
        /// 2. create edge-loops
        /// 3. Convert to Contours and remove Contour-points from pointList as these point are added by adding the contour afterwards
        /// </summary>
        /// <param name="triMap"></param>
        /// <param name="pointList">pointList by ref, as ContourPoints are removed from pointList (preventing duplicates, when contours are added)</param>
        /// <returns>Contours</returns>
        public static List<Contour> GetEdgeLoopsFromTriMap(HashSet<triangleMap> triMap, ref List<TriangleNet.Geometry.Vertex> pointList)
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
                    var point = tmp_pointList.Find(p => p.ID == pi);
                    point.Label = (int)VertexLabel.Contour;
                    vertList.Add(point);
                    pointList.Remove(point);
                }
                contours.Add(new Contour(vertList, n++));
            }

            return contours;
        }

        /// <summary>
        /// Interpolates the Z value at point p on the segment defined by points a and b.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a">Start point of segment</param>
        /// <param name="b">End point of segement</param>
        /// <returns></returns>
        public static double InterpolateZOnSegment(Coordinate p, Coordinate a, Coordinate b)
        {
            if (p == null || a == null || b == null) return double.NaN;

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double denom = dx * dx + dy * dy;
            if (denom <= double.Epsilon)
            {
                if (!double.IsNaN(a.Z)) return a.Z;
                if (!double.IsNaN(b.Z)) return b.Z;
                return double.NaN;
            }

            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / denom;
            t = Math.Clamp(t, 0.0, 1.0);

            double z0 = a.Z;
            double z1 = b.Z;
            if (!double.IsNaN(z0) && !double.IsNaN(z1))
            {
                return z0 + t * (z1 - z0);
            }
            if (!double.IsNaN(z0)) return z0;
            if (!double.IsNaN(z1)) return z1;
            return double.NaN;
        }

        /// <summary>
        /// Get or compute Z value for given vertex.
        /// </summary>
        /// <param name="vert">Vertex to get Z for</param>
        /// <param name="mesh">Surrounding mesh to interpolate Z (fallback for breakline interpolation)</param>
        /// <param name="breaklines">Breaklines as prior source for interpolation</param>
        /// <returns></returns>
        public static double ComputeVertexZ(TriangleNet.Geometry.Vertex vert, IMesh mesh, IList<NetTopologySuite.Geometries.LineString> breaklines)
        {
            // if vert is already 3D with Z -> return
            if (vert is Vertex3D v3 && !double.IsNaN(v3.Z)) return v3.Z;

            // try nearest breakline (planar distance), prefer close (< 0.5)
            if (breaklines != null && breaklines.Count > 0)
            {
                var ptGeom = new NetTopologySuite.Geometries.Point(vert.X, vert.Y);
                double minDist = double.MaxValue;
                NetTopologySuite.Geometries.LineString nearest = null;
                foreach (var line in breaklines)
                {
                    double d = line.Distance(ptGeom);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = line;
                    }
                }
                if (nearest != null && minDist < 0.5)
                {
                    for (int k = 0; k < nearest.NumPoints - 1; k++)
                    {
                        var a = nearest.GetCoordinateN(k);
                        var b = nearest.GetCoordinateN(k + 1);
                        var z = InterpolateZOnSegment(new Coordinate(vert.X, vert.Y), a, b);
                        if (!double.IsNaN(z)) return z;
                    }
                }
            }

            // fallback: average Z of connected vertices (preserves original weighting behavior)
            double sumWeights = 0.0;
            double sumHeights = 0.0;
            foreach (var triangle in mesh.Triangles)
            {
                // check if triangle contains the vertex
                if (triangle.GetVertexID(0) == vert.ID || triangle.GetVertexID(1) == vert.ID || triangle.GetVertexID(2) == vert.ID)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (triangle.GetVertex(i) is Vertex3D vv && !double.IsNaN(vv.Z))
                        {
                            double dist = Vertex3D.Dist2D(vv, vert);
                            double w = dist == 0.0 ? 1.0 : 1 / dist;
                            sumHeights += vv.Z * w;
                            sumWeights += w;
                        }
                    }
                }
            }
            return sumWeights > 0 ? sumHeights / sumWeights : 0.0;
        }

        /// <summary>
        /// Simple plane fitting through given 3D points (no LSQ).
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Plane3D FitPlane(IEnumerable<Vertex3D> points)
        {
            var pts = points.ToList();
            if (pts.Count < 3) throw new ArgumentException("need more than 2 points for plane fit");
            if (pts.Any(p => double.IsNaN(p.X) || double.IsNaN(p.Y) || double.IsNaN(p.Z))) throw new ArgumentException("Point for plane fir contains NaN value");
            var normals = new List<(double nx, double ny, double nz)>();

            // For all triplets of points, compute normal vector
            for (int i = 0; i < pts.Count - 2; i++)
            {
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = pts[i + 2];

                // Vectors
                double ux = p2.X - p1.X;
                double uy = p2.Y - p1.Y;
                double uz = p2.Z - p1.Z;

                double vx = p3.X - p1.X;
                double vy = p3.Y - p1.Y;
                double vz = p3.Z - p1.Z;

                // Cross product
                double nx = uy * vz - uz * vy;
                double ny = uz * vx - ux * vz;
                double nz = ux * vy - uy * vx;

                normals.Add((nx, ny, nz));
            }

            // Average normal vector
            double a = normals.Average(n => n.nx);
            double b = normals.Average(n => n.ny);
            double c = normals.Average(n => n.nz);

            // Center point
            double cx = pts.Average(p => p.X);
            double cy = pts.Average(p => p.Y);
            double cz = pts.Average(p => p.Z);

            return new Plane3D(new Vector3D(a, b, c), new CoordinateZ(cx, cy, cz));
        }

        /// <summary>
        /// Filter points by Z influence compared to fitted plane through neighbours.
        /// point is removed if its distance to the plane is <= filterZ.
        /// </summary>
        /// <param name="result">In- Output of points to filter</param>
        /// <param name="mesh">Underlying mesh for getting connected neighbours</param>
        /// <param name="filterZ">Threshold for removing points with less influence</param>
        /// <returns></returns>
        public static bool FilterByZInfluence(Result result, IMesh mesh, double filterZ)
        {
            if (result == null || mesh == null) return false;

            var meshVertices = mesh.Vertices.OfType<Vertex3D>().ToList();
            var meshEdges = mesh.Edges.ToList();

            // Fast Lookup ID -> Vertex
            var vertexById = meshVertices.Where(v => v != null).ToDictionary(v => v.ID, v => v);

            // build adjacency list (VertexID -> neighbour IDs)
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

            var maxThreads = Math.Max(1, Environment.ProcessorCount - 1);
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

                    var connectedVerts = new List<Vertex3D>(neighIds.Count);
                    foreach (var nid in neighIds)
                    {
                        if (vertexById.TryGetValue(nid, out var otherVert) && otherVert is Vertex3D vv)
                        {
                            connectedVerts.Add(vv);
                        }
                    }
                    if (connectedVerts.Count < 3) continue;

                    var plane = FitPlane(connectedVerts);
                    double dist = plane.OrientedDistance(new CoordinateZ(vert3D.X, vert3D.Y, vert3D.Z));
                    if (Math.Abs(dist) <= filterZ)
                    {
                        if (vert3D.ID >= 0 && vert3D.ID < result.pointList.Count)
                        {
                            toRemoveIndices.Add(vert3D.ID);
                        }
                    }
                }
            });

            if (!toRemoveIndices.IsEmpty)
            {
                // call index-based removal — deterministic remapping of triMap
                result.RemovePoints(toRemoveIndices.Distinct().ToList());
                return true;
            }
            return false;
        }
    }
}
