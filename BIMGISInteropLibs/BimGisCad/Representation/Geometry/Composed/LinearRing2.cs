using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Representation.Geometry.Elementary;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Composed
{
    /// <summary>
    /// 2D Linearring (geschlossener Linienzug)
    /// </summary>
    public class LinearRing2 : IEnumerable<Point2>
    {
        private readonly List<Point2> _points;
        private bool? _isCCW;

        /// BBox MinX
        public double MinX { get; private set; } = double.PositiveInfinity;

        /// BBox MinY
        public double MinY { get; private set; } = double.PositiveInfinity;

        /// BBox MaxX
        public double MaxX { get; private set; } = double.NegativeInfinity;

        /// BBox MaxY
        public double MaxY { get; private set; } = double.NegativeInfinity;

        private LinearRing2()
        { 
            _points = new List<Point2>();
        }

        private LinearRing2(IEnumerable<Point2> points)
        {
            _points = new List<Point2>(points);
            foreach (var point in _points)
            {
                minmax(point);
            }
        }

        private void reset()
        {
            _isCCW = null;
        }

        private void minmax(Point2 point)
        {
            if (point.X < MinX) { MinX = point.X; }
            if (point.Y < MinY) { MinY = point.Y; }
            if (point.X > MaxX) { MaxX = point.X; }
            if (point.Y > MaxY) { MaxY = point.Y; }
        }


        /// <summary>
        /// Erzeugt Linearring aus gegebenen Punkten (Achtung Anfang und Ende müssen verschieden sein!)
        /// </summary>
        /// <param name="points">Punkte</param>
        /// <returns></returns>
        public static LinearRing2 Create(params Point2[] points) => Create(points);

        /// <summary>
        /// Erzeugt Linearring aus gegebenen Punkten (Achtung Anfang und Ende müssen verschieden sein!)
        /// </summary>
        /// <param name="points">Punkte</param>
        /// <returns></returns>
        public static LinearRing2 Create(IEnumerable<Point2> points) => new LinearRing2(points);

        public IEnumerator<Point2> GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Ist Linienzug gegen Uhrzeigersinn orientiert? (Achtung doppelte Punkte oder "gerade" Knicke können Ergebnis verfälschen!)
        /// </summary>
        public bool IsCCW {
            get
            {
                if (!_isCCW.HasValue)
                {
                    // der LU Punkt muss ein konvexer Knick sein
                    // besser als gesamte Fläche zu berechnen
                    int last = _points.Count - 1;
                    int lui = 0;
                    var lup = _points[0];

                    for (int i = 1; i <= last; i++)
                    {
                        if (_points[i].Y < lup.Y || (_points[i].Y == lup.Y && _points[i].X < lup.X))
                        {
                            lui = i;
                            lup = _points[i];
                        }
                    }
                    var prev = _points[lui == 0 ? last : lui - 1];
                    var next = _points[lui == last ? 0 : lui + 1];
                    _isCCW = Vector2.Det(lup - prev, next - prev) > 0.0;

                }
                return _isCCW.Value;
            }
        }

        /// Anzahl Punkte
        public int Count => _points.Count;


        /// Indexer
        public Point2 this[int i] {
            get { return _points[i]; }
            set
            {
                reset();
                if(MinX == _points[i].X || MinY == _points[i].Y || MaxX == _points[i].X || MaxY == _points[i].Y)
                {
                    MinX  = double.PositiveInfinity;
                    MinY = double.PositiveInfinity;
                    MaxX = double.NegativeInfinity;     
                    MaxY = double.NegativeInfinity;
                    _points[i] = value;
                    foreach (var point in _points)
                    {
                        minmax(point);
                    }
                }
                else
                {
                    minmax(value);
                    _points[i] = value;
                }
            }
        }

        /// <summary>
        /// Fügt neuen Punkt hinzu
        /// </summary>
        /// <param name="point">Neuer Punkt</param>
        public void Add(Point2 point)
        {
            reset();
            minmax(point);
            _points.Add(point);
        }

        // TODO: noch implementieren
        ///// <summary>
        ///// Entfernt zu kurze Seiten und zu "gerade" Knicke
        ///// </summary>
        ///// <param name="lring">Eingabering</param>
        ///// <param name="mindist">Mindestabstand unterschiedlicher Punkte</param>
        ///// <returns>Neuen Linearring</returns>
        //public static LinearRing2 CleanGeometry(LinearRing2 lring, double mindist = MINDIST)
        //{

        //}

        /// <summary>
        /// Prüft ob Punkt innerhalb eines Rings liegt (Achtung Grenzfälle wenn Punkt auf oder Nahe einer Kante liegt verfälschen evtl. Ergebnis)
        /// </summary>
        /// <param name="ring">Linearring</param>
        /// <param name="point">Test Punkt</param>
        public static bool Inside(LinearRing2 ring, Point2 point)
        {
            // Algorithmus adaptiert aus http://geomalgorithms.com/a03-_inclusion.html
            int n = ring.Count;
            int wn = 0;    // the  winding number counter

            // loop through all edges of the polygon
            for (int i = 0; i < n; i++)
            { // edge from V[i] to  V[i+1]
                var next = ring[(i + 1) % n];
                if (ring[i].Y <= point.Y)
                { // start y <= P.y
                    if (next.Y > point.Y)  // an upward crossing
                    {
                        if (Point2.Det(ring[i], next, point) > 0.0)  // P left of  edge
                        {
                            ++wn; // have  a valid up intersect
                        }
                    }
                }
                else
                { // start y > P.y (no test needed)
                    if (next.Y <= point.Y)  // a downward crossing
                    {
                        if (Point2.Det(ring[i], next, point) < 0.0) // P right of  edge
                        {
                            --wn; // have  a valid down intersect
                        }
                    }
                }
            }
            return wn != 0;
        }
    }
}
