using Autodesk.Revit.DB;
using System;

namespace CityBIM
{
    public static class Prop_Revit
    {
        private static ElementId terrainId;
        private static ElementId pickedId;
        private static Element pickedElement;

        public const double radToDeg = 180 / System.Math.PI;
        public const double feetToM = 0.3048;

        public static ElementId TerrainId { get => terrainId; set => terrainId = value; }
        public static ElementId PickedId { get => pickedId; set => pickedId = value; }
        public static Element PickedElement { get => pickedElement; set => pickedElement = value; }

    }
}
