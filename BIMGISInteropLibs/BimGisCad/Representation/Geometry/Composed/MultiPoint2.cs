using BimGisCad.Representation.Geometry.Elementary;
using System;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad.Representation.Geometry.Composed
{
    /// <summary>
    /// Gemittelter 2D Punkt
    /// </summary>

    public class MultiPoint2
    {
        private readonly double minDistsq;
        private double count;
        private double xs;
        private double ys;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="minDist">Mindestabstand unterschiedlicher Punkte</param>
        /// <param name="point">Punkt</param>
        public MultiPoint2(double minDist, Point2 point)
        {
            minDistsq = minDist * minDist;
            count = 1.0;
            xs = point.X;
            ys = point.Y;
        }


        /// <summary>
        /// Fügt neuen Punkt hinzu, wenn innerhalb des Mindestabstandes
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Add(Point2 point)
        {
            double dx = xs / count - point.X;
            double dy = ys / count - point.Y;
            if ((dx * dx + dy * dy) < minDistsq)
            {
                xs += point.X;
                ys += point.Y;
                count += 1.0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gemitteltes X
        /// </summary>
        public double X => xs / count;

        /// <summary>
        /// Gemitteltes Y
        /// </summary>
        public double Y => ys / count;

        /// <summary>
        /// Gemittelter Punkt
        /// </summary>
        public Point2 Point => Point2.Create(X, Y);

    }
}
