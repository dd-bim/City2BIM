using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Ifc4.Interfaces;

namespace IFCGeorefShared
{
    internal class Utils
    {
        internal static double AngleFromDirection(IIfcDirection direction)
        {
            return AngleFromDirection(direction.X, direction.Y);
        }
        internal static double AngleFromDirection(double X, double Y)
        {
            return Math.Atan2(X, Y) * (180 / Math.PI);
        }
        internal static double TrueNorthFromRefDirection(IIfcDirection refDirection)
        {
            return TrueNorthFromRefDirection(refDirection.X, refDirection.Y);
        }
        internal static double TrueNorthFromRefDirection(double X, double Y)
        {
            return AngleFromDirection(-Y, X);
        }
        internal const string dashLine = "--------------------------------------------------------------------------------";
        internal const string starLine = "********************************************************************************";
    }
}
