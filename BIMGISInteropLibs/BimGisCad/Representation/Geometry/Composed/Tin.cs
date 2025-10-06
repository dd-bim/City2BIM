using BimGisCad.Representation.Geometry.Elementary;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad.Representation.Geometry.Composed
{
    /// <summary>
    /// Klasse für TIN Objekte, Achtung Dreiecksindizes müssen gegen Uhrzeigersinn geordnet sein!
    /// </summary>
    public readonly struct Tin
    {
        /// <summary>
        /// Punkte des TIN
        /// </summary>
        public IReadOnlyList<Point3> Points { get; }

        /// <summary>
        /// Dreiecke, Indizes der Vertizes (gegen Uhrzeigersinn ist positive Fläche!)
        /// </summary>
        public IReadOnlyList<int> Triangles { get; }

        /// <summary>
        /// Indizes der Nachbardreiecke (an Kante gegenüber Vertex, wenn <code>null</code> dann ohne Nachbar)
        /// </summary>
        public IReadOnlyList<int?> Neighbours { get; }

        /// <summary>
        /// Markierte Kanten (in der Regel Bruchkante, im Dreieck die Kante gegenüber Vertex)
        /// </summary>
        public BitArray MarkedEdges { get; }

        /// <summary>
        /// Sind markierte Kanten (Bruchlinien) vorhanden ?
        /// </summary>
        public bool HasMarkedLines => MarkedEdges.Length > 0;

        /// <summary>
        /// Sind Nachbardreiecke definiert ?
        /// </summary>
        public bool HasNeighBours => Neighbours.Count > 0;

        /// <summary>
        /// Anzahl der Dreiecke
        /// </summary>
        public int NumTriangles => Triangles.Count / 3;

        /// <summary>
        /// Konstruktor 
        /// </summary>
        /// <param name="points">Punkte des TIN</param>
        /// <param name="triangles">Dreiecke des TIN</param>
        /// <param name="neighbours">Nachbardreiecke</param>
        /// <param name="markedEdges">Markierte Kanten</param>
        public Tin(in Point3[] points, in int[] triangles, in int?[] neighbours, in BitArray markedEdges)
        {
            Points = points;
            Triangles = triangles;
            Neighbours = neighbours;
            MarkedEdges = markedEdges;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="points">Punkte des TIN</param>
        /// <param name="triangles">Dreiecke des TIN</param>
        /// <param name="neighbours">Nachbardreiecke</param>
        /// <param name="markedEdges">Markierte Kanten</param>
        public Tin(Point3[] points, int[] triangles, int?[] neighbours, bool[] markedEdges)
        {
            Points = points;
            Triangles = triangles;
            Neighbours = neighbours;
            MarkedEdges = new BitArray(markedEdges);
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="points">Punkte des TIN</param>
        /// <param name="triangles">Dreiecke des TIN</param>
        /// <param name="neighbours">Nachbardreiecke</param>
        public Tin(Point3[] points, int[] triangles, int?[] neighbours)
        {
            Points = points;
            Triangles = triangles;
            Neighbours = neighbours;
            MarkedEdges = new BitArray(0);
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="points">Punkte des TIN</param>
        /// <param name="triangles">Dreiecke des TIN</param>
        /// <param name="markedEdges">Markierte Kanten</param>
        public Tin(Point3[] points, int[] triangles, BitArray markedEdges)
        {
            Points = points;
            Triangles = triangles;
            Neighbours = Array.Empty<int?>();
            MarkedEdges = markedEdges;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="points">Punkte des TIN</param>
        /// <param name="triangles">Dreiecke des TIN</param>
        /// <param name="markedEdges">Markierte Kanten</param>
        public Tin(Point3[] points, int[] triangles, bool[] markedEdges)
        {
            Points = points;
            Triangles = triangles;
            Neighbours = Array.Empty<int?>();
            MarkedEdges = new BitArray(markedEdges);
        }

        /// <summary>
        /// Konstrukor
        /// </summary>
        /// <param name="points">Punkte des TIN</param>
        /// <param name="triangles">Dreiecke des TIN</param>
        public Tin(Point3[] points, int[] triangles)
        {
            Points = points;
            Triangles = triangles;
            Neighbours = Array.Empty<int?>();
            MarkedEdges = new BitArray(0);
        }

        /// <summary>
        /// Punktindizes der Dreiecksvertizes
        /// </summary>
        /// <param name="triangleIndex">Index des Dreiecks</param>
        /// <param name="pointIndexVertexA">Punktindex des ersten Vertex</param>
        /// <param name="pointIndexVertexB">Punktindex des zweiten Vertex</param>
        /// <param name="pointIndexVertexC">Punktindex des dritten Vertex</param>
        public void TriangleVertexPointIndizesAt(int triangleIndex, out int pointIndexVertexA, out int pointIndexVertexB, out int pointIndexVertexC)
        {
            int i0 = triangleIndex * 3;
            pointIndexVertexA = Triangles[i0];
            pointIndexVertexB = Triangles[i0 + 1];
            pointIndexVertexC = Triangles[i0 + 2];
        }

        /// <summary>
        /// Vertizes des Dreiecks
        /// </summary>
        /// <param name="triangleIndex">Index des Dreiecks</param>
        /// <param name="vertexA">Erster Vertex</param>
        /// <param name="vertexB">Zweiter Vertex</param>
        /// <param name="vertexC">Dritter Vertex</param>
        public void TriangleVertizesAt(int triangleIndex, out Point3 vertexA, out Point3 vertexB, out Point3 vertexC)
        {
            int i0 = triangleIndex * 3;
            vertexA = Points[Triangles[i0]];
            vertexB = Points[Triangles[i0 + 1]];
            vertexC = Points[Triangles[i0 + 2]];
        }

        /// <summary>
        /// Indizes der Nachbardreiecke eines Dreiecksv
        /// </summary>
        /// <param name="triangleIndex">Index des Dreiecks</param>
        /// <param name="neighbourIndexA">Index des Nachbardreieckes genüber des ersten Vertex</param>
        /// <param name="neighbourIndexB">Index des Nachbardreieckes genüber des zweiten Vertex</param>
        /// <param name="neighbourIndexC">Index des Nachbardreieckes genüber des dritten Vertex</param>
        public void TriangleNeighbourIndizesAt(int triangleIndex, out int? neighbourIndexA, out int? neighbourIndexB, out int? neighbourIndexC)
        {
            int i0 = triangleIndex * 3;
            neighbourIndexA = Neighbours[i0];
            neighbourIndexB = Neighbours[i0 + 1];
            neighbourIndexC = Neighbours[i0 + 2];
        }

        /// <summary>
        /// Aufzählung aller Dreiecke (Punktindizes der Vertizes)
        /// </summary>
        public IEnumerable<int[]> TriangleVertexPointIndizes()
        {
            for (int i = 0; i < Triangles.Count;)
            {
                yield return new int[] { Triangles[i++], Triangles[i++], Triangles[i++] };
            }
        }

        /// <summary>
        /// Aufzählung aller Dreiecke (Vertizes)
        /// </summary>
        public IEnumerable<Point3[]> TriangleVertizes()
        {
            for (int i = 0; i < Triangles.Count;)
            {
                yield return new Point3[] { Points[Triangles[i++]], Points[Triangles[i++]], Points[Triangles[i++]] };
            }
        }

        /// <summary>
        /// Aufzählung aller Nachbardreiecksindizes
        /// </summary>
        public IEnumerable<int?[]> TriangleNeighbourIndizes()
        {
            for (int i = 0; i < Neighbours.Count;)
            {
                yield return new int?[] { Neighbours[i++], Neighbours[i++], Neighbours[i++] };
            }
        }

        /// <summary>
        /// Aufzählung aller markierten Kanten (Punktindizes)
        /// </summary>
        public IEnumerable<int[]> MarkedEdgePointIndizes()
        {
            for (int i = 0; i < MarkedEdges.Length; i++)
            {
                if (MarkedEdges[i])
                {
                    yield return new[] { Triangles[i + (i + 1) % 3], Triangles[i + (i + 2) % 3] };
                }
            }
        }

        /// <summary>
        /// Aufzählung aller markierten Kanten (Punktindizes)
        /// </summary>
        public IEnumerable<Point3[]> MarkedEdgeVertizes()
        {
            for (int i = 0; i < MarkedEdges.Length; i++)
            {
                if (MarkedEdges[i])
                {
                    yield return new[] { Points[Triangles[i + (i + 1) % 3]], Points[Triangles[i + (i + 2) % 3]] };
                }
            }
        }

        /// <summary>
        /// Erzeugt leeren Builder
        /// </summary>
        /// <param name="mapIndizes">Eingabe Indizes entsprechen nicht der Position der Reihenfolge</param>
        /// <returns></returns>
        public static Tin.Builder CreateBuilder(bool mapIndizes) => new Tin.Builder(mapIndizes);

        /// <summary>
        /// Klasse zum Erzeugen eines Tin durch schrittweises Hinzufügen von Elementen
        /// </summary>
        public class Builder
        {
            private readonly List<Point3> _points;
            private readonly List<int> _triangles;
            private readonly List<int?> _neighbours;
            private readonly List<int> _markedEdges;
            private readonly Dictionary<int, int> _pointMaps;
            private readonly Dictionary<int, int> _triangleMaps;
            private readonly bool _indizesMapped;


            internal Builder(bool mapIndizes)
            {
                _points = new List<Point3>();
                _triangles = new List<int>();
                _neighbours = new List<int?>();
                _markedEdges = new List<int>();
                _indizesMapped = mapIndizes;
                _pointMaps = new Dictionary<int, int>();
                _triangleMaps = new Dictionary<int, int>();
            }

            /// <summary>
            /// Fügt Punkt dem TIN hinzu
            /// </summary>
            /// <param name="point"></param>
            public void AddPoint(in Point3 point)
            {
                if (!_indizesMapped)
                    _points.Add(point);
            }

            /// <summary>
            /// Fügt Punkt dem TIN hinzu
            /// </summary>
            /// <param name="number"></param>
            /// <param name="point"></param>
            public void AddPoint(int number, in Point3 point)
            {
                _pointMaps[number] = _points.Count;
                _points.Add(point);
            }

            /// <summary>
            /// Fügt Punkt dem TIN hinzu
            /// </summary>
            /// <param name="number"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public void AddPoint(int number, in double x, in double y, in double z)
            {
                AddPoint(number, Point3.Create(x, y, z));
            }

            /// <summary>
            /// Fügt Punkt dem TIN hinzu
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public void AddPoint(in double x, in double y, in double z)
            {
                if (!_indizesMapped)
                    AddPoint(Point3.Create(x, y, z));
            }

            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="pointIndexVertexA">Punktindex des ersten Vertex</param>
            /// <param name="pointIndexVertexB">Punktindex des zweiten Vertex</param>
            /// <param name="pointIndexVertexC">Punktindex des dritten Vertex</param>
            /// <param name="triangleIndexNeighbourA">Index des Nachbardreieckes gegenüber des ersten Vertex oder <code>null</code></param>
            /// <param name="triangleIndexNeighbourB">Index des Nachbardreieckes gegenüber des zweiten Vertex oder <code>null</code></param>
            /// <param name="triangleIndexNeighbourC">Index des Nachbardreieckes gegenüber des dritten Vertex oder <code>null</code></param>
            /// <param name="isMarkedEdgeA">Kante gegenüber des ersten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeB">Kante gegenüber des zweiten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeC">Kante gegenüber des dritten Vertex ist markiert (Bruchkante)</param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int pointIndexVertexA, int pointIndexVertexB, int pointIndexVertexC,
                int? triangleIndexNeighbourA, int? triangleIndexNeighbourB, int? triangleIndexNeighbourC,
                bool isMarkedEdgeA, bool isMarkedEdgeB, bool isMarkedEdgeC,
                bool clockwise = false)
            {
                if (_indizesMapped)
                {
                    if (clockwise)
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexC, pointIndexVertexB });
                        _neighbours.AddRange(new[]{ triangleIndexNeighbourA, triangleIndexNeighbourC, triangleIndexNeighbourB});
                        _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeC ? 2 : 0) + (isMarkedEdgeB ? 4 : 0));
                    }
                    else
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexB, pointIndexVertexC });
                        _neighbours.AddRange(new[]{ triangleIndexNeighbourA, triangleIndexNeighbourB, triangleIndexNeighbourC});
                        _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeB ? 2 : 0) + (isMarkedEdgeC ? 4 : 0));
                    }
                }
            }

            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="pointIndexVertexA">Punktindex des ersten Vertex</param>
            /// <param name="pointIndexVertexB">Punktindex des zweiten Vertex</param>
            /// <param name="pointIndexVertexC">Punktindex des dritten Vertex</param>
            /// <param name="triangleIndexNeighbourA">Index des Nachbardreieckes gegenüber des ersten Vertex oder <code>null</code></param>
            /// <param name="triangleIndexNeighbourB">Index des Nachbardreieckes gegenüber des zweiten Vertex oder <code>null</code></param>
            /// <param name="triangleIndexNeighbourC">Index des Nachbardreieckes gegenüber des dritten Vertex oder <code>null</code></param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int pointIndexVertexA, int pointIndexVertexB, int pointIndexVertexC,
                int? triangleIndexNeighbourA, int? triangleIndexNeighbourB, int? triangleIndexNeighbourC,
                bool clockwise = false)
            {
                if (_indizesMapped)
                {
                    if (clockwise)
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexC, pointIndexVertexB });
                        _neighbours.AddRange(new[]{ triangleIndexNeighbourA, triangleIndexNeighbourC, triangleIndexNeighbourB});
                    }
                    else
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexB, pointIndexVertexC });
                        _neighbours.AddRange(new[]{ triangleIndexNeighbourA, triangleIndexNeighbourB, triangleIndexNeighbourC});
                    }
                }
            }

            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="pointIndexVertexA">Punktindex des ersten Vertex</param>
            /// <param name="pointIndexVertexB">Punktindex des zweiten Vertex</param>
            /// <param name="pointIndexVertexC">Punktindex des dritten Vertex</param>
            /// <param name="isMarkedEdgeA">Kante gegenüber des ersten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeB">Kante gegenüber des zweiten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeC">Kante gegenüber des dritten Vertex ist markiert (Bruchkante)</param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int pointIndexVertexA, int pointIndexVertexB, int pointIndexVertexC,
                bool isMarkedEdgeA, bool isMarkedEdgeB, bool isMarkedEdgeC,
                bool clockwise = false)
            {
                if (_indizesMapped)
                {
                    if (clockwise)
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexC, pointIndexVertexB });
                        _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeC ? 2 : 0) + (isMarkedEdgeB ? 4 : 0));
                    }
                    else
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexB, pointIndexVertexC });
                        _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeB ? 2 : 0) + (isMarkedEdgeC ? 4 : 0));
                    }
                }
            }

            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="pointIndexVertexA">Punktindex des ersten Vertex</param>
            /// <param name="pointIndexVertexB">Punktindex des zweiten Vertex</param>
            /// <param name="pointIndexVertexC">Punktindex des dritten Vertex</param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int pointIndexVertexA, int pointIndexVertexB, int pointIndexVertexC,
                bool clockwise = false)
            {
                if (_indizesMapped)
                {
                    if (clockwise)
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexC, pointIndexVertexB });
                    }
                    else
                    {
                        _triangles.AddRange(new[] { pointIndexVertexA, pointIndexVertexB, pointIndexVertexC });
                    }
                }
            }

            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="number">Nummer des Dreiecks</param>
            /// <param name="numberVertexA">Punktnummer des ersten Vertex</param>
            /// <param name="numberVertexB">Punktnummer des zweiten Vertex</param>
            /// <param name="numberVertexC">Punktnummer des dritten Vertex</param>
            /// <param name="numberNeighbourA">Nummer des Nachbardreieckes gegenüber des ersten Vertex oder <code>null</code></param>
            /// <param name="numberNeighbourB">Nummer des Nachbardreieckes gegenüber des zweiten Vertex oder <code>null</code></param>
            /// <param name="numberNeighbourC">Nummer des Nachbardreieckes gegenüber des dritten Vertex oder <code>null</code></param>
            /// <param name="isMarkedEdgeA">Kante gegenüber des ersten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeB">Kante gegenüber des zweiten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeC">Kante gegenüber des dritten Vertex ist markiert (Bruchkante)</param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int number, int numberVertexA, int numberVertexB, int numberVertexC,
                int? numberNeighbourA, int? numberNeighbourB, int? numberNeighbourC,
                bool isMarkedEdgeA, bool isMarkedEdgeB, bool isMarkedEdgeC,
                bool clockwise = false)
            {
                _triangleMaps[number] = _triangles.Count / 3;
                if (clockwise)
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexC, numberVertexB });
                    _neighbours.AddRange(new[]{ numberNeighbourA, numberNeighbourC, numberNeighbourB});
                    _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeC ? 2 : 0) + (isMarkedEdgeB ? 4 : 0));
                }
                else
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexB, numberVertexC });
                    _neighbours.AddRange(new[]{ numberNeighbourA, numberNeighbourB, numberNeighbourC});
                    _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeB ? 2 : 0) + (isMarkedEdgeC ? 4 : 0));
                }
            }


            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="number">Nummer des Dreiecks</param>
            /// <param name="numberVertexA">Punktnummer des ersten Vertex</param>
            /// <param name="numberVertexB">Punktnummer des zweiten Vertex</param>
            /// <param name="numberVertexC">Punktnummer des dritten Vertex</param>
            /// <param name="numberNeighbourA">Nummer des Nachbardreieckes gegenüber des ersten Vertex oder <code>null</code></param>
            /// <param name="numberNeighbourB">Nummer des Nachbardreieckes gegenüber des zweiten Vertex oder <code>null</code></param>
            /// <param name="numberNeighbourC">Nummer des Nachbardreieckes gegenüber des dritten Vertex oder <code>null</code></param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int number, int numberVertexA, int numberVertexB, int numberVertexC,
                int? numberNeighbourA, int? numberNeighbourB, int? numberNeighbourC,
                bool clockwise = false)
            {
                _triangleMaps[number] = _triangles.Count / 3;
                if (clockwise)
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexC, numberVertexB });
                    _neighbours.AddRange(new[]{ numberNeighbourA, numberNeighbourC, numberNeighbourB});
                 }
                else
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexB, numberVertexC });
                    _neighbours.AddRange(new[]{ numberNeighbourA, numberNeighbourB, numberNeighbourC});
                }
            }


            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="number">Nummer des Dreiecks</param>
            /// <param name="numberVertexA">Punktnummer des ersten Vertex</param>
            /// <param name="numberVertexB">Punktnummer des zweiten Vertex</param>
            /// <param name="numberVertexC">Punktnummer des dritten Vertex</param>
            /// <param name="isMarkedEdgeA">Kante gegenüber des ersten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeB">Kante gegenüber des zweiten Vertex ist markiert (Bruchkante)</param>
            /// <param name="isMarkedEdgeC">Kante gegenüber des dritten Vertex ist markiert (Bruchkante)</param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int number, int numberVertexA, int numberVertexB, int numberVertexC,
                bool isMarkedEdgeA, bool isMarkedEdgeB, bool isMarkedEdgeC,
                bool clockwise = false)
            {
                _triangleMaps[number] = _triangles.Count / 3;
                if (clockwise)
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexC, numberVertexB });
                    _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeC ? 2 : 0) + (isMarkedEdgeB ? 4 : 0));
                }
                else
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexB, numberVertexC });
                    _markedEdges.Add((isMarkedEdgeA ? 1 : 0) + (isMarkedEdgeB ? 2 : 0) + (isMarkedEdgeC ? 4 : 0));
                }
            }

            /// <summary>
            /// Fügt Dreieck dem TIN hinzu
            /// </summary>
            /// <param name="number">Nummer des Dreiecks</param>
            /// <param name="numberVertexA">Punktnummer des ersten Vertex</param>
            /// <param name="numberVertexB">Punktnummer des zweiten Vertex</param>
            /// <param name="numberVertexC">Punktnummer des dritten Vertex</param>
            /// <param name="clockwise">Hier <code>true</code> setzen wenn Reihenfolge der Vertizes im Uhrzeigersinn, sonst weglassen oder <code>false</code></param>
            public void AddTriangle(int number, int numberVertexA, int numberVertexB, int numberVertexC,
                bool clockwise = false)
            {
                _triangleMaps[number] = _triangles.Count / 3;
                if (clockwise)
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexC, numberVertexB });
                }
                else
                {
                    _triangles.AddRange(new[] { numberVertexA, numberVertexB, numberVertexC });
                }
            }

            /// <summary>
            /// Erzeugt <see cref="Tin"/> aus diesem <see cref="Builder"/>
            /// </summary>
            /// <returns>Neues <see cref="Tin"/></returns>
            public Tin ToTin(out IReadOnlyDictionary<int,int> pointIndex2NumberMap, out IReadOnlyDictionary<int,int> triangleIndex2NumberMap)
            {
                var markedEdges = new BitArray(_triangles.Count == _markedEdges.Count * 3 ? _triangles.Count : 0);
                if (markedEdges.Length > 0)
                {
                    for (int i = 0; i < _markedEdges.Count; i++)
                    {
                        var val = _markedEdges[i];
                        int i0 = i * 3;
                        markedEdges.Set(i0, (val & 1) == 1);
                        markedEdges.Set(i0 + 1, (val & 2) == 2);
                        markedEdges.Set(i0 + 2, (val & 4) == 4);
                    }
                }
                var pointMap = new Dictionary<int, int>();
                foreach (var numidx in _pointMaps)
                {
                    pointMap[numidx.Value] = numidx.Key;
                }
                var triangleMap = new Dictionary<int, int>();
                foreach (var numidx in _triangleMaps)
                {
                    triangleMap[numidx.Value] = numidx.Key;
                }
                bool hasNeighbours = _triangles.Count == _neighbours.Count;
                for (int i = 0; i < _triangles.Count; i++)
                {
                    _triangles[i] = _pointMaps[_triangles[i]];
                    if (hasNeighbours && _neighbours[i].HasValue)
                        _neighbours[i] = _triangleMaps[_neighbours[i].Value];
                }
                pointIndex2NumberMap = pointMap;
                triangleIndex2NumberMap = triangleMap;
                return new Tin(_points.ToArray(), _triangles.ToArray(),
                    hasNeighbours ? _neighbours.ToArray() : Array.Empty<int?>(), markedEdges);
            }

        }
    }
}
