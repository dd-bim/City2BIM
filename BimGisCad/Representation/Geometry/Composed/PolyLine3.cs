using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BimGisCad.Representation.Geometry.Elementary;
using static BimGisCad.Representation.Geometry.Elementary.Common;

//namespace BimGisCad.Geometry.Composed
//{
//    public class PolyLine3 : List<Point3>, ICurve
//    {
//        private PolyLine3() : base()
//        {
//        }

//        private PolyLine3(int capacity) : base(capacity)
//        {
//        }

//        private PolyLine3(IEnumerable<Point3> points) : base(points)
//        {
//        }

//        public Dimensions Dim => Dimensions._3D;

//        /// <summary>
//        /// Polylinien mit mehr als 2 Eckpunkten (also mehr als 3 Punkten) und identischem Anfangs- und Endpunkt
//        /// </summary>
//        public bool IsClosed => this.Count > 3 && this[0].Equals(this[this.Count - 1]);

//        /// <summary>
//        /// Berechnet Eigenschaften der Polyline
//        /// </summary>
//        /// <param name="poly"></param>
//        /// <param name="isClosed">Geschlossen?</param>
//        /// <param name="centroid">Schwerpunkt (wenn geschlossen Flächenschwerpunkt)</param>
//        /// <param name="isConvex">wenn geschlossen: konvex?</param>
//        /// <param name="area">wenn geschlossen: die Fläche (negativ wenn im Uhrzeigersinn)</param>
//        /// <param name="normal">wenn geschlossen: Normale der Ebene (kann null sein)</param>
//        /// <param name="vv">Quadratsumme der Punktabstände von der Ebene</param>
//        public static void Properties(PolyLine3 poly, out bool isClosed, out Point3 centroid, out bool? isConvex, out double? area, out Direction3 normal, out double? vv)
//        {
//            int n = poly.Count - 1;
//            isClosed = true;
//            isConvex = null;
//            area = null;
//            normal = null;
//            vv = null;
//            if(n < 1 || !poly[0].Equals(poly[n]))
//            {
//                centroid = Point3.Centroid(poly);
//                isClosed = false;
//                return;
//            }
//            IList<Vector3> vecs;
//            centroid = Point3.Centroid(poly.Take(n).ToList(), out vecs);
//            if(n < 3)
//            { return; }
//            if(n == 3)
//            {
//                area = 0.0;
//                return;
//            }
//            var nrm = Vector3.Zero;
//            var crosses = new Vector3[n];
//            for(int i = 0, j = n - 1; i < n; j = i, i++)
//            {
//                crosses[i] = Vector3.Cross(vecs[j], vecs[i]);
//                nrm += crosses[i];
//            }
//            double norm = Vector3.Norm(nrm);
//            if(norm >= MINDIST_SQUARED2)
//            {
//                nrm /= norm;
//                var sp = Vector3.Zero;
//                bool cnvx = true;
//                for(int i = 0, j = n - 1; i < n; j = i, i++)
//                {
//                    double tarea = Vector3.Dot(crosses[i], nrm);
//                    cnvx &= tarea > -EPS;
//                    sp += (vecs[j] + vecs[i]) * tarea;
//                }

//                centroid += sp / (3.0 * norm);
//                isConvex = cnvx;
//                area = norm / 2.0;
//                normal = Direction3.Create(nrm);
//                double tvv = 0.0;
//                for(int i = 0; i < n; i++)
//                {
//                    double d = Vector3.Dot(poly[i] - centroid, nrm);
//                    tvv += d * d;
//                }
//                vv = tvv;
//            }
//        }

//        /// <summary>
//        /// Erzeugt Polylinie aus gegebenen Punkten
//        /// </summary>
//        /// <param name="close">Wenn true, wird der erste Punkt ans Ende kopiert</param>
//        /// <param name="points"></param>
//        /// <returns></returns>
//        public static PolyLine3 Create(bool close, params Point3[] points) => Create(close, points);

//        /// <summary>
//        /// Erzeugt Polylinie aus gegebenen Punkten
//        /// </summary>
//        /// <param name="close">Wenn true, wird der erste Punkt ans Ende kopiert</param>
//        /// <param name="points"></param>
//        /// <returns></returns>
//        public static PolyLine3 Create(bool close, IEnumerable<Point3> points)
//        {
//            var poly = new PolyLine3(points);
//            if(close && poly.Count > 2)
//            { poly.Add(poly[0]); }
//            return poly;
//        }
//    }
//}