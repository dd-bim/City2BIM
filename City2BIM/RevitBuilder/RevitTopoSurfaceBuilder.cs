using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using City2BIM.GetGeometry;

namespace City2BIM.RevitBuilder
{
    internal class RevitTopoSurfaceBuilder
    {
        //private City2BIM.GetGeometry.C2BPoint gmlCorner;
        private Transform revitPBP;
        private Document doc;

        public RevitTopoSurfaceBuilder(Document doc)
        {
            this.doc = doc;
            this.revitPBP = GetRevitProjectLocation(doc);
        }

        private Transform GetRevitProjectLocation(Document doc)
        {
            ProjectLocation proj = doc.ActiveProjectLocation;
            ProjectPosition projPos = proj.GetProjectPosition(Autodesk.Revit.DB.XYZ.Zero);

            double angle = projPos.Angle;
            double elevation = projPos.Elevation;
            double easting = projPos.EastWest;
            double northing = projPos.NorthSouth;

            Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle);
            var vector = new Autodesk.Revit.DB.XYZ(easting, northing, elevation);
            Transform ttrans = Transform.CreateTranslation(-vector);
            Transform transf = trot.Multiply(ttrans);

            return transf;
        }

        private XYZ TransformPointForRevit(C2BPoint terrainPt)
        {
            //Muiltiplication with feet factor (neccessary because of feet in Revit database)
            var xFeet = terrainPt.X * 3.28084;
            var yFeet = terrainPt.Y * 3.28084;
            var zFeet = terrainPt.Z * 3.28084;

            //Creation of Revit point
            var revitXYZ = new XYZ(xFeet, yFeet, zFeet);

            //Transform global coordinate to Revit project coordinate system (system of project base point)
            var revTransXYZ = revitPBP.OfPoint(revitXYZ);

            return revTransXYZ;
        }

        public void CreateDTM(List<C2BPoint> terrainPoints)
        {
            var revDTMpts = new List<XYZ>();

            foreach(var pt in terrainPoints)
            {
                revDTMpts.Add(TransformPointForRevit(pt));
            }

            using(Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                t.Start();

                var surface = TopographySurface.Create(doc, revDTMpts);

                surface.Pinned = true;

                t.Commit();
            }
        }
    }
}