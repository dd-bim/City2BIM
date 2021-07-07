using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.IFC
{
    /// <summary>
    /// collection for small functions in context with IFC processing
    /// </summary>
    public class utils
    {

        /// <summary>
        /// support function to calclue azimuth to vector
        /// </summary>
        public static double[] AzimuthToVector(double azi)
        {
            var razi = DegToRad(azi);
            return new[] { Math.Cos(razi), Math.Sin(razi) };
        }

        /// <summary>
        /// support to calc rho
        /// </summary>
        private static readonly double RevRho = Math.PI / 180.0;

        /// <summary>
        /// calc deg to rad
        /// </summary>
        private static double DegToRad(double deg) => deg * RevRho;

    }
}
