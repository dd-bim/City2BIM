using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using City2BIM.Alkis;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace City2BIM.RevitBuilder
{
    class RevitAlkisBuilder
    {
        private Transform trafoPBP = Revit_Prop.TrafoPBP;
        private Document doc;
        private Dictionary<ColorType, ElementId> colors;

        public RevitAlkisBuilder(Document doc)
        {
            this.doc = doc;
            this.colors = CreateColorAsMaterial();
        }

        private List<XYZ> GetRevPts(List<C2BPoint> rawPts)
        {
            List<XYZ> revPts = new List<XYZ>();

            foreach (var pt in rawPts)
            {
                revPts.Add(GetRevPt(pt));
            }

            return revPts;
        }

        private XYZ GetRevPt(C2BPoint rawPt)
        {
            //Transformation for revit
            var ptCalc = new GeorefCalc();
            var unprojectedPt = ptCalc.CalcUnprojectedPoint(rawPt, true);

            var revitPt = unprojectedPt / Prop.feetToM;

            //Creation of Revit point
            var revitXYZ = new XYZ(revitPt.Y, revitPt.X, revitPt.Z);

            //Transform global coordinate to Revit project coordinate system (system of project base point)
            var revTransXYZ = trafoPBP.OfPoint(revitXYZ);

            return revTransXYZ;
        }

        public void CreateTopo(List<AX_Object> topoObjs)
        {


            FilteredElementCollector collector0 = new FilteredElementCollector(doc);
            IList<Element> topos = collector0.OfClass(typeof(TopographySurface)).ToElements();

            //var existTopoId = topos.FirstOrDefault().Id;

            //each AX_Object will be represented as SubRegion
            //it is not allowed that subRegions overlap in Revit
            //therefore for each AX_Object.Group will be an separate topography object with the certain objects as subRegion
            //subRegions of one group should not overlap in the ALKIS data

            var groupedTopo = topoObjs.GroupBy(g => g.Group);

            foreach (var topoGr in groupedTopo)
            {
                var allPtArr = topoGr.Select(p => p.Segments);
                var fPt = allPtArr.First().First()[0];

                C2BPoint uppR = new C2BPoint(fPt.X, fPt.Y, 0);
                C2BPoint lowL = new C2BPoint(fPt.X, fPt.Y, 0);

                foreach (var ptArr in allPtArr)
                {
                    foreach (var pt in ptArr)
                    {
                        foreach (var p in pt)
                        {
                            if (p.X > uppR.X)
                                uppR.X = p.X;

                            if (p.Y > uppR.Y)
                                uppR.Y = p.Y;

                            if (p.X < lowL.X)
                                lowL.X = p.X;

                            if (p.Y < lowL.Y)
                                lowL.Y = p.Y;
                        }
                    }
                }

                List<C2BPoint> bBox = new List<C2BPoint>();
                bBox.Add(lowL);
                bBox.Add(new C2BPoint(lowL.X, uppR.Y, 0.0));
                bBox.Add(uppR);
                bBox.Add(new C2BPoint(uppR.X, lowL.Y, 0.0));

                var revPts = GetRevPts(bBox);

                double zOffset = 0;

                if (topoGr.Key == AX_Object.AXGroup.parcel)
                    zOffset = -(300 / Prop.feetToM);

                if (topoGr.Key == AX_Object.AXGroup.usage)
                    zOffset = -(200 / Prop.feetToM);

                if (topoGr.Key == AX_Object.AXGroup.building)
                    zOffset = -(100 / Prop.feetToM);

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                IList<Element> viewFamilyTypes = collector.OfClass(typeof(ViewFamilyType)).ToElements();
                ElementId floorPlanId = new ElementId(-1);
                foreach (Element e in viewFamilyTypes)
                {
                    ViewFamilyType v = e as ViewFamilyType;

                    if (v != null && v.ViewFamily == ViewFamily.FloorPlan)
                    {
                        floorPlanId = e.Id;
                        break;
                    }
                }

                FilteredElementCollector collector2 = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan));

                bool alreadyExistent
                  = (from ViewPlan f in collector2
                     where (f.ViewType == ViewType.FloorPlan && !f.IsTemplate && f.ViewName.Equals("Plan_ALKIS_" + topoGr.Key))
                     select f).Any();

                if (!alreadyExistent)
                {
                    Transaction planTransaction = new Transaction(doc, "Create floor plan for topography " + topoGr.Key);
                    {
                        planTransaction.Start();
                        Level level1 = Level.Create(doc, zOffset);
                        level1.Name = "Plan_ALKIS_" + topoGr.Key;

                        ViewPlan floorView = ViewPlan.Create(doc, floorPlanId, level1.Id);
                        floorView.ViewName = "Plan_ALKIS_" + topoGr.Key;
                        var topoCatId = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography).Id;
                        floorView.SetCategoryHidden(topoCatId, false);
                    }
                    planTransaction.Commit();
                }
                var revPtsOffset = new List<XYZ>();

                foreach (var pt in revPts)
                {
                    var ptOff = new XYZ(pt.X, pt.Y, zOffset);
                    revPtsOffset.Add(ptOff);
                }

                ElementId topoId = default(ElementId);

                Transaction topoTransaction = new Transaction(doc, "Create Topography reference plane for " + topoGr.Key);
                {
                    try
                    {
                        topoTransaction.Start();
                        //SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        TopographySurface topoRef = TopographySurface.Create(doc, revPtsOffset);
                        topoRef.MaterialId = colors[ColorType.reference];

                        string commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        Parameter commAttr = topoRef.LookupParameter(commAttrLabel);
                        commAttr.Set("Reference plane for " + topoGr.Key);
                        topoId = topoRef.Id;

                    }
                    catch
                    {

                    }
                }
                topoTransaction.Commit();

                foreach (var obj in topoGr)
                {

                    try
                    {
                        List<CurveLoop> loopList = new List<CurveLoop>();

                        //exterior Ring

                        List<Curve> edges = new List<Curve>();

                        for (var c = 0; c < obj.Segments.Count; c++)
                        {
                            var pStart = new C2BPoint(obj.Segments[c][0].X, obj.Segments[c][0].Y, zOffset);
                            var pEnd = new C2BPoint(obj.Segments[c][1].X, obj.Segments[c][1].Y, zOffset);

                            Line edge = Line.CreateBound(GetRevPt(pStart), GetRevPt(pEnd));

                            edges.Add(edge);
                        }

                        CurveLoop baseLoop = CurveLoop.Create(edges);
                        loopList.Add(baseLoop);

                        //-------

                        //interior Rings

                        if (obj.InnerSegments != null)
                        {
                            foreach (var intLoop in obj.InnerSegments)
                            {
                                List<Curve> innerEdges = new List<Curve>();

                                for (var c = 0; c < intLoop.Count; c++)
                                {
                                    var pStart = new C2BPoint(intLoop[c][0].X, intLoop[c][0].Y, zOffset);
                                    var pEnd = new C2BPoint(intLoop[c][1].X, intLoop[c][1].Y, zOffset);

                                    Line edge = Line.CreateBound(GetRevPt(pStart), GetRevPt(pEnd));

                                    innerEdges.Add(edge);
                                }
                                CurveLoop innerLoop = CurveLoop.Create(innerEdges);
                                loopList.Add(innerLoop);
                            }
                        }

                        Transaction subTrans = new Transaction(doc, "Create " + obj.UsageType);
                        {
                            try
                            {
                                FailureHandlingOptions options = subTrans.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new AxesFailure());
                                subTrans.SetFailureHandlingOptions(options);

                                subTrans.Start();
                                //SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                                //if (obj.Group == AX_Object.AXGroup.usage || obj.Group == AX_Object.AXGroup.building)
                                //topoId = existTopoId;

                                SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, loopList, topoId);
                                siteSubRegion.TopographySurface.MaterialId = colors[MapToSubGroupForMaterial(obj.UsageType)];

                                if (obj.Attributes != null)
                                    siteSubRegion = SetAttributeValues(siteSubRegion, obj.Attributes);

                                string commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                Parameter commAttr = siteSubRegion.TopographySurface.LookupParameter(commAttrLabel);
                                commAttr.Set("ALKIS: " + obj.UsageType);

                            }
                            catch
                            {

                            }

                        }
                        subTrans.Commit();

                    }
                    catch
                    {
                        continue;
                    }
                }

            }
        }

        public class AxesFailure : IFailuresPreprocessor
        {
            //Eventhandler, der eine ignorierbare Warnung, die nur auf einzelnen Geräten auftrat, überspringt.
            public FailureProcessingResult PreprocessFailures(
              FailuresAccessor a)
            {
                // inside event handler, get all warnings
                IList<FailureMessageAccessor> failures
                  = a.GetFailureMessages();

                foreach (FailureMessageAccessor f in failures)
                {
                    // check failure definition ids 
                    // against ones to dismiss:

                    FailureDefinitionId id
                      = f.GetFailureDefinitionId();

                    if (BuiltInFailures.InaccurateFailures.InaccurateSketchLine
                      == id)
                    {
                        a.DeleteWarning(f);
                    }
                }
                return FailureProcessingResult.Continue;
            }
        }


        ////Gesamt-Topos --> Lesen der BBox

        //XYZ origin = new XYZ(0, 0, 0);
        //XYZ normal = new XYZ(0, 0, 1 /*feetToMeter*/);
        //Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

        //ElementId elementIdFlst = default(ElementId);

        //Transaction topoTransaction = new Transaction(doc, "Create Topography Gesamt Flurstücke");
        //{
        //    FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
        //    options.SetFailuresPreprocessor(new AxesFailure());
        //    topoTransaction.SetFailureHandlingOptions(options);

        //    topoTransaction.Start();
        //    SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
        //    TopographySurface flst = TopographySurface.Create(doc, pointsFlst);
        //    Parameter gesamt = flst.LookupParameter("Kommentare");
        //    gesamt.Set("TopoGesamt");
        //    elementIdFlst = flst.Id;
        //}
        //topoTransaction.Commit();


        private SiteSubRegion SetAttributeValues(SiteSubRegion ds, Dictionary<XmlAttribute, string> attributes)
        {
            var attr = attributes.Keys;

            foreach (var aName in attr)
            {
                var p = ds.TopographySurface.LookupParameter(aName.XmlNamespace + ": " + aName.Name);
                attributes.TryGetValue(aName, out var val);

                try
                {
                    if (val != null)
                    {
                        switch (aName.XmlType)
                        {
                            case (XmlAttribute.AttrType.intAttribute):
                                p.Set(int.Parse(val));
                                break;

                            case (XmlAttribute.AttrType.doubleAttribute):
                                p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                break;

                            case (XmlAttribute.AttrType.boolAttribute):
                                {
                                    if (val == "true")
                                        p.Set(1);

                                    if (val == "false" || val == "")
                                        p.Set(0);

                                    break;
                                }


                            case (XmlAttribute.AttrType.measureAttribute):
                                var valNew = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                p.Set(valNew / Prop.feetToM);
                                break;

                            case (XmlAttribute.AttrType.areaAttribute):
                                var valArea = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                p.Set(valArea * (1 / Prop.feetToM * 1 / Prop.feetToM));
                                break;

                            default:
                                p.Set(val);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Semantik-Fehler bei " + aName.Name + " Error: " + ex.Message);
                    continue;
                }
            }

            return ds;
        }


        private Dictionary<ColorType, ElementId> CreateColorAsMaterial()
        {
            ElementId parcelMat = ElementId.InvalidElementId;
            ElementId bldgMat = ElementId.InvalidElementId;
            ElementId settleMat = ElementId.InvalidElementId;
            ElementId trafficMat = ElementId.InvalidElementId;
            ElementId vegetMat = ElementId.InvalidElementId;
            ElementId watersMat = ElementId.InvalidElementId;
            ElementId refMat = ElementId.InvalidElementId;

            using (Transaction t = new Transaction(doc, "Create material"))
            {
                t.Start();

                var coll = new FilteredElementCollector(doc).OfClass(typeof(Material));
                IEnumerable<Material> materialsEnum = coll.ToElements().Cast<Material>();

                //Building

                var bldgMats
                  = from materialElement in materialsEnum
                    where materialElement.Name == "Building_red"
                    select materialElement.Id;

                if (bldgMats.Count() == 0)
                {
                    bldgMat = Material.Create(doc, "Building_red");
                    Material matBldg = doc.GetElement(bldgMat) as Material;
                    matBldg.Color = new Color(255, 0, 0);
                }
                else
                    bldgMat = bldgMats.First();

                //Parcel

                var parcelMats
                  = from materialElement in materialsEnum
                    where materialElement.Name == "Parcel_grey"
                    select materialElement.Id;

                if (parcelMats.Count() == 0)
                {
                    parcelMat = Material.Create(doc, "Parcel_grey");
                    Material matParcel = doc.GetElement(parcelMat) as Material;
                    matParcel.Color = new Color(80, 80, 80);
                }
                else
                    parcelMat = parcelMats.First();

                //Settlements

                var settleMats
              = from materialElement in materialsEnum
                where materialElement.Name == "Settlement_orange"
                select materialElement.Id;

                if (settleMats.Count() == 0)
                {
                    settleMat = Material.Create(doc, "Settlement_orange");
                    Material matSettle = doc.GetElement(settleMat) as Material;
                    matSettle.Color = new Color(255, 127, 0);
                }
                else
                    settleMat = settleMats.First();

                //Traffic

                var trafficMats
                      = from materialElement in materialsEnum
                        where materialElement.Name == "Traffic_black"
                        select materialElement.Id;

                if (trafficMats.Count() == 0)
                {
                    trafficMat = Material.Create(doc, "Traffic_black");
                    Material matTraff = doc.GetElement(trafficMat) as Material;
                    matTraff.Color = new Color(33, 33, 33);
                }
                else
                    trafficMat = trafficMats.First();


                //Vegetation

                var vegetMats
                      = from materialElement in materialsEnum
                        where materialElement.Name == "Vegetation_green"
                        select materialElement.Id;

                if (vegetMats.Count() == 0)
                {
                    vegetMat = Material.Create(doc, "Vegetation_green");
                    Material matVeg = doc.GetElement(vegetMat) as Material;
                    matVeg.Color = new Color(0, 139, 0);
                }
                else
                    vegetMat = vegetMats.First();


                //Waters

                var watersMats
                      = from materialElement in materialsEnum
                        where materialElement.Name == "Waters_blue"
                        select materialElement.Id;

                if (watersMats.Count() == 0)
                {
                    watersMat = Material.Create(doc, "Waters_blue");
                    Material matWater = doc.GetElement(watersMat) as Material;
                    matWater.Color = new Color(0, 191, 255);
                }
                else
                    watersMat = watersMats.First();

                //ReferencePlanes

                var refMats
                      = from materialElement in materialsEnum
                        where materialElement.Name == "Reference_transparent"
                        select materialElement.Id;

                if (refMats.Count() == 0)
                {
                    refMat = Material.Create(doc, "Reference_transparent");
                    Material matRef = doc.GetElement(refMat) as Material;
                    matRef.Color = new Color(255, 255, 255);
                    matRef.Transparency = 100;
                }
                else
                    refMat = refMats.First();

                t.Commit();
            }

            var colorList = new Dictionary<ColorType, ElementId>
            {
                { ColorType.parcel, parcelMat },
                { ColorType.building, bldgMat },
                { ColorType.settlement, settleMat },
                { ColorType.traffic, trafficMat },
                { ColorType.vegetation, vegetMat },
                { ColorType.waters, watersMat },
                { ColorType.reference, refMat }
            };

            return colorList;
        }

        private enum ColorType { parcel, building, settlement, traffic, vegetation, waters, reference }

        private ColorType MapToSubGroupForMaterial(string usageType)
        {
            if (usageType == "AX_Flurstueck")
                return ColorType.parcel;

            else if (usageType == "AX_Gebaeude")
                return ColorType.building;

            else if (usageType == "AX_Wohnbauflaeche" ||
                    usageType == "AX_IndustrieUndGewerbeflaeche" ||
                    usageType == "AX_Halde" ||
                    usageType == "AX_Bergbaubetrieb" ||
                    usageType == "AX_TagebauGrubeSteinbruch" ||
                    usageType == "AX_FlaecheGemischterNutzung" ||
                    usageType == "AX_FlaecheBesondererFunktionalerPraegung" ||
                    usageType == "AX_SportFreizeitUndErholungsflaeche" ||
                    usageType == "AX_Friedhof")
            { return ColorType.settlement; }

            else if (usageType == "AX_Strassenverkehr" ||
                    usageType == "AX_Weg" ||
                    usageType == "AX_Platz" ||
                    usageType == "AX_Bahnverkehr" ||
                    usageType == "AX_Flugverkehr" ||
                    usageType == "AX_Schiffsverkehr")
            { return ColorType.traffic; }

            else if (usageType == "AX_Landwirtschaft" ||
                    usageType == "AX_Wald" ||
                    usageType == "AX_Gehoelz" ||
                    usageType == "AX_Heide" ||
                    usageType == "AX_Moor" ||
                    usageType == "AX_Sumpf" ||
                    usageType == "AX_UnlandVegetationsloseFlaeche")
            { return ColorType.vegetation; }

            //group "Gewaesser"
            else if (usageType == "AX_Fliessgewaesser" ||
                    usageType == "AX_Hafenbecken" ||
                    usageType == "AX_StehendesGewaesser" ||
                    usageType == "AX_Meer")
            { return ColorType.waters; }

            else
                return ColorType.parcel;

        }
    }
}

