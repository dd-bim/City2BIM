using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
using BimGisCad.Representation.Geometry.Elementary;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.Geometry
{
    public class terrain {

        /// <summary>
        /// add point to create a unique coord list
        /// </summary>
        public static int addPoint(HashSet<Point> points, Point p)
        {
            if (!points.Contains(p))
            {
                points.Add(p);
                p.UserData = points.Count-1;
                return (int)p.UserData;
            }

            points.TryGetValue(p, out var pt);
            return (int)pt.UserData;
        }


        /// <summary>
        /// add point to list (without redundant points)
        /// </summary>
        public static int addToList(HashSet<uPoint3> pList, Point3 p)
        {
            //create new point (use list length for "point number"
            uPoint3 up = new uPoint3(pList.Count, p);

            //check if point is allready in hash set
            if (pList.TryGetValue(up, out var pOld))
            {
                //return old point number; use the number for right tin index
                return pOld.pnr;
            }
            //if this was false

            //add point to hash set
            pList.Add(up);

            //log
            LogWriter.Add(LogType.verbose, "[File reader] Point (" + up.pnr + ") set (x= " + up.X + "; y= " + up.Y + "; z= " + up.Z + ")");

            //return current point number
            return up.pnr;
        }
    }

    /// <summary>
    /// support class for point with point number
    /// </summary>
    public class uPoint3 : IEquatable<uPoint3>
    {
        #region Constructor

        /// <summary>
        /// constructor using BIMGISCAD
        /// </summary>
        public uPoint3(int pnr, Point3 point3)
        {
            this.pnr = pnr;
            this.point3 = point3;
        }
        #endregion

        #region Properties
        public int pnr { get; set; }

        /// <summary>
        /// x-value from point3
        /// </summary>
        public double X => point3.X;
        /// <summary>
        /// y-value from point3
        /// </summary>
        public double Y => point3.Y;
        /// <summary>
        /// z-value from point3
        /// </summary>
        public double Z => point3.Z;
        public Point3 point3 { get; set; }

        /// <summary>
        /// tolerance for point compare
        /// </summary>
        public const double tol = 0.0001;

        /// <summary>
        /// rounding tolerance for coordinants in hash codes
        /// </summary>
        public const int tolDigits = 3;
        #endregion

        /// <summary>
        /// check if point equals another point (under the use of tolerance
        /// </summary>
        public bool Equals(uPoint3 other)
        {
            var x = Math.Abs(this.X - other.X);
            var y = Math.Abs(this.Y - other.Y);
            var z = Math.Abs(this.Z - other.Z);

            return
                x < tol &&
                y < tol &&
                z < tol;
        }
        public override bool Equals(object obj)
        {
            return obj is uPoint3 p ? Equals(p) : false;
        }

        /// <summary>
        /// hash code to compare performant (Attention: X, Y & Z will be rounded)
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = -307843816;
            hashCode = hashCode * -1521134295 + Math.Round(X, tolDigits).GetHashCode();
            hashCode = hashCode * -1521134295 + Math.Round(Y, tolDigits).GetHashCode();
            hashCode = hashCode * -1521134295 + Math.Round(Z, tolDigits).GetHashCode();
            return hashCode;
        }

        public static bool operator ==(uPoint3 left, uPoint3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(uPoint3 left, uPoint3 right)
        {
            return !(left == right);
        }
    }
}

