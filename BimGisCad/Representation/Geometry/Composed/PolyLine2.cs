using BimGisCad.Representation.Geometry.Elementary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Composed
{
    /// <summary>
    /// 2D Polylinie
    /// </summary>
    public class PolyLine2 : IEnumerable<Point2>
    {
        private bool? _isClosed;
        private bool? _isCCW;
 
        private readonly double _mindist;

        private readonly List<Point2> _points;


        private PolyLine2(double mindist)
        {
            this._mindist = mindist;
            this._points = new List<Point2>();
        }

        private PolyLine2(double mindist, IEnumerable<Point2> points)
        {
            this._mindist = mindist;
            this._points = new List<Point2>(points);
        }

        private void reset()
        {
            _isClosed = null;
            _isCCW = null;
        }



        /// <summary>
        /// Polylinien mit mehr als 2 Eckpunkten (also mehr als 3 Punkten) und identischem Anfangs- und Endpunkt
        /// </summary>
        public bool IsClosed
        {
            get
            {
                if (!_isClosed.HasValue)
                {
                    _isClosed = _points.Count > 3 && _points[0].Coincident(_points[_points.Count - 1], _mindist);
                }
                return _isClosed.Value;
            }
        }

        /// <summary>
        /// Gegen Uhrzeigersinn orientiert und geschlossen
        /// </summary>
        public bool IsCCW
        {
            get
            {
                if (!_isCCW.HasValue)
                {
                    if (!IsClosed)
                    {
                        _isCCW = false;
                    }
                    else
                    {
                        // der LU Punkt muss ein konvexer Knick sein
                        // besser als gesamte Fläche zu berechnen
                        int last = _points.Count - 1;
                        int lui = 0;
                        var lup = _points[0];

                        for (int i = 1; i < last; i++)
                        {
                            if (_points[i].Y < lup.Y || (_points[i].Y == lup.Y && _points[i].X < lup.X))
                            {
                                lui = i;
                                lup = _points[i];
                            }
                        }
                        var prev = _points[lui == 0 ? last - 1 : lui - 1];
                        _isCCW = Vector2.Det(lup - prev, _points[lui + 1] - prev) > 0.0;
                    }
                }
                return _isCCW.Value;
            }
        }

        ///// <summary>
        ///// Berechnet Eigenschaften der Polyline
        ///// </summary>
        ///// <param name="poly">Polylinie</param>
        ///// <param name="mindist">Mindestabstand für Check ob geschlossen</param>
        ///// <param name="isClosed">Geschlossen?</param>
        ///// <param name="isConvex">wenn geschlossen: konvex?</param>
        ///// <param name="area">wenn geschlossen: die Fläche (negativ wenn im Uhrzeigersinn)</param>
        ///// <param name="centroid">Schwerpunkt (wenn geschlossen Flächenschwerpunkt)</param>
        //public static void Properties(PolyLine2 poly, double mindist, out bool isClosed, out bool isConvex, out double area, out Point2 centroid)
        //{
        //    int n = poly.Count - 1;
        //    isClosed = true;
        //    isConvex = false;
        //    area = double.NaN;
        //    // Einzelpunkt, oder Linienzug
        //    if (n < 1 || !poly[0].Coincident(poly[n], mindist))
        //    {
        //        centroid = Point2.Centroid(poly);
        //        isClosed = false;
        //        return;
        //    }
        //    // Schwerpunktberechnung
        //    IList<Vector2> vecs;
        //    centroid = Point2.Centroid(poly.Take(n).ToList(), out vecs);
        //    if (n < 4)
        //    {
        //        area = 0.0;
        //        return;
        //    }
        //    double x = 0.0, y = 0.0;
        //    area = 0.0;
        //    int sign = 0;
        //    for (int i = 1; i < n; i++)
        //    {
        //        double t = (vecs[i].X * vecs[i + 1].Y) - (vecs[i + 1].X * vecs[i].Y);
        //        if (sign < 2 && (t < -EPS || t > EPS))
        //        {
        //            sign = sign == 0 
        //                ? t > 0.0 ? 1 : -1 
        //                : sign < 0 != t < 0.0 
        //                ? 2 : sign;
        //        }
        //        x += (poly[i].X + poly[i + 1].X) * t;
        //        y += (poly[i].Y + poly[i + 1].Y) * t;
        //        area += t;
        //    }
        //    centroid = Point2.Create(centroid.X + (x / (3.0 * area)), centroid.Y + (y / (3.0 * area)));
        //    isConvex = sign != 2;
        //    area /= 2.0;
        //}

        /// <summary>
        /// Kopiert Anfangspunkt an das Ende, wenn nötig
        /// </summary>
        public void Close()
        {
            if (!IsClosed)
            {
                reset();
                _points.Add(_points[0]);
            }
        }

        /// <summary>
        /// Entfernt zu kurze Kanten
        /// </summary>
        /// <returns>Anzahl Entfernte Punkte</returns>
        public int RemoveShortEdges()
        {
            reset();
            int removed = 0;
            for (int i = _points.Count - 1; i > 0; i--)
            {
                if (_points[i - 1].Coincident(_points[i], _mindist))
                {
                    removed++;
                    _points.RemoveAt(i);
                }
            }
            // Sicherstellen das 1. und letzter identisch
            if (IsClosed)
            {
                _points[0] = _points[_points.Count - 1];
            }
            return removed;
        }

        public int RemoveStraightEdges()
        {
            reset();
            int removed = 0;
            for (int i = _points.Count - 2; i > 0; i--)
            {
                var dist = Point2.SignedDistance(_points[i - 1], _points[i + 1], _points[i]);
                if (dist > -_mindist && dist < _mindist)
                {
                    removed++;
                    _points.RemoveAt(i);
                }
            }
            if (IsClosed)
            {
                var dist = Point2.SignedDistance(_points[_points.Count - 2], _points[1], _points[0]);
                if (dist > -_mindist && dist < _mindist)
                {
                    removed++;
                    _points.RemoveAt(_points.Count - 1);
                    _points[0] = _points[_points.Count - 1];
                    reset();
                }
            }
            return removed;
        }


        /// <summary>
        /// Erzeugt Polylinie aus gegebenen Punkten
        /// </summary>
        /// <param name="mindist"></param>
        /// <param name="points">Polygonpunkte</param>
        /// <returns></returns>
        public static PolyLine2 Create(double mindist, params Point2[] points)
        {
            return Create(mindist, points);
        }

        /// <summary>
        /// Erzeugt Polylinie aus gegebenen Punkten,
        /// </summary>
        /// <param name="mindist"></param>
        /// <param name="points">Polygonpunkte</param>
        /// <returns></returns>
        public static PolyLine2 Create(double mindist, IEnumerable<Point2> points)
        {
            PolyLine2 poly = new PolyLine2(mindist, points);
            return poly;
        }

        public IEnumerator<Point2> GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Point2 this[int i]
        {
            get { return _points[i]; }
            set
            {
                reset();
                _points[i] = value;
            }
        }

        public void Add(Point2 point)
        {
            reset();
            _points.Add(point);
        }
    }
}