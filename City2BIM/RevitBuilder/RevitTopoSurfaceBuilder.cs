using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using City2BIM.GetGeometry;
using City2BIM.RevitCommands;

namespace City2BIM.RevitBuilder
{
    internal class RevitTopoSurfaceBuilder
    {
        private Transform trafoPBP = Revit_Prop.TrafoPBP;
        private Document doc;

        public RevitTopoSurfaceBuilder(Document doc)
        {
            this.doc = doc;
        }

        public void CreateDTM(List<C2BPoint> terrainPoints)
        {
            var revDTMpts = new List<XYZ>();

            foreach(var pt in terrainPoints)
            {
                //Transformation for revit
                var ptCalc = new GeorefCalc();
                var unprojectedPt = ptCalc.CalcUnprojectedPoint(pt, true);

                var revitPt = unprojectedPt / Prop.feetToM;

                //Creation of Revit point
                var revitXYZ = new XYZ(revitPt.Y, revitPt.X, revitPt.Z);

                //Transform global coordinate to Revit project coordinate system (system of project base point)
                var revTransXYZ = trafoPBP.OfPoint(revitXYZ);

                revDTMpts.Add(revTransXYZ);
            }

            using(Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                t.Start();

                var surface = TopographySurface.Create(doc, revDTMpts);
                Revit_Prop.TerrainId = surface.Id;

                surface.Pinned = true;

                t.Commit();
            }
        }
    }
}