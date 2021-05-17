using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Representation.Geometry.Elementary;

namespace BimGisCad.Collections
{
    /// <summary>
    /// Tuple zweier Indexwerte
    /// </summary>
    public struct TupleIdx : IEquatable<TupleIdx>
    {
        /// <summary>
        /// Index 1
        /// </summary>
        public readonly int Idx1;
        /// <summary>
        /// Index 2
        /// </summary>
        public readonly int Idx2;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="idx1">Index 1</param>
        /// <param name="idx2">Index 2</param>
        public TupleIdx(int idx1, int idx2)
        {
            this.Idx1 = idx1;
            this.Idx2 = idx2;
        }

        /// <summary>
        /// Vergleich anhand der sortierten Indizes
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is TupleIdx && this.Equals((TupleIdx)obj);
        /// <summary>
        /// Vergleich anhand der sortierten Indizes
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TupleIdx other) => this.Idx1 == other.Idx1 && this.Idx2 == other.Idx2;

        /// <summary>
        /// Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => (this.Idx1 * 31) + this.Idx2;

        /// <summary>
        /// Vergleich
        /// </summary>
        /// <param name="tup1"></param>
        /// <param name="tup2"></param>
        /// <returns></returns>
        public static bool operator ==(TupleIdx tup1, TupleIdx tup2) => tup1.Equals(tup2);

        /// <summary>
        /// Vergleich (Ungleich)
        /// </summary>
        /// <param name="tup1"></param>
        /// <param name="tup2"></param>
        /// <returns></returns>
        public static bool operator !=(TupleIdx tup1, TupleIdx tup2) => !(tup1 == tup2);

        /// <summary>
        /// Gibt neues Tuple mit getauschten Indizes zurück
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static TupleIdx Flipped(TupleIdx edge) => new TupleIdx(edge.Idx2, edge.Idx1);
    }

    abstract class Node
    {
        public int Index { get; internal set; }

        public abstract int Add(IReadOnlyList<Point3> points, double minDistSq, Point3 point);
    }

    abstract class Node2 : Node
    {
        protected static double distSq(Point3 p1, Point3 p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return (dx * dx) + (dy * dy);
        }

    }

    class Node2X : Node2
    {
        private Node2Y left = null;
        private Node2Y right = null;

        public override int Add(IReadOnlyList<Point3> points, double minDistSq, Point3 point)
        {
            var p = points[this.Index];
            if(distSq(p, point) < minDistSq)
            {
                return this.Index;
            }
            if(p.X.CompareTo(point.X) <= 0)
            {
                if(this.left != null)
                {
                    return this.left.Add(points, minDistSq, point);
                }
                else
                {
                    this.left = new Node2Y
                    {
                        Index = points.Count
                    };
                    return this.left.Index;
                }
            }
            if(this.right != null)
            {
                return this.right.Add(points, minDistSq, point);
            }
            else
            {
                this.right = new Node2Y
                {
                    Index = points.Count
                };
                return this.right.Index;
            }
        }
    }

    class Node2Y : Node2
    {
        private Node2X left = null;
        private Node2X right = null;

        public override int Add(IReadOnlyList<Point3> points, double minDistSq, Point3 point)
        {
            var p = points[this.Index];
            if(distSq(p, point) < minDistSq)
            {
                return this.Index;
            }
            if(p.Y.CompareTo(point.Y) <= 0)
            {
                if(this.left != null)
                {
                    return this.left.Add(points, minDistSq, point);
                }
                else
                {
                    this.left = new Node2X
                    {
                        Index = points.Count
                    };
                    return this.left.Index;
                }
            }
            if(this.right != null)
            {
                return this.right.Add(points, minDistSq, point);
            }
            else
            {
                this.right = new Node2X
                {
                    Index = points.Count
                };
                return this.right.Index;
            }
        }
    }

    abstract class Node3 : Node
    {
        protected static double distSq(Point3 p1, Point3 p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dz = p2.Z - p1.Z;
            return (dx * dx) + (dy * dy) + (dz * dz);
        }

    }

    class Node3X : Node3
    {
        private Node3Y left = null;
        private Node3Y right = null;

        public override int Add(IReadOnlyList<Point3> points, double minDistSq, Point3 point)
        {
            var p = points[this.Index];
            if(distSq(p, point) < minDistSq)
            {
                return this.Index;
            }
            if(p.X.CompareTo(point.X) <= 0)
            {
                if(this.left != null)
                {
                    return this.left.Add(points, minDistSq, point);
                }
                else
                {
                    this.left = new Node3Y
                    {
                        Index = points.Count
                    };
                    return this.left.Index;
                }
            }
            if(this.right != null)
            {
                return this.right.Add(points, minDistSq, point);
            }
            else
            {
                this.right = new Node3Y
                {
                    Index = points.Count
                };
                return this.right.Index;
            }
        }
    }

    class Node3Y : Node3
    {
        private Node3Z left = null;
        private Node3Z right = null;

        public override int Add(IReadOnlyList<Point3> points, double minDistSq, Point3 point)
        {
            var p = points[this.Index];
            if(distSq(p, point) < minDistSq)
            {
                return this.Index;
            }
            if(p.Y.CompareTo(point.Y) <= 0)
            {
                if(this.left != null)
                {
                    return this.left.Add(points, minDistSq, point);
                }
                else
                {
                    this.left = new Node3Z
                    {
                        Index = points.Count
                    };
                    return this.left.Index;
                }
            }
            if(this.right != null)
            {
                return this.right.Add(points, minDistSq, point);
            }
            else
            {
                this.right = new Node3Z
                {
                    Index = points.Count
                };
                return this.right.Index;
            }
        }
    }

    class Node3Z : Node3
    {
        private Node3X left = null;
        private Node3X right = null;

        public override int Add(IReadOnlyList<Point3> points, double minDistSq, Point3 point)
        {
            var p = points[this.Index];
            if(distSq(p, point) < minDistSq)
            {
                return this.Index;
            }
            if(p.Z.CompareTo(point.Z) <= 0)
            {
                if(this.left != null)
                {
                    return this.left.Add(points, minDistSq, point);
                }
                else
                {
                    this.left = new Node3X
                    {
                        Index = points.Count
                    };
                    return this.left.Index;
                }
            }
            if(this.right != null)
            {
                return this.right.Add(points, minDistSq, point);
            }
            else
            {
                this.right = new Node3X
                {
                    Index = points.Count
                };
                return this.right.Index;
            }
        }
    }

    /// <summary>
    /// Klasse für vernetzte Flächen
    /// </summary>
    public class Mesh
    {
        /// <summary>
        /// Umschließen die Flächen eine Körper (wichtig bei der Bestimmung identischer Punkte)
        /// Wenn kein Shape wird Z-Wert ignoriert
        /// </summary>
        public bool IsShape { get; }

        private Node root;
        private double minDistSq;

        #region Vertices

        /// <summary>
        /// Punkte (Vertices) des Mesh
        /// </summary>
        public List<Point3> Points { get; }

        /// <summary>
        /// Index einer abgehenden Halbkante der Punkte
        /// </summary>
        public List<int> VertexEdges { get; }

        #endregion Vertices

        #region Edges

        /// <summary>
        /// Index des Startvertex der Halbkanten
        /// </summary>
        public List<int> EdgeVertices { get; }

        /// <summary>
        /// Index der Folgehalbkante der Halbkanten
        /// </summary>
        public List<int> EdgeNexts { get; }

        /// <summary>
        /// Index des Faces der Halbkanten
        /// </summary>
        public List<int> EdgeFaces { get; }

        /// <summary>
        /// Map der Halbkanten mit Indices der Anfangs- und Endvertices und zugeordnetem Index in Liste
        /// </summary>
        public Dictionary<TupleIdx, int> EdgeIndices { get; }

        /// <summary>
        /// Set der unveränderlichen Kanten
        /// </summary>
        public HashSet<TupleIdx> FixedEdges { get; }

        #endregion Edges

        #region Faces

        /// <summary>
        /// Index einer Halbkante der Faces
        /// </summary>
        public List<int> FaceEdges { get; }

        /// <summary>
        /// Maximale Anzahl von Ecken/Kanten aller enthaltenen Faces
        /// </summary>
        public int MaxFaceCorners { get; private set; }

        /// <summary>
        /// Minimale Anzahl von Ecken/Kanten aller enthaltenen Faces
        /// </summary>
        public int MinFaceCorners { get; private set; }

        #endregion Faces

        #region BBox

        /// <summary>
        /// Minimaler X-Wert
        /// </summary>
        public double MinX { get; private set; }
 
        /// <summary>
        /// Maximaler X-Wert
        /// </summary>
        public double MaxX { get; private set; }

        /// <summary>
        /// Minimaler Y-Wert
        /// </summary>
        public double MinY { get; private set; }

        /// <summary>
        /// Maximaler Y-Wert
        /// </summary>
        public double MaxY { get; private set; }

        /// <summary>
        /// Minimaler Z-Wert
        /// </summary>
        public double MinZ { get; private set; }

        /// <summary>
        /// Maximaler Z-Wert
        /// </summary>
        public double MaxZ { get; private set; }

        #endregion BBox

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="isShape">Begrenzen Flächen einen Körper</param>
        /// <param name="minDist">Mindestabstand unterschiedlicher Punkte</param>
        public Mesh(bool isShape, double minDist)
        {
            this.IsShape = isShape;
            this.root = null;
            this.minDistSq = minDist * minDist;

            this.Points = new List<Point3>();
            this.VertexEdges = new List<int>();

            this.EdgeVertices = new List<int>();
            this.EdgeNexts = new List<int>();
            this.EdgeFaces = new List<int>();
            this.EdgeIndices = new Dictionary<TupleIdx, int>();
            this.FixedEdges = new HashSet<TupleIdx>();

            this.MaxFaceCorners = 0;
            this.MinFaceCorners = int.MaxValue;
            this.FaceEdges = new List<int>();

            this.MinX = double.PositiveInfinity;
            this.MinY = double.PositiveInfinity;
            this.MinZ = double.PositiveInfinity;
            this.MaxX = double.NegativeInfinity;
            this.MaxY = double.NegativeInfinity;
            this.MaxZ = double.NegativeInfinity;
        }

        /// <summary>
        /// Konstruktor als Kopie
        /// </summary>
        /// <param name="mesh">zu kopierendes Mesh</param>
        public Mesh(Mesh mesh)
        {
            this.IsShape = mesh.IsShape;
            this.root = mesh.root;
            this.minDistSq = mesh.minDistSq;

            this.Points = new List<Point3>(mesh.Points);
            this.VertexEdges = new List<int>(mesh.VertexEdges);

            this.EdgeVertices = new List<int>(mesh.EdgeVertices);
            this.EdgeNexts = new List<int>(mesh.EdgeNexts);
            this.EdgeFaces = new List<int>(mesh.EdgeFaces);
            this.EdgeIndices = new Dictionary<TupleIdx, int>(mesh.EdgeIndices);
            this.FixedEdges = new HashSet<TupleIdx>(mesh.FixedEdges);

            this.MaxFaceCorners = mesh.MaxFaceCorners;
            this.FaceEdges = new List<int>(mesh.FaceEdges);

            this.MinX = mesh.MinX;
            this.MinY = mesh.MinY;
            this.MinZ = mesh.MinZ;
            this.MaxX = mesh.MaxX;
            this.MaxY = mesh.MaxY;
            this.MaxZ = mesh.MaxZ;

        }

        private void extentBBox(Point3 point)
        {
            if(point.X < this.MinX)
            { this.MinX = point.X; }
            if(point.X > this.MaxX)
            { this.MaxX = point.X; }
            if(point.Y < this.MinY)
            { this.MinY = point.Y; }
            if(point.Y > this.MaxY)
            { this.MaxY = point.Y; }
            if(point.Z < this.MinZ)
            { this.MinZ = point.Z; }
            if(point.Z > this.MaxZ)
            { this.MaxZ = point.Z; }
        }

        /// <summary>
        /// Fügt einen Punkt dem Mesh als Vertex hinzu
        /// </summary>
        /// <param name="point">Punkt</param>
        /// <returns>Index des Vertex</returns>
        public int AddPoint(Point3 point)
        {
            int pi;
            if(this.root == null)
            {
                if(this.IsShape)
                { this.root = new Node3X { Index = 0 }; }
                else
                { this.root = new Node2X { Index = 0 }; }
                pi = 0;
            }
            else
            {
                pi = this.root.Add(this.Points, this.minDistSq, point);
            }
            if(pi == this.Points.Count)
            {
                this.Points.Add(point);
                this.extentBBox(point);
                this.VertexEdges.Add(-1);
            }
            return pi;
        }

        /// <summary>
        /// Fixiert eine Kante
        /// </summary>
        /// <param name="point1">Index des ersten Punktes/Vertex</param>
        /// <param name="point2">Index des zweiten Punktes/Vertex</param>
        public void FixEdge(int point1, int point2)
        {
            if(point1 >= 0 && point2 >= 0 && point1 < this.Points.Count && point2 < this.Points.Count)
            {
                var e = new TupleIdx(point2, point1);
                if(!this.FixedEdges.Contains(e))
                {
                    this.FixedEdges.Add(TupleIdx.Flipped(e));
                }
            }
        }

        /// <summary>
        /// Fügt neues Face hinzu
        /// </summary>
        /// <param name="points">Eckpunkte</param>
        /// <returns>Faceindex wenn erzeugt</returns>
        public int? AddFace(IReadOnlyList<Point3> points)
        {
            var vertices = new List<int>();
            foreach(var p in points)
            { vertices.Add(this.AddPoint(p)); }
            return this.AddFace(vertices);
        }

        /// <summary>
        /// Fügt neues Face hinzu
        /// </summary>
        /// <param name="pointIdToVertexMap">Mapping der Punktnummern zum Index</param>
        /// <param name="pointIds">Punktnummern der Eckpunkte</param>
        /// <returns>Faceindex wenn erzeugt</returns>
        public int? AddFace(IReadOnlyDictionary<int, int> pointIdToVertexMap, IReadOnlyList<int> pointIds)
        {
            var vertices = new List<int>();
            int vi;
            foreach(int pi in pointIds)
            {
                if(pointIdToVertexMap.TryGetValue(pi, out vi))
                { vertices.Add(vi); }
                else
                { return null; }
            }
            return this.AddFace(vertices);
        }

        /// <summary>
        /// Fügt neues Face hinzu
        /// </summary>
        /// <param name="vertices">Indices der Eckpunkte(Vertices)</param>
        /// <returns>Faceindex wenn erzeugt</returns>
        public int? AddFace(IReadOnlyList<int> vertices)
        {
            if(vertices.Count < 3)
            { return null; }

            // Vertices prüfen (auf Wiederholung)
            var verts = new List<int> { vertices[0] };
            foreach(int v in vertices)
            {
                // evtl. durch Grenzwertunterschreitung entfernte Kante ignorieren
                if(verts[verts.Count - 1] != v)
                {
                    // ein Vertex darf nur einmal im Face vorhanden sein
                    if(verts.Contains(v))
                    { return null; }
                    verts.Add(v);
                }
            }
            int vcnt = verts.Count;
            if(vcnt < 3)
            { return null; }

            // ersten Punkt anhängen
            verts.Add(verts[0]);

            // evtl. Reihenfolge umkehren
            if(this.IsShape)
            {
                // da hier keine Richtung definierbar ist, muss bei vorhandener Kante geprüft werden ob umgedrehte Richtung möglich
                int fcnt = 0, rcnt = 0;

                for(int i = 1; i < verts.Count; i++)
                {
                    var e = new TupleIdx(verts[i - 1], verts[i]);
                    if(this.EdgeIndices.ContainsKey(e))
                    {
                        fcnt++;
                    }
                    else if(this.EdgeIndices.ContainsKey(TupleIdx.Flipped(e)))
                    {
                        rcnt++;
                    }
                }
                if(fcnt > 0)
                {
                    // keien Drehung Möglich
                    if(rcnt > 0)
                    { return null; }
                    verts.Reverse();
                }
            }
            else
            {
                // 2D müssen immer positiven Z-Wert in Normale haben
                //var a = this.Points[verts[1]];
                //var b = this.Points[verts[2]];
                //var c = this.Points[verts[0]];
                //double normalZ = ((a.X - c.X) * (b.Y - c.Y)) - ((a.Y - c.Y) * (b.X - c.X));

                if(Vector3.Cross(this.Points[verts[1]] - this.Points[verts[0]], this.Points[verts[2]] - this.Points[verts[0]]).Z < 0.0)
                {
                    verts.Reverse();
                }

                // wenn Kante vorhanden, abbrechen
                for(int i = 1; i < verts.Count; i++)
                {
                    if(this.EdgeIndices.ContainsKey(new TupleIdx(verts[i - 1], verts[i])))
                    {
                        return null;
                    }
                }
            }

            // Kanten hinzufügen
            int face = this.FaceEdges.Count;
            int fst = this.EdgeFaces.Count;
            this.FaceEdges.Add(fst);
            for(int i = 1; i < verts.Count; i++)
            {
                var e = new TupleIdx(verts[i - 1], verts[i]);

                // Vertex der Halbkante zuordnen (nur einmal nötig)
                if(this.VertexEdges[e.Idx1] < 0)
                { this.VertexEdges[e.Idx1] = fst + i - 1; }

                // Kante erzeugen
                this.EdgeIndices.Add(e, fst + i - 1);
                this.EdgeVertices.Add(e.Idx1);
                this.EdgeNexts.Add(fst + (i % vcnt));
                this.EdgeFaces.Add(face);
            }

            if(vcnt > this.MaxFaceCorners)
            { this.MaxFaceCorners = vcnt; }
            if(vcnt < this.MinFaceCorners)
            { this.MinFaceCorners = vcnt; }

            return face;
        }

    }
}
