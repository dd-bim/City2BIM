using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Fabrication;
using Autodesk.Revit.UI;
using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Forms;
using System.Xml;

namespace City2RVT.Builder
{
    class RevitAlkisBuilder
    {
        ExternalCommandData commandData;
        private readonly Document doc;
        private readonly Dictionary<ColorType, ElementId> colors;

        public RevitAlkisBuilder(Document doc, ExternalCommandData cData)
        {
            commandData = cData;
            this.doc = doc;
            this.colors = CreateColorAsMaterial();
        }

        public void CreateTopo(List<AX_Object> topoObjs, System.Collections.IList selectedLayer)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            //for safety, Revit-Topo-List must contain internal stored TopoId (which will be saved when DTM2BIM-Topo is imported)

            if (GUI.Prop_NAS_settings.DrapeBldgsOnTopo || GUI.Prop_NAS_settings.DrapeParcelsOnTopo || GUI.Prop_NAS_settings.DrapeUsageOnTopo)
            {
                using (FilteredElementCollector collector = new FilteredElementCollector(doc))
                {
                    var topos = collector.OfClass(typeof(TopographySurface)).Select(t => t.Id);

                    if (!topos.Contains(Prop_Revit.TerrainId))
                    {
                        GUI.Prop_NAS_settings.DrapeBldgsOnTopo = false;
                        GUI.Prop_NAS_settings.DrapeParcelsOnTopo = false;
                        GUI.Prop_NAS_settings.DrapeUsageOnTopo = false;
                    }
                }
            }
            //----------------

            //each AX_Object will be represented as SubRegion
            //it is not allowed that subRegions overlap in Revit
            //therefore for each AX_Object.Group will be an separate topography object with the certain objects as subRegion
            //subRegions of one group should not overlap in the ALKIS data

            var groupedTopo = topoObjs.GroupBy(g => g.Group);

            

            foreach (var topoGr in groupedTopo)
            {
                bool createTopoPlane = true;

                if (topoGr.Key == AX_Object.AXGroup.building)
                    if (GUI.Prop_NAS_settings.DrapeBldgsOnTopo)
                        createTopoPlane = false;

                if (topoGr.Key == AX_Object.AXGroup.parcel)
                    if (GUI.Prop_NAS_settings.DrapeParcelsOnTopo)
                        createTopoPlane = false;

                if (topoGr.Key == AX_Object.AXGroup.usage)
                    if (GUI.Prop_NAS_settings.DrapeUsageOnTopo)
                        createTopoPlane = false;

                var topoId = Prop_Revit.TerrainId;

                if (createTopoPlane)
                {
                    var allPtArr = topoGr.Select(p => p.Segments);

                    var revPts = GetTopoPts(allPtArr, topoGr.Key, out double zOffset);

                    CreatePlanViews(zOffset, topoGr.Key);

                    using (Transaction topoTransaction = new Transaction(doc, "Create Topography reference plane for " + topoGr.Key))
                    {
                        {
                            topoTransaction.Start();
                            using (TopographySurface topoRef = TopographySurface.Create(doc, revPts))
                            {
                                topoRef.MaterialId = colors[ColorType.reference];

                                string commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                Parameter commAttr = topoRef.LookupParameter(commAttrLabel);
                                commAttr.Set("Reference plane for " + topoGr.Key);
                                topoId = topoRef.Id;

                                topoRef.Pinned = true;

                            }
                        }
                        topoTransaction.Commit();
                    }
                }
                
                foreach (AX_Object obj in topoGr)
                {
                    if (selectedLayer.Contains(obj.UsageType))
                    {
                        try
                        {
                            List<C2BPoint> polyExt = obj.Segments.Select(j => j[0]).ToList();
                            polyExt.Add(obj.Segments[0][0]);                                    //convert Segments to LinearRing

                            List<List<C2BPoint>> polysInt = new List<List<C2BPoint>>();

                            if (obj.InnerSegments != null)
                            {

                                foreach (var segInt in obj.InnerSegments)
                                {
                                    List<C2BPoint> polyInt = segInt.Select(j => j[0]).ToList();
                                    polyInt.Add(segInt[0][0]);                                    //convert Segments to LinearRing

                                    polysInt.Add(polyInt);
                                }
                            }

                            List<CurveLoop> loopList = Revit_Build.CreateExteriorCurveLoopList(polyExt, polysInt, out XYZ normal);

                            using (Transaction subTrans = new Transaction(doc, "Create " + obj.UsageType))
                            {
                                {
                                    FailureHandlingOptions options = subTrans.GetFailureHandlingOptions();
                                    options.SetFailuresPreprocessor(new AxesFailure());
                                    subTrans.SetFailureHandlingOptions(options);

                                    subTrans.Start();

                                    SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, loopList, topoId);
                                    siteSubRegion.TopographySurface.MaterialId = colors[MapToSubGroupForMaterial(obj.UsageType)];

                                    if (obj.Attributes != null)
                                        siteSubRegion = SetAttributeValues(siteSubRegion, obj.Attributes);

                                    string commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                    Parameter commAttr = siteSubRegion.TopographySurface.LookupParameter(commAttrLabel);
                                    commAttr.Set("ALKIS: " + obj.UsageType);

                                    siteSubRegion.TopographySurface.Pinned = true;


                                    TopographySurface topoSurface = siteSubRegion.TopographySurface;
                                    Dictionary<string, string> li = new Dictionary<string, string>();

                                    // get gml-id of element
                                    //topoGr.
                                    //XmlElement root = nodeSurf.ParentNode.ParentNode.ParentNode as XmlElement;
                                    //string gmlId = root.GetAttribute("gml:id");
                                    //string gmlId = "DEBYvAAAAABHqr1H";
                                    string gmlId = obj.Gmlid;

                                    // register the Schema object
                                    // check if schema with specific name exists. Otherwise new Schema is created. 
                                    Schema schema = default;

                                    XPlan_Semantic xPlan_Semantic = new XPlan_Semantic(doc, app);
                                    //Dictionary<string, string> paramDict = xPlan_Semantic.createParameter(xPlanObject, defFile, paramList, nodeSurf, xmlDoc, categorySet, logger, siteSubRegion.TopographySurface);

                                    string metaJsonPath = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\aaa.json";

                                    GUI.Properties.Wf_showProperties wf_ShowProperties = new GUI.Properties.Wf_showProperties(commandData);
                                    List<string> propListNames = wf_ShowProperties.readGmlJson(metaJsonPath, obj.UsageType);
                                    //string pathGml = wf_ShowProperties.retrieveFilePath(doc, list);
                                    string pathGml = @"D:\Daten\LandBIM\AP 2\Daten\ALKIS Import\Daten Ingolstadt\789172_0.xml";

                                    XmlReaderSettings readerSettings = new XmlReaderSettings();
                                    readerSettings.IgnoreComments = true;
                                    XmlDocument xmlDoc = new XmlDocument();

                                    Reader.ReadXPlan xPlanReader = new Reader.ReadXPlan();

                                    using (XmlReader reader = XmlReader.Create(pathGml, readerSettings))
                                    {
                                        xmlDoc.Load(reader);
                                        xmlDoc.Load(pathGml);
                                    }

                                    // Namespacemanager for used namespaces, e.g. in XPlanung GML or ALKIS XML files
                                    var XmlNsmgr = new Builder.Revit_Semantic(doc);
                                    XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);


                                    // get all nodes by gml-id
                                    XmlNodeList nodes = xmlDoc.SelectNodes("//ns2:" + obj.UsageType /*+ "[@gml:id='" + gmlId + "']"*/, nsmgr);

                                    string schemaName = obj.UsageType;

                                    // new schema builder for editing schema
                                    SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                                    schemaBuilder.SetSchemaName(schemaName);

                                    // allow anyone to read the object
                                    schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                                    //List<string> li = new List<string>();
                                    foreach (XmlNode xmlNode in nodes)
                                    {
                                        foreach (XmlNode c in xmlNode.ChildNodes)
                                        {
                                            if (propListNames.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                                            {
                                                if (!li.ContainsKey(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                                                {
                                                    li.Add(c.Name.Substring(c.Name.LastIndexOf(':') + 1), "-");
                                                }
                                                // parameter and values of datagridview are checkd and added as fields

                                                XmlElement test = xmlNode as XmlElement;
                                                if (test.GetAttribute("gml:id") == gmlId)
                                                {
                                                    li[c.Name.Substring(c.Name.LastIndexOf(':') + 1)] = c.InnerText;
                                                }
                                            }
                                        }
                                    }

                                    li.Add("id", gmlId);

                                    foreach (var p in li)
                                    {
                                        string paramName = p.Key.Substring(p.Key.LastIndexOf(':') + 1);

                                        // adds new field to the schema
                                        FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(paramName, typeof(string));
                                        fieldBuilder.SetDocumentation("Set XPlanung properties.");
                                    }

                                    schema = schemaBuilder.Finish();
                                    //}

                                    // create an entity (object) for this schema (class)
                                    Entity entity = new Entity(schema);

                                    foreach (var p in li)
                                    {
                                        // get the field from the schema
                                        string fieldName = p.Key.Substring(p.Key.LastIndexOf(':') + 1);
                                        Field fieldSpliceLocation = schema.GetField(fieldName);

                                        // set the value for this entity
                                        entity.Set<string>(fieldSpliceLocation, li[p.Key]);

                                        // store the entity in the element
                                        topoSurface.SetEntity(entity);
                                    }


                                }
                                subTrans.Commit();
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }                    
                } 
            }
        }

        private List<XYZ> GetTopoPts(IEnumerable<List<C2BPoint[]>> segments, AX_Object.AXGroup axGroup, out double zOffset)
        {
            List<XYZ> topoPts = new List<XYZ>();

            var ptList = new List<Calc.Point>();

            foreach (var ptArr in segments)
            {
                foreach (var ptA in ptArr)
                {
                    foreach (var pt in ptA)
                    {
                        var p = new Calc.Point(pt.X, pt.Y);
                        ptList.Add(p);
                    }
                }
            }

            var convexHull = Calc.ConvexHull.MakeHull(ptList);

            zOffset = default(double);

            if (GUI.Prop_NAS_settings.ZOffsetIsChecked)
            {
                switch (axGroup)
                {
                    case (AX_Object.AXGroup.parcel):
                        {
                            zOffset = -300;
                            break;
                        }

                    case (AX_Object.AXGroup.usage):
                        {
                            zOffset = -200;
                            break;
                        }

                    case (AX_Object.AXGroup.building):
                        {
                            zOffset = -100;
                            break;
                        }
                }
            }
            else
            {
                zOffset = 0.0;
            }

            foreach (var pt in convexHull)
            {
                var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(new C2BPoint(pt.x, pt.y, zOffset), true);
                //var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(new C2BPoint(pt.x, pt.y, 0.0), true);

                topoPts.Add(Revit_Build.GetRevPt(unprojectedPt));
            }
            return topoPts;
        }

        private void CreatePlanViews(double planElev, AX_Object.AXGroup axGroup)
        {
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
                 where (f.ViewType == ViewType.FloorPlan && !f.IsTemplate && f.Name.Equals("Plan_ALKIS_" + axGroup))
                 select f).Any();

            if (!alreadyExistent)
            {
                using (Transaction planTransaction = new Transaction(doc, "Create floor plan for topography " + axGroup))
                {
                    {
                        planTransaction.Start();
                        using (Level level1 = Level.Create(doc, planElev))
                        {
                            level1.Name = "Plan_ALKIS_" + axGroup;

                            using (ViewPlan floorView = ViewPlan.Create(doc, floorPlanId, level1.Id))
                            {
                                floorView.Name = "Plan_ALKIS_" + axGroup;
                                var topoCatId = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography).Id;
                                floorView.SetCategoryHidden(topoCatId, false);
                            }
                        }
                    }
                    planTransaction.Commit();
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

        private SiteSubRegion SetAttributeValues(SiteSubRegion siteSubRegion, Dictionary<Xml_AttrRep, string> attributes)
        {
            var attr = attributes.Keys;

            foreach (var aName in attr)
            {
                var p = siteSubRegion.TopographySurface.LookupParameter(aName.XmlNamespace + ": " + aName.Name);
                attributes.TryGetValue(aName, out var val);

                try
                {
                    if (val != null)
                    {
                        switch (aName.XmlType)
                        {
                            case (Xml_AttrRep.AttrType.intAttribute):
                                p.Set(int.Parse(val));
                                break;

                            case (Xml_AttrRep.AttrType.doubleAttribute):
                                p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                break;

                            case (Xml_AttrRep.AttrType.boolAttribute):
                                {
                                    if (val == "true")
                                        p.Set(1);

                                    if (val == "false" || val == "")
                                        p.Set(0);

                                    break;
                                }


                            case (Xml_AttrRep.AttrType.measureAttribute):
                                var valNew = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                p.Set(valNew / Prop_Revit.feetToM);
                                break;

                            case (Xml_AttrRep.AttrType.areaAttribute):
                                var valArea = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                p.Set(valArea * (1 / Prop_Revit.feetToM * 1 / Prop_Revit.feetToM));
                                break;

                            default:
                                p.Set(val);
                                break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            

            return siteSubRegion;
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

