using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
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

        public Line CreateLineString(string[] koordWerte, double R, Transform transf, double zOffset)
        {
            double xStart = Convert.ToDouble(koordWerte[0], System.Globalization.CultureInfo.InvariantCulture);
            double xStartMeter = xStart * feetToMeter;
            double xStartMeterRedu = xStartMeter / R;
            double yStart = Convert.ToDouble(koordWerte[1], System.Globalization.CultureInfo.InvariantCulture);
            double yStartMeter = yStart * feetToMeter;
            double yStartMeterRedu = yStartMeter / R;
            double zStart = zOffset;
            double zStartMeter = zStart * feetToMeter;

            double xEnd = Convert.ToDouble(koordWerte[2], System.Globalization.CultureInfo.InvariantCulture);
            double xEndMeter = xEnd * feetToMeter;
            double xEndMeterRedu = xEndMeter / R;
            double yEnd = Convert.ToDouble(koordWerte[3], System.Globalization.CultureInfo.InvariantCulture);
            double yEndMeter = yEnd * feetToMeter;
            double yEndMeterRedu = yEndMeter / R;
            double zEnd = zOffset;
            double zEndMeter = zEnd * feetToMeter;

            XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
            XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

            XYZ transfStartPoint = transf.OfPoint(startPoint);
            XYZ transfEndPoint = transf.OfPoint(endPoint);

            Line lineStrasse = Line.CreateBound(transfStartPoint, transfEndPoint);

            return lineStrasse;
        }

        public Line CreateLineRing(string[] koordWerte, double R, Transform transf, int iSplit, double zOffset)
        {
            double xStart = Convert.ToDouble(koordWerte[iSplit], System.Globalization.CultureInfo.InvariantCulture);
            double xStartMeter = xStart * feetToMeter;
            double xStartMeterRedu = xStartMeter / R;
            double yStart = Convert.ToDouble(koordWerte[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
            double yStartMeter = yStart * feetToMeter;
            double yStartMeterRedu = yStartMeter / R;
            double zStart = zOffset;
            double zStartMeter = zStart * feetToMeter;

            double xEnd = Convert.ToDouble(koordWerte[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
            double xEndMeter = xEnd * feetToMeter;
            double xEndMeterRedu = xEndMeter / R;
            double yEnd = Convert.ToDouble(koordWerte[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
            double yEndMeter = yEnd * feetToMeter;
            double yEndMeterRedu = yEndMeter / R;
            double zEnd = zOffset;
            double zEndMeter = zEnd * feetToMeter;

            XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
            XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

            XYZ tStartPoint = transf.OfPoint(startPoint);
            XYZ tEndPoint = transf.OfPoint(endPoint);

            Line lineClIndu = default(Line);

            if (tStartPoint.DistanceTo(tEndPoint) > 0)
            {
                lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);
            }


            return lineClIndu;
        }

        public Dictionary<string, ElementId> CreateMaterial()
        {
            #region material
            var transparentMaterialId = default(ElementId);
            var strassenVerkehrsFlaecheMaterialId = default(ElementId);
            var interiorMaterialId = default(ElementId);
            var defaultMaterialId = default(ElementId);
            var ueberbaubareGrundstuecksFlaecheMaterialId = default(ElementId);
            var gewaesserFlaecheId = default(ElementId);
            var bereichMaterialId = default(ElementId);
            var planMaterialId = default(ElementId);
            var bauGebietsTeilFlaecheMaterialId = default(ElementId);
            var gemeinBedarfsFlaecheMaterialId = default(ElementId);
            var kennzeichnungsFlaecheMaterialId = default(ElementId);
            var erhaltungsBereichFlaecheMaterialId = default(ElementId);
            var colorList = new Dictionary<string, ElementId>();

            Transaction tMaterial = new Transaction(doc, "Creates Material");
            {
                tMaterial.Start();

                transparentMaterialId = Material.Create(doc, "transparent");
                Material referenceMaterial = doc.GetElement(transparentMaterialId) as Material;
                referenceMaterial.Transparency = 100;
                colorList.Add("transparent", transparentMaterialId);


                strassenVerkehrsFlaecheMaterialId = Material.Create(doc, "strassenVerkehrsFlaeche");
                Material strassenVerkehrsFlaecheMaterial = doc.GetElement(strassenVerkehrsFlaecheMaterialId) as Material;
                strassenVerkehrsFlaecheMaterial.Color = new Color(240, 230, 140);
                strassenVerkehrsFlaecheMaterial.Transparency = 75;
                colorList.Add("BP_StrassenVerkehrsFlaeche", strassenVerkehrsFlaecheMaterialId);

                gewaesserFlaecheId = Material.Create(doc, "gewaesserFlaeche");
                Material gewaesserFlaeche = doc.GetElement(gewaesserFlaecheId) as Material;
                gewaesserFlaeche.Color = new Color(030, 144, 255);
                colorList.Add("BP_GewaesserFlaeche", gewaesserFlaecheId);

                ueberbaubareGrundstuecksFlaecheMaterialId = Material.Create(doc, "ueberbaubareGrundstuecksFlaeche");
                Material ueberbaubareGrundstuecksFlaecheMaterial = doc.GetElement(ueberbaubareGrundstuecksFlaecheMaterialId) as Material;
                ueberbaubareGrundstuecksFlaecheMaterial.Color = new Color(160, 082, 045);
                //ueberbaubareGrundstuecksFlaecheMaterial.SurfaceForegroundPatternId = new SurfaceForegroundPatternId
                colorList.Add("BP_UeberbaubareGrundstuecksFlaeche", ueberbaubareGrundstuecksFlaecheMaterialId);

                interiorMaterialId = Material.Create(doc, "interior");
                Material interiorMaterial = doc.GetElement(interiorMaterialId) as Material;
                //interiorMaterial.Color = new Color(240, 230, 140);
                interiorMaterial.Transparency = 100;
                colorList.Add("interior", interiorMaterialId);


                defaultMaterialId = Material.Create(doc, "default");
                Material defaultMaterial = doc.GetElement(defaultMaterialId) as Material;
                defaultMaterial.Color = new Color(100, 100, 100);
                colorList.Add("default", defaultMaterialId);

                bereichMaterialId = Material.Create(doc, "bereich");
                Material bereichMaterial = doc.GetElement(bereichMaterialId) as Material;
                bereichMaterial.Transparency = 100;
                colorList.Add("BP_Bereich", bereichMaterialId);

                planMaterialId = Material.Create(doc, "plan");
                Material planMaterial = doc.GetElement(planMaterialId) as Material;
                planMaterial.Transparency = 100;
                colorList.Add("BP_Plan", planMaterialId);

                bauGebietsTeilFlaecheMaterialId = Material.Create(doc, "BaugebietsTeilFlaeche");
                Material bauGebietsTeilFlaecheMaterial = doc.GetElement(bauGebietsTeilFlaecheMaterialId) as Material;
                bauGebietsTeilFlaecheMaterial.Color = new Color(233, 150, 122);
                colorList.Add("BP_BaugebietsTeilFlaeche", bauGebietsTeilFlaecheMaterialId);

                gemeinBedarfsFlaecheMaterialId = Material.Create(doc, "GemeinbedarfsFlaeche");
                Material gemeinBedarfsFlaecheMaterial = doc.GetElement(gemeinBedarfsFlaecheMaterialId) as Material;
                gemeinBedarfsFlaecheMaterial.Color = new Color(255, 106, 106);
                colorList.Add("BP_GemeinbedarfsFlaeche", gemeinBedarfsFlaecheMaterialId);

                kennzeichnungsFlaecheMaterialId = Material.Create(doc, "KennzeichnungsFlaeche");
                Material kennzeichnungsFlaecheMaterial = doc.GetElement(kennzeichnungsFlaecheMaterialId) as Material;
                kennzeichnungsFlaecheMaterial.Color = new Color(110, 139, 061);
                kennzeichnungsFlaecheMaterial.Transparency = 50;
                colorList.Add("BP_KennzeichnungsFlaeche", kennzeichnungsFlaecheMaterialId);

                erhaltungsBereichFlaecheMaterialId = Material.Create(doc, "ErhaltungsBereichFlaeche");
                Material erhaltungsBereichFlaecheMaterial = doc.GetElement(erhaltungsBereichFlaecheMaterialId) as Material;
                erhaltungsBereichFlaecheMaterial.Color = new Color(0, 255, 0);
                //erhaltungsBereichFlaecheMaterial.Transparency = 50;
                colorList.Add("BP_ErhaltungsBereichFlaeche", erhaltungsBereichFlaecheMaterialId);

                ////Create a new property set that can be used by this material
                //StructuralAsset strucAsset = new StructuralAsset("My Property Set", StructuralAssetClass.Concrete);
                //strucAsset.Behavior = StructuralBehavior.Isotropic;
                //strucAsset.Density = 232.0;

                ////Assign the property set to the material.
                //PropertySetElement pse = PropertySetElement.Create(doc, strucAsset);
                //referenceMaterial.SetMaterialAspectByPropertySet(MaterialAspect.Structural, pse.Id);

            }
            tMaterial.Commit();

            return colorList;
            #endregion material
        }

        public enum ColorType { parcel, building, settlement, traffic, vegetation, waters, reference }

    }
}
