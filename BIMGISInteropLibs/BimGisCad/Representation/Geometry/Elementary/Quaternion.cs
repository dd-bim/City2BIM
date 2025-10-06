using System;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    /// 
    /// </summary>
    public class Quaternion
    {
        #region Fields

        /// <summary>
        /// Keine Rotation
        /// </summary>
        public static readonly Quaternion Zero = new Quaternion(1.0, 0.0, 0.0, 0.0);

        #endregion Fields

        #region Constructors

        private Quaternion(double s, double x, double y, double z)
        {
            this.S = s;
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        private Quaternion(Direction3 xAxis, Direction3 yAxis, Direction3 zAxis)
        {
            this.S = Math.Sqrt(Math.Max(0.0, 1.0 + xAxis.X + yAxis.Y + zAxis.Z)) / 2.0;
            double x = Math.Sqrt(Math.Max(0.0, 1.0 + xAxis.X - yAxis.Y - zAxis.Z)) / 2.0;
            double y = Math.Sqrt(Math.Max(0.0, 1.0 - xAxis.X + yAxis.Y - zAxis.Z)) / 2.0;
            double z = Math.Sqrt(Math.Max(0.0, 1.0 - xAxis.X - yAxis.Y + zAxis.Z)) / 2.0;
            this.X = yAxis.Z < zAxis.Y ? -x : x;
            this.Y = zAxis.X < xAxis.Z ? -y : y;
            this.Z = xAxis.Y < yAxis.X ? -z : z;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public double S { get; set;}

        /// <summary>
        /// 
        /// </summary>
        public double X { get; set;}

        /// <summary>
        /// 
        /// </summary>
        public double Y { get; set;}

        /// <summary>
        /// 
        /// </summary>
        public double Z { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Rotiert vektor mit Quaternion
        /// </summary>
        /// <param name="q"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            //Vec3 vv = cross(q.v, v);
            //return b + (2.0 * ((q.s * vv) + cross(q.v, vv)));
            double
                abX = (q.Y * v.Z) - (q.Z * v.Y),
                abY = (q.Z * v.X) - (q.X * v.Z),
                abZ = (q.X * v.Y) - (q.Y * v.X);
            return Vector3.Create(
                v.X + (2.0 * ((q.S * abX) + (q.Y * abZ) - (q.Z * abY))),
                v.Y + (2.0 * ((q.S * abY) + (q.Z * abX) - (q.X * abZ))),
                v.Z + (2.0 * ((q.S * abZ) + (q.X * abY) - (q.Y * abX)))
            );
        }

        /// <summary>
        /// Rotiert vektor mit Quaternion
        /// </summary>
        /// <param name="q"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Direction3 operator *(Quaternion q, Direction3 v)
        {
            //Vec3 vv = cross(q.v, v);
            //return b + (2.0 * ((q.s * vv) + cross(q.v, vv)));
            double
                abX = (q.Y * v.Z) - (q.Z * v.Y),
                abY = (q.Z * v.X) - (q.X * v.Z),
                abZ = (q.X * v.Y) - (q.Y * v.X);
            return Direction3.Create(
                v.X + (2.0 * ((q.S * abX) + (q.Y * abZ) - (q.Z * abY))),
                v.Y + (2.0 * ((q.S * abY) + (q.Z * abX) - (q.X * abZ))),
                v.Z + (2.0 * ((q.S * abZ) + (q.X * abY) - (q.Y * abX)))
            );
        }

        /// <summary>
        /// Transformiert Punkt (erst rotation dann translation)
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="translation"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point3 Transform(Quaternion rotation, Vector3 translation, Point3 point)
        {
            double
                 abX = (rotation.Y * point.Z) - (rotation.Z * point.Y),
                 abY = (rotation.Z * point.X) - (rotation.X * point.Z),
                 abZ = (rotation.X * point.Y) - (rotation.Y * point.X);
            return Point3.Create(
                translation.X + point.X + (2.0 * ((rotation.S * abX) + (rotation.Y * abZ) - (rotation.Z * abY))),
                translation.Y + point.Y + (2.0 * ((rotation.S * abY) + (rotation.Z * abX) - (rotation.X * abZ))),
                translation.Z + point.Z + (2.0 * ((rotation.S * abZ) + (rotation.X * abY) - (rotation.Y * abX)))
            );
        }

        /// <summary>
        /// Multiplikation
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            double s = (-q1.X * q2.X) - (q1.Y * q2.Y) - (q1.Z * q2.Z) + (q1.S * q2.S);
            double x = (q1.X * q2.S) + (q1.Y * q2.Z) - (q1.Z * q2.Y) + (q1.S * q2.X);
            double y = (-q1.X * q2.Z) + (q1.Y * q2.S) + (q1.Z * q2.X) + (q1.S * q2.Y);
            double z = (q1.X * q2.Y) - (q1.Y * q2.X) + (q1.Z * q2.S) + (q1.S * q2.Z);
            return new Quaternion(s, x, y, z);
        }
 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Direction3 XAxis(Quaternion q)
        {
            double yy = q.Y * q.Y;
            double zz = q.Z * q.Z;
            double s = 2.0 / ((q.S * q.S) + (q.X * q.X) + yy + zz); // normiert!
            return Direction3.Create(
                1.0 - (s * (yy + zz)),
                s * ((q.S * q.Z) + (q.X * q.Y)),
                s * ((q.X * q.Z) - (q.S * q.Y)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Direction3 YAxis(Quaternion q)
        {
            double xx = q.X * q.X;
            double zz = q.Z * q.Z;
            double s = 2.0 / ((q.S * q.S) + xx + (q.Y * q.Y) + zz); // normiert!
            return Direction3.Create(
                s * ((q.Y * q.X) - (q.S * q.Z)),
                1.0 - (s * (zz + xx)),
                s * ((q.S * q.X) + (q.Y * q.Z)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Direction3 ZAxis(Quaternion q)
        {
            double xx = q.X * q.X;
            double yy = q.Y * q.Y;
            double s = 2.0 / ((q.S * q.S) + xx + yy + (q.Z * q.Z)); // normiert!
            return Direction3.Create(
                s * ((q.S * q.Y) + (q.Z * q.X)),
                s * ((q.Z * q.Y) - (q.S * q.X)),
                1.0 - (s * (xx + yy)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Quaternion Normalized(Quaternion q)
        {
            double norm = Math.Sqrt((q.S * q.S) + (q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z));
            return new Quaternion(q.S / norm, q.X / norm, q.Y / norm, q.Z / norm);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Quaternion Conj(Quaternion q) => new Quaternion(q.S, -q.X, -q.Y, -q.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="translation"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 Transform(Quaternion rotation, Vector3 translation, Vector3 vector) => (rotation * vector) + translation;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="zAxis"></param>
        /// <returns></returns>
        public static Quaternion Create(Direction3 xAxis, Direction3 yAxis, Direction3 zAxis) => new Quaternion(xAxis, yAxis, zAxis);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Quaternion Create(double s, double x, double y, double z) => new Quaternion(s, x, y, z);

        #endregion Methods
    }
}