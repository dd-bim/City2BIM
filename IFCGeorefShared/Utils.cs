using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Ifc;
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
        internal static T RefDirectionFromTrueNorth<T>(IfcStore model, IIfcDirection trueNorth) where T : Xbim.Common.IInstantiableEntity
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (trueNorth == null) throw new ArgumentNullException(nameof(trueNorth));

            return model.Instances.New<T>(d =>
            {
                if (d is IIfcDirection refDir)
                {
                    refDir.SetXYZ(trueNorth.Y, -trueNorth.X, 0.0);
                }
                else
                {
                    throw new ArgumentException($"Type {typeof(T).Name} is not compatible with IIfcDirection");
                }
            });
        }
        internal static T TrueNorthFromRefDirection<T>(IfcStore model, IIfcDirection refDirection) where T : Xbim.Common.IInstantiableEntity
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (refDirection == null) throw new ArgumentNullException(nameof(refDirection));
            return model.Instances.New<T>(d =>
            {
                if (d is IIfcDirection trueNorth)
                {
                    trueNorth.SetXYZ(-refDirection.Y, refDirection.X, 0.0);
                }
                else
                {
                    throw new ArgumentException($"Type {typeof(T).Name} is not compatible with IIfcDirection");
                }
            });
        }
        internal const string dashLine = "--------------------------------------------------------------------------------";
        internal const string starLine = "********************************************************************************";
    }
}
