using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Representation.Geometry.Elementary;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Linear
{
    /// <summary>
    /// Löst Gleichungen mit 3 Unbekannten
    /// </summary>
    public class Solve3
    {
 
        double xx, xy, xz, yy, yz, zz;

        /// <summary>
        /// 
        /// </summary>
        public Solve3()
        {
            this.X = new double[3];
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
        public double Variance => this.N > 3 ? this.VV / (this.N - 3) : 0.0;

        /// <summary>
        /// 
        /// </summary>
        public double Condition
        {
            get
            {
                double a = Math.Abs(this.xx), b = Math.Abs(this.yy), c = Math.Abs(this.zz);
                SortAsc(ref a, ref b, ref c);
                return a != 0.0 ? c / a : double.PositiveInfinity;
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
                    this.xz = row[2];
                    this.yy = 0.0;
                    this.yz = 0.0;
                    this.zz = 0.0;
                    this.X[0] = row[3];
                    this.X[1] = 0.0;
                    this.X[2] = 0.0;
                    this.VV = 0.0;
                }
                else
                {
                    double x = row[0];
                    double y = row[1];
                    double z = row[2];
                    double d = row[3];
                    // in erste Zeile einarbeiten
                    double t = this.xx, tt;
                    this.xx = Math.Abs(row[0]) > Math.Abs(t) ? givrot(ref t, ref x) : givrot(ref x, ref t);

                    tt = (t * this.xy) + (x * y);
                    y = (t * y) - (x * this.xy);
                    this.xy = tt;

                    tt = (t * this.xz) + (x * z);
                    z = (t * z) - (x * this.xz);
                    this.xz = tt;

                    tt = (t * this.X[0]) + (x * d);
                    d = (t * d) - (x * this.X[0]);
                    this.X[0] = tt;

                    // in zweite Zeile einarbeiten
                    t = this.yy;
                    this.yy = Math.Abs(y) > Math.Abs(t) ? givrot(ref t, ref y) : givrot(ref y, ref t);

                    tt = (t * this.yz) + (y * z);
                    z = (t * z) - (y * this.yz);
                    this.yz = tt;

                    tt = (t * this.X[1]) + (y * d);
                    d = (t * d) - (y * this.X[1]);
                    this.X[1] = tt;

                    // in dritte Zeile einarbeiten
                    t = this.zz;
                    this.zz = Math.Abs(z) > Math.Abs(t) ? givrot(ref t, ref z) : givrot(ref z, ref t);

                    tt = (t * this.X[2]) + (z * d);
                    d = (t * d) - (z * this.X[2]);
                    this.X[2] = tt;

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
            this.Determinant = this.zz * this.yy * this.xx;

            // Vektor berechnen
            this.X[2] /= this.zz;
            this.X[1] = (this.X[1] - (this.yz * this.X[2])) / this.yy;
            this.X[0] = (this.X[0] - (this.xy * this.X[1]) - (this.xz * this.X[2])) / this.xx;

            return this.N > 2 && Math.Abs(this.Determinant) > TRIGTOL;
        }
    }
}