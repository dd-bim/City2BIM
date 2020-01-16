﻿using Autodesk.Revit.DB;

namespace City2RVT
{
    public static class Prop_Revit
    {
        private static Transform trafoPBP = SetRevitProjectTransformation();
        private static ElementId terrainId;

        public const double radToDeg = 180 / System.Math.PI;
        public const double feetToM = 0.3048;

        public static ElementId TerrainId { get => terrainId; set => terrainId = value; }
        public static Transform TrafoPBP { get => SetRevitProjectTransformation() /*trafoPBP*/; }

        /// <summary>
        /// Creates a Revit Transform object
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <returns>Revit transformation matrix</returns>
        private static Transform SetRevitProjectTransformation()
        {
            XYZ vectorPBP =
            new XYZ(GUI.Prop_GeoRefSettings.ProjCoord[1] / feetToM, GUI.Prop_GeoRefSettings.ProjCoord[0] / feetToM, GUI.Prop_GeoRefSettings.ProjElevation / feetToM);

            double angle = GUI.Prop_GeoRefSettings.ProjAngle / Prop_Revit.radToDeg;

            using (Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle))
            {
                Transform ttrans = Transform.CreateTranslation(-vectorPBP);
                Transform transf = trot.Multiply(ttrans);

                return transf;
            }
        }
    }
}