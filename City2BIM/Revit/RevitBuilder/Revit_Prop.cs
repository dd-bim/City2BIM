using Autodesk.Revit.DB;
using City2BIM.GetGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.RevitBuilder
{
    public static class Revit_Prop
    {
        private static Transform trafoPBP = SetRevitProjectTransformation();
        private static ElementId terrainId;

        public const double radToDeg = 180 / System.Math.PI;
        public const double feetToM = 0.3048;

        public static ElementId TerrainId { get => terrainId; set => terrainId = value; }
        public static Transform TrafoPBP { get => trafoPBP; }

        /// <summary>
        /// Creates a Revit Transform object
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <returns>Revit transformation matrix</returns>
        private static Transform SetRevitProjectTransformation()
        {
            XYZ vectorPBP =
            new XYZ(GeoRefSettings.ProjCoord[1] / feetToM, GeoRefSettings.ProjCoord[0] / feetToM, GeoRefSettings.ProjElevation / feetToM);

            double angle = GeoRefSettings.ProjAngle / Revit_Prop.radToDeg;

            using (Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle))
            {
                Transform ttrans = Transform.CreateTranslation(-vectorPBP);
                Transform transf = trot.Multiply(ttrans);

                return transf;
            }
        }
    }
}
