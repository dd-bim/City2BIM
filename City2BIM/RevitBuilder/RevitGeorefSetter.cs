using Autodesk.Revit.DB;
using City2BIM.GetGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.RevitBuilder
{
    public static class RevitGeorefSetter
    {
        private static Transform trafoPBP = SetRevitProjectTransformation();

        public static Transform TrafoPBP { get => trafoPBP; }

        /// <summary>
        /// Creates a Revit Transform object
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <returns>Revit transformation matrix</returns>
        private static Transform SetRevitProjectTransformation()
        {
            XYZ vectorPBP =
            new XYZ(GeoRefSettings.ProjCoord[1] / Prop.feetToM, GeoRefSettings.ProjCoord[0] / Prop.feetToM, GeoRefSettings.ProjElevation / Prop.feetToM);

            double angle = GeoRefSettings.ProjAngle / Prop.radToDeg; 

            Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle);
            Transform ttrans = Transform.CreateTranslation(-vectorPBP);
            Transform transf = trot.Multiply(ttrans);

            return transf;
        }
    }
}
