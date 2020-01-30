using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using System.Collections.Generic;
using System.Linq;

namespace City2RVT.Builder
{
    class RevitXPlanBuilder
    {
        private readonly Document doc;
        double feetToMeter = 1.0 / 0.3048;

        public RevitXPlanBuilder(Document doc)
        {
            this.doc = doc;
            //this.colors = CreateColorAsMaterial();
        }

        public void CreateStrasse(XYZ[] pointsFlst)
        {
            XYZ origin = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, feetToMeter);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            ElementId elementIdFlst = default(ElementId);

            Transaction topoTransaction = new Transaction(doc, "Strassen");
            {
                //FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
                //options.SetFailuresPreprocessor(new AxesFailure());
                //topoTransaction.SetFailureHandlingOptions(options);

                topoTransaction.Start();
                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                TopographySurface flst = TopographySurface.Create(doc, pointsFlst);
                Parameter gesamt = flst.LookupParameter("Kommentare");
                gesamt.Set("TopoGesamt");
                elementIdFlst = flst.Id;
            }
            topoTransaction.Commit();
        }
    }
}
