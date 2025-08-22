using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.DXF
{
    internal class InterpolationHelper
    {
        public static LineString InterpolateArc(DxfArc Arc, float allowedTolerance = 0.1f)
        {
            return InterpolateArc(Arc.Center, Arc.Radius, allowedTolerance, Arc.StartAngle, Arc.EndAngle);
        }
        public static LineString InterpolateArc(DxfCircle Circle, float allowedTolerance = 0.1f)
        {
            return InterpolateArc(Circle.Center, Circle.Radius, allowedTolerance);
        }
        /// <summary>
        /// Interpolates an arc as a LineString based on the center, radius, and angles.
        /// </summary>
        /// <param name="Center"></param>
        /// <param name="Radius"></param>
        /// <param name="allowedTolerance">Maximum Distance between Polyline and perfect Circle</param>
        /// <param name="StartAngle"></param>
        /// <param name="EndAngle"></param>
        /// <returns></returns>
        public static LineString InterpolateArc(DxfPoint Center,double Radius, float allowedTolerance = 0.1f, double StartAngle = 0, double EndAngle = 360)
        {
            //Calc curvature dependent delta:
            double r_ = Radius+ allowedTolerance; //increase radius to minimize distance between Circle and Polyline (Polyline interecting the circle)
            double delta = 2 * r_ * Math.Acos(1 - (allowedTolerance * 2 / r_)); // TODO increase Radius to minimize distance between Circle and Polyline (Polyline interecting the circle)
            double arclength = ((EndAngle-StartAngle) * Math.PI / 180.0) * r_;
            int numSegments = (int)Math.Floor(arclength / delta);
            delta = (EndAngle - StartAngle) / numSegments; //angular delta
            CoordinateZ[] coords = new CoordinateZ[numSegments+1];
            for (int i = 0; i <= numSegments; i++)
            {  
                double angle = Math.PI * (StartAngle + i * delta) / 180.0;
                double x = Center.X + Radius * Math.Cos(angle);
                double y = Center.Y + Radius * Math.Sin(angle);
                coords[i] = new CoordinateZ(x, y,Center.Z);
            }
            return new LineString(coords);
        }
    }
}
