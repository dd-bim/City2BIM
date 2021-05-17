using System;
using System.Collections.Generic;
using System.Globalization;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    /// Hilfsklasse mit allgemeinen Methoden
    /// </summary>
    public static class Common
    {
        #region Fields

        /// <summary>
        ///  Double Machine Epsilon
        /// </summary>
        public static readonly double EPS = 1.0 / (1L << 52);

        /// <summary>
        ///  1 / (Wurzel 3)
        /// </summary>
        public static readonly double RSQRT3 = 1.0 / Math.Sqrt(3.0);

        /// <summary>
        ///  kleinstmöglicher Trig/Det Wert
        /// </summary>
        public static readonly double TRIGTOL = 1.0e-11;

        /// <summary>
        /// Quadrierter kleinstmöglicher Trig/Det Wert
        /// </summary>
        public static readonly double TRIGTOL_SQUARED = TRIGTOL * TRIGTOL;

        /// <summary>
        ///  Kleinstmögliche Strecke
        /// </summary>
        public const double MINDIST = 1.0e-4;

        /// <summary>
        ///  Quadrierte Kleinstmögliche Strecke
        /// </summary>
        public const double MINDIST_SQUARED = MINDIST * MINDIST;

        //public static readonly double MINDIST_SQUARED2 = 2.0 * MINDIST_SQUARED;

        /// <summary>
        /// Faktor Grad - Rad
        /// </summary>
        public static readonly double RHODEG = 180.0 / Math.PI;

        /// <summary>
        /// Faktor Gon - Rad
        /// </summary>
        public static readonly double RHOGON = 200.0 / Math.PI;


        #endregion Fields

        #region Methods

        // Hilfsfunktion zur Berechnung der Givensrotation
        internal static double givrot(ref double c, ref double s)
        {
            if (s == 0.0)
            {
                s = 1.0;
                c = 0.0;
                return 0.0;
            }
            double t1 = c / s;
            double t2 = Math.Sqrt(1d + (t1 * t1));
            double r = s * t2;
            s = 1d / t2;
            c = s * t1;
            return r;
        }

        /// <summary>
        /// Prüft ob eine Norm gültig ist (Not NaN und >= MinDist)
        /// </summary>
        /// <param name="norm"></param>
        /// <param name="mindist">kleinstmögliche Strecke</param>
        /// <returns></returns>
        public static bool IsValidNorm(double norm, double mindist = MINDIST)
        {
            return !double.IsNaN(norm) && norm >= mindist;
        }

        /// <summary>
        /// Prüft ob eine quadrierte Norm gültig ist (Not NaN und >= MinDist)
        /// </summary>
        /// <param name="norm2"></param>
        /// <param name="mindistSquared">quadrierte kleinstmögliche Strecke</param>
        /// <returns></returns>
        public static bool IsValidNormSquared(double norm2, double mindistSquared = MINDIST_SQUARED)
        {
            return !double.IsNaN(norm2) && norm2 >= mindistSquared;
        }

        /// <summary>
        /// Abstand ist kleiner als kleinstmögliche Strecke
        /// </summary>
        /// <param name="dist"></param>
        /// <param name="mindist">kleinstmögliche Strecke</param>
        /// <returns></returns>
        public static bool IsNearlyZero(double dist, double mindist = MINDIST)
        {
            return -MINDIST < dist && dist < mindist;
        }

        /// <summary>
        /// Quadrierter Abstand ist kleiner als quadrierte kleinstmögliche Strecke
        /// </summary>
        /// <param name="dist"></param>
        /// <param name="mindistSquared">quadrierte kleinstmögliche Strecke</param>
        /// <returns></returns>
        public static bool IsNearlyZeroSquared(double dist, double mindistSquared = MINDIST_SQUARED)
        {
            return dist < mindistSquared;
        }

        /// <summary>
        ///  Sichere Normberechnung, vermeidet unnötige Überläufe
        /// </summary>
        /// <param name="x"> Kathete </param>
        /// <param name="y"> Kathete </param>
        public static double Norm(double x, double y)
        {
            double ax = Math.Abs(x);
            double ay = Math.Abs(y);
            SortDesc(ref ax, ref ay);
            if (ax < EPS)
            { return 0.0; }
            ay /= ax;
            return ax * Math.Sqrt((ay * ay) + 1.0);
        }

        /// <summary>
        ///  Sichere Normberechnung, vermeidet unnötige Überläufe
        /// </summary>
        /// <param name="x"> Kathete </param>
        /// <param name="y"> Kathete </param>
        /// <param name="z"> Kathete </param>
        public static double Norm(double x, double y, double z)
        {
            double ax = Math.Abs(x);
            double ay = Math.Abs(y);
            double az = Math.Abs(z);
            SortDesc(ref ax, ref ay, ref az);
            if (ax < EPS)
            { return 0.0; }
            ay /= ax;
            az /= ax;
            return ax * Math.Sqrt((az * az) + (ay * ay) + 1.0);
        }

        /// <summary>
        /// Erzeugt ein DoubleArray aus einem StringArray
        /// </summary>
        /// <param name="strings">Array mit Strings der Zahlen</param>
        /// <param name="doubles">Geparste Strings</param>
        /// <param name="startIndex">Position der ersten Zahl in strings</param>
        /// <param name="length">Länge des DoubleArrays</param>
        /// <returns>Erfolgreich geparst?</returns>
        public static bool DoubleTryParse(IList<string> strings, out double[] doubles, int startIndex = 0, int length = -1)
        {
            length = length < 0 ? strings.Count - startIndex : length;
            doubles = null;
            if (strings.Count >= (length - startIndex))
            {
                doubles = new double[length];
                double val;
                for (int i = 0, p = startIndex; i < length; i++, p++)
                {
                    if (double.TryParse(strings[p], NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                    {
                        doubles[i] = val;
                    }
                    else
                    { return false; }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sortiert zwei Zahlen aufsteigend
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void SortAsc(ref double a, ref double b)
        {
            if (a > b)
            {
                double t = a;
                a = b;
                b = t;
            }
        }

        /// <summary>
        /// Sortiert zwei Zahlen absteigend
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void SortDesc(ref double a, ref double b)
        {
            if (a < b)
            {
                double t = a;
                a = b;
                b = t;
            }
        }

        /// <summary>
        /// Sortiert 3 Zahlen aufsteigend
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public static void SortAsc(ref double a, ref double b, ref double c)
        {
            SortAsc(ref a, ref b);
            SortAsc(ref b, ref c);
            SortAsc(ref a, ref b);
        }

        /// <summary>
        /// Sortiert 3 Zahlen absteigend
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public static void SortDesc(ref double a, ref double b, ref double c)
        {
            SortDesc(ref a, ref b);
            SortDesc(ref b, ref c);
            SortDesc(ref a, ref b);
        }

        /// <summary>
        /// Sortiert 4 Zahlen aufsteigend
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public static void SortAsc(ref double a, ref double b, ref double c, ref double d)
        {
            SortAsc(ref a, ref b);
            SortAsc(ref b, ref c);
            SortAsc(ref c, ref d);
            SortAsc(ref b, ref c);
            SortAsc(ref a, ref b);
        }

        /// <summary>
        /// Rechnet Grad Wert in Rad um
        /// <param name="deg"></param>
        /// </summary>
        public static double Deg2Rad(double deg) => deg / RHODEG;

        /// <summary>
        /// Rechnet Rad Wert in Grad um
        /// <param name="rad"></param>
        /// </summary>
        public static double Rad2Deg(double rad) => rad * RHODEG;

        /// <summary>
        /// Rechnet Gon Wert in Rad um
        /// <param name="gon"></param>
        /// </summary>
        public static double Gon2Rad(double gon) => gon / RHOGON;

        /// <summary>
        /// Rechnet Rad Wert in Gon um
        /// <param name="rad"></param>
        /// </summary>
        public static double Rad2Gon(double rad) => rad * RHOGON;

        ///// <summary>
        ///// Rechnet Grad Wert in Rad um
        ///// <param name="deg">Grad</param>
        ///// <param name="min">Minute</param>
        ///// <param name="sec">Sekunde</param>
        //public static double DMS2Rad(double deg, double min, double sec)
        //{

        //}

        #endregion Methods
    }
}