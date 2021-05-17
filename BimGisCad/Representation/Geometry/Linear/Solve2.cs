using System;
using System.Collections.Generic;
using System.Text;
using static BimGisCad.Representation.Geometry.Elementary.Common;


namespace BimGisCad.Representation.Geometry.Linear
{
    /// <summary>
    /// Löst Gleichungen mit 2 Unbekannten
    /// </summary>

    public class Solve2
    {

        double xx, xy, yy;

        /// <summary>
        /// 
        /// </summary>
        public Solve2()
        {
            this.X = new double[2];
            this.N = 0;
            this.Determinant = double.NaN;
        }

        /// <summary>
        /// 
        /// </summary>
        public double VV { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int N { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public double[] X { get; }

        /// <summary>
        /// 
        /// </summary>
        public double Determinant { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public double Variance => this.N > 2 ? this.VV / (this.N - 2) : 0.0;

        /// <summary>
        /// 
        /// </summary>
        public double Condition
        {
            get
            {
                double a = Math.Abs(this.xx), b = Math.Abs(this.yy);
                SortAsc(ref a, ref b);
                return a != 0.0 ? b / a : double.PositiveInfinity;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        public void AddRows(double[][] rows)
        {
            foreach(double[] row in rows)
            {
                if(this.N == 0)
                {
                    this.xx = row[0];
                    this.xy = row[1];
                    this.yy = 0.0;
                    this.X[0] = row[2];
                    this.X[1] = 0.0;
                    this.VV = 0.0;
                }
                else
                {
                    double x = row[0];
                    double y = row[1];
                    double d = row[2];
                    // in erste Zeile einarbeiten
                    double t = this.xx, tt;
                    this.xx = Math.Abs(row[0]) > Math.Abs(t) ? givrot(ref t, ref x) : givrot(ref x, ref t);

                    tt = (t * this.xy) + (x * y);
                    y = (t * y) - (x * this.xy);
                    this.xy = tt;

                    tt = (t * this.X[0]) + (x * d);
                    d = (t * d) - (x * this.X[0]);
                    this.X[0] = tt;

                    // in zweite Zeile einarbeiten
                    t = this.yy;
                    this.yy = Math.Abs(y) > Math.Abs(t) ? givrot(ref t, ref y) : givrot(ref y, ref t);

                    tt = (t * this.X[1]) + (y * d);
                    d = (t * d) - (y * this.X[1]);
                    this.X[1] = tt;

                    this.VV += d * d;
                }
                this.N++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Solve()
        {
            this.Determinant = this.yy * this.xx;

            // Vektor berechnen
            this.X[1] /= this.yy;
            this.X[0] = (this.X[0] - (this.xy * this.X[1])) / this.xx;

            return this.N > 1 && Math.Abs(this.Determinant) > TRIGTOL;
        }
    }
}