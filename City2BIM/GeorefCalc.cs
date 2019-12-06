using City2BIM.GetGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM
{
    public static class GeorefCalc
    {
        /// <summary>
        /// Calculates unscaled Point out of Scale and adds global coordinate offset (if specified before)
        /// </summary>
        /// <param name="localPt">point coordinate (local, if global offset specified)</param>
        /// <param name="isGeodetic">coordinate order of input (t=YXZ, f=XYZ)</param>
        /// <param name="globalPt">global coordinate offset if specified for mathematical calculations</param>
        /// <returns>Unprojected (scale = 1.0) point for CAD/BIM representation</returns>
        public static C2BPoint CalcUnprojectedPoint(C2BPoint localPt, bool isGeodetic, C2BPoint globalPt = null)
        {
            if (globalPt == null)
                globalPt = new C2BPoint(0, 0, 0);

            //At first add lowerCorner from gml
            double xGlobalProj = localPt.X + globalPt.X;
            double yGlobalProj = localPt.Y + globalPt.Y;

            var deltaX = xGlobalProj - GeoRefSettings.ProjCoord[1];
            var deltaY = yGlobalProj - GeoRefSettings.ProjCoord[0];

            if (isGeodetic)
            {
                xGlobalProj = localPt.Y + globalPt.Y;
                yGlobalProj = localPt.X + globalPt.X;

                deltaX = xGlobalProj - GeoRefSettings.ProjCoord[0];
                deltaY = yGlobalProj - GeoRefSettings.ProjCoord[1];
            }

            var deltaXUnproj = deltaX / GeoRefSettings.ProjScale;
            var deltaYUnproj = deltaY / GeoRefSettings.ProjScale;

            var xGlobalUnproj = xGlobalProj - deltaX + deltaXUnproj;
            var yGlobalUnproj = yGlobalProj - deltaY + deltaYUnproj;

            var zGlobal = localPt.Z + globalPt.Z;

            return new C2BPoint(xGlobalUnproj, yGlobalUnproj, zGlobal);
        }
    }
}
