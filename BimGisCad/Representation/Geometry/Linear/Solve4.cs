using System;
using System.Collections.Generic;
using System.Text;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Linear
{
    /// <summary>
    /// Löst Gleichungen mit 4 Unbekannten
    /// </summary>
    public class Solve4
    {

        double xx, xy, xz, xw, yy, yz, yw, zz, zw, ww;

        /// <summary>
        /// 
        /// </summary>
        public Solve4()
        {
            this.X = new double[4];
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
        public double Variance => this.N > 4 ? this.VV / (this.N - 4) : 0.0;

        /// <summary>
        /// 
        /// </summary>
        public double Condition
        {
            get
            {
                double a = Math.Abs(this.xx), b = Math.Abs(this.yy), c = Math.Abs(this.zz), d = Math.Abs(this.ww);
                SortAsc(ref a, ref b, ref c, ref d);
                return a != 0.0 ? d / a : double.PositiveInfinity;
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
                    this.xw = row[3];
                    this.yy = 0.0;
                    this.yz = 0.0;
                    this.yw = 0.0;
                    this.zz = 0.0;
                    this.zw = 0.0;
                    this.ww = 0.0;
                    this.X[0] = row[4];
                    this.X[1] = 0.0;
                    this.X[2] = 0.0;
                    this.X[3] = 0.0;
                    this.VV = 0.0;
                }
                else
                {
                    double x = row[0];
                    double y = row[1];
                    double z = row[2];
                    double w = row[3];
                    double d = row[4];
                    // in erste Zeile einarbeiten
                    double t = this.xx, tt;
                    this.xx = Math.Abs(row[0]) > Math.Abs(t) ? givrot(ref t, ref x) : givrot(ref x, ref t);

                    tt = (t * this.xy) + (x * y);
                    y = (t * y) - (x * this.xy);
                    this.xy = tt;

                    tt = (t * this.xz) + (x * z);
                    z = (t * z) - (x * this.xz);
                    this.xz = tt;

                    tt = (t * this.xw) + (x * w);
                    z = (t * w) - (x * this.xw);
                    this.xw = tt;

                    tt = (t * this.X[0]) + (x * d);
                    d = (t * d) - (x * this.X[0]);
                    this.X[0] = tt;

                    // in zweite Zeile einarbeiten
                    t = this.yy;
                    this.yy = Math.Abs(y) > Math.Abs(t) ? givrot(ref t, ref y) : givrot(ref y, ref t);

                    tt = (t * this.yz) + (y * z);
                    z = (t * z) - (y * this.yz);
                    this.yz = tt;

                    tt = (t * this.yw) + (y * w);
                    z = (t * w) - (y * this.yw);
                    this.yw = tt;

                    tt = (t * this.X[1]) + (y * d);
                    d = (t * d) - (y * this.X[1]);
                    this.X[1] = tt;

                    // in dritte Zeile einarbeiten
                    t = this.zz;
                    this.zz = Math.Abs(z) > Math.Abs(t) ? givrot(ref t, ref z) : givrot(ref z, ref t);

                    tt = (t * this.zw) + (z * w);
                    d = (t * w) - (z * this.zw);
                    this.X[2] = tt;

                    tt = (t * this.X[2]) + (z * d);
                    d = (t * d) - (z * this.X[2]);
                    this.X[2] = tt;

                    // in vierte Zeile einarbeiten
                    t = this.ww;
                    this.ww = Math.Abs(w) > Math.Abs(t) ? givrot(ref t, ref w) : givrot(ref w, ref t);

                    tt = (t * this.X[3]) + (w * d);
                    d = (t * d) - (w * this.X[3]);
                    this.X[3] = tt;

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
            this.Determinant = this.ww * this.zz * this.yy * this.xx;
 
            // Vektor berechnen
            this.X[3] /= this.ww;
            this.X[2] = (this.X[2] - (this.zw * this.X[3])) / this.zz;
            this.X[1] = (this.X[1] - (this.yz * this.X[2]) - (this.yw * this.X[3])) / this.yy;
            this.X[0] = (this.X[0] - (this.xy * this.X[1]) - (this.xz * this.X[2]) - (this.xw * this.X[3])) / this.xx;

            return this.N > 3 && Math.Abs(this.Determinant) > TRIGTOL;
        }
    }
}