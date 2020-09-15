using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using System.Collections.Generic;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Xml;

using NLog;
using NLog.Targets;
using NLog.Config;
using Xbim.Tessellator;
using City2RVT.GUI.XPlan2BIM;

namespace City2RVT.Builder
{
    class RevitXPlanBuilder
    {
        ExternalCommandData commandData;
        private readonly Document doc;
        private readonly Autodesk.Revit.ApplicationServices.Application app;
        double feetToMeter = 1.0 / 0.3048;

        public RevitXPlanBuilder(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            this.doc = doc;
            this.app = app;
            //this.colors = CreateColorAsMaterial();
        }

        /// <summary>
        /// Creates a line by start and end point of the XML/GML node. 
        /// </summary>
        /// <param name="koordWerte"></param>
        /// <param name="R"></param>
        /// <param name="transf"></param>
        /// <param name="zOffset"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates a line by multiple curve points of XML/GML node. 
        /// </summary>
        /// <param name="koordWerte"></param>
        /// <param name="R"></param>
        /// <param name="transf"></param>
        /// <param name="iSplit"></param>
        /// <param name="zOffset"></param>
        /// <returns></returns>
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

            Line lineClIndu = default;

            if (tStartPoint.DistanceTo(tEndPoint) > 0)
            {
                lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);
            }


            return lineClIndu;
        }

        /// <summary>
        /// Creates Material for the imported topographys.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, ElementId> CreateMaterial()
        {
            #region material
            var transparentMaterialId = default(ElementId);
            var strassenVerkehrsFlaecheMaterialId = default(ElementId);
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
                colorList.Add("BP_StrassenVerkehrsFlaeche", strassenVerkehrsFlaecheMaterialId);

                gewaesserFlaecheId = Material.Create(doc, "gewaesserFlaeche");
                Material gewaesserFlaeche = doc.GetElement(gewaesserFlaecheId) as Material;
                gewaesserFlaeche.Color = new Color(030, 144, 255);
                colorList.Add("BP_GewaesserFlaeche", gewaesserFlaecheId);

                ueberbaubareGrundstuecksFlaecheMaterialId = Material.Create(doc, "ueberbaubareGrundstuecksFlaeche");
                Material ueberbaubareGrundstuecksFlaecheMaterial = doc.GetElement(ueberbaubareGrundstuecksFlaecheMaterialId) as Material;
                ueberbaubareGrundstuecksFlaecheMaterial.Color = new Color(160, 082, 045);
                colorList.Add("BP_UeberbaubareGrundstuecksFlaeche", ueberbaubareGrundstuecksFlaecheMaterialId);

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
                colorList.Add("BP_KennzeichnungsFlaeche", kennzeichnungsFlaecheMaterialId);

                erhaltungsBereichFlaecheMaterialId = Material.Create(doc, "ErhaltungsBereichFlaeche");
                Material erhaltungsBereichFlaecheMaterial = doc.GetElement(erhaltungsBereichFlaecheMaterialId) as Material;
                erhaltungsBereichFlaecheMaterial.Color = new Color(0, 255, 0);
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

        public ElementId createRefPlane(XmlNodeList xPlanExterior, string xPlanObject, double zOffset, Plane geomPlane, Dictionary<string, ElementId> colorDict, Logger logger)
        {
            ElementId refplaneId = Prop_Revit.TerrainId;

            var transfClass = new Calc.Transformation();
            Transform transf = transfClass.transform(doc);

            // No UTM reduction at the moment, factor R = 1
            double R = 1;

            Dictionary<string, XYZ[]> xPlanPointDict = new Dictionary<string, XYZ[]>();

            List<string> xPlanReference = new List<String>();
            int xPlanCountReference = 0;

            List<double> xPlanAllValues = new List<double>();
            List<double> xPlanXValues = new List<double>();
            List<double> xPlanYValues = new List<double>();

            foreach (XmlNode exteriorNode in xPlanExterior)
            {
                xPlanReference.Add(exteriorNode.InnerText);
                string[] coordsReference = xPlanReference[xPlanCountReference].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var x in coordsReference)
                {
                    double values_double = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
                    xPlanAllValues.Add(values_double);
                }

                for (int ix = 0; ix < xPlanAllValues.Count; ix += 2)
                {
                    xPlanXValues.Add(xPlanAllValues[ix]);
                }

                for (int iy = 1; iy < xPlanAllValues.Count; iy += 2)
                {
                    xPlanYValues.Add(xPlanAllValues[iy]);
                }
                xPlanCountReference++;
            }

            double xPlanXMin = (xPlanXValues.Min() * feetToMeter) / R;
            double xPlanXMax = (xPlanXValues.Max() * feetToMeter) / R;
            double xPlanYMin = (xPlanYValues.Min() * feetToMeter) / R;
            double xPlanYMax = (xPlanYValues.Max() * feetToMeter) / R;

            XYZ[] pointsExteriorXPlan = new XYZ[4];
            pointsExteriorXPlan[0] = transf.OfPoint(new XYZ(xPlanXMin, xPlanYMin, zOffset));
            pointsExteriorXPlan[1] = transf.OfPoint(new XYZ(xPlanXMax, xPlanYMin, zOffset));
            pointsExteriorXPlan[2] = transf.OfPoint(new XYZ(xPlanXMax, xPlanYMax, zOffset));
            pointsExteriorXPlan[3] = transf.OfPoint(new XYZ(xPlanXMin, xPlanYMax, zOffset));

            xPlanPointDict.Add(xPlanObject, pointsExteriorXPlan);

            foreach (var referencePoints in xPlanPointDict)
            {
                {
                    using (Transaction referenceTransaction = new Transaction(doc, "Reference plane: " + (referencePoints.Key).Substring((referencePoints.Key).LastIndexOf(':') + 1)))
                    {
                        FailureHandlingOptions options = referenceTransaction.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new GUI.XPlan2BIM.Wpf_XPlan.AxesFailure());
                        referenceTransaction.SetFailureHandlingOptions(options);

                        referenceTransaction.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                        TopographySurface referencePlane = TopographySurface.Create(doc, referencePoints.Value);

                        ElementId farbeReference = colorDict["transparent"];

                        Parameter materialParam = referencePlane.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                        materialParam.Set(farbeReference);

                        Parameter kommentarParam = referencePlane.LookupParameter("Kommentare");
                        kommentarParam.Set("Reference plane: " + (referencePoints.Key).Substring(6));

                        refplaneId = referencePlane.Id;

                        referencePlane.CanBeLocked();
                        referencePlane.Pinned = true;

                        logger.Info("Reference plane: '" + (referencePoints.Key).Substring((referencePoints.Key).LastIndexOf(':') + 1) + "' created.");
                        referenceTransaction.Commit();
                    }
                }
            }
            return refplaneId;
        }

        public ElementId copyDtm(string xPlanObject, ElementId pickedId, double zOffset, Dictionary<string, ElementId> colorDict)
        {
            ElementId refplaneId = Prop_Revit.TerrainId;
            using (Transaction copyTrans = new Transaction(doc, "Copy DTM"))
            {
                copyTrans.Start();
                var eCopy = ElementTransformUtils.CopyElement(doc, pickedId, new XYZ(0, 0, zOffset + 0.1));
                ElementId copyElemId = eCopy.First();
                Element copiedElement = doc.GetElement(copyElemId);
                TopographySurface referencePlane = copiedElement as TopographySurface;
                refplaneId = referencePlane.Id;
                ElementId farbeReference = colorDict["transparent"];

                Parameter materialParam = referencePlane.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                materialParam.Set(farbeReference);

                Parameter kommentarParam = referencePlane.LookupParameter("Kommentare");
                kommentarParam.Set("Reference plane: " + xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1));

                copyTrans.Commit();
            }
            return refplaneId;
        }

        public void createSurface(string xPlanObject, XmlNodeList bpSurface, double zOffset, XmlDocument xmlDoc, CategorySet categorySet, Plane geomPlane, Logger logger, 
            Dictionary<string, ElementId> colorDict, ElementId refplaneId)
        {
            var transfClass = new City2RVT.Calc.Transformation();
            Transform transf = transfClass.transform(doc);

            DefinitionFile defFile = default;
            City2RVT.GUI.XPlan2BIM.XPlan_Parameter parameter = new GUI.XPlan2BIM.XPlan_Parameter();

            // No UTM reduction at the moment, factor R = 1
            double R = 1;

            //create shared parameter file
            string modulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
            string sharedParamFile = Path.Combine(modulePath, "City2BIM_Parameters.txt");

            if (!File.Exists(sharedParamFile))
            {
                FileStream fs = File.Create(sharedParamFile);
                fs.Close();
            }

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            List<string> positionList = new List<String>();
            List<string> paramList = new List<String>();
            List<string> interiorListe = new List<String>();
            int i = 0;
            foreach (XmlNode nodeSurf in bpSurface)
            {
                List<CurveLoop> curveLoopSurfaceList = new List<CurveLoop>();
                CurveLoop curveLoop = new CurveLoop();
                int ii = 0;
                foreach (XmlNode interiorNode in nodeSurf.ParentNode.ChildNodes)
                {
                    CurveLoop curveLoopInterior = new CurveLoop();
                    if (interiorNode.Name == "gml:interior")
                    {
                        XmlNodeList interiorNodeList = interiorNode.SelectNodes("gml:LinearRing/gml:posList", nsmgr);
                        XmlNodeList interiorRingNodeList = interiorNode.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);

                        foreach (XmlNode xc in interiorNodeList)
                        {
                            interiorListe.Add(xc.InnerText);
                            string[] koordWerteInterior = interiorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (koordWerteInterior.Count() == 4)
                            {
                                var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                                Line lineExterior = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                                curveLoopInterior.Append(lineExterior);
                            }

                            else if (koordWerteInterior.Count() > 4)
                            {
                                int ia = 0;

                                foreach (string split in koordWerteInterior)
                                {
                                    var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                                    Line lineClIndu = geomBuilder.CreateLineRing(koordWerteInterior, R, transf, ia, zOffset);
                                    curveLoopInterior.Append(lineClIndu);

                                    if ((ia + 3) == (koordWerteInterior.Count() - 1))
                                    {
                                        break;
                                    }
                                    ia += 2;
                                }
                            }
                            ii++;
                        }

                        foreach (XmlNode xc in interiorRingNodeList)
                        {
                            interiorListe.Add(xc.InnerText);
                            string[] koordWerteInterior = interiorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (koordWerteInterior.Count() == 4)
                            {
                                var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                                Line lineStrasse = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                                curveLoopInterior.Append(lineStrasse);
                            }

                            else if (koordWerteInterior.Count() > 4)
                            {
                                int ib = 0;
                                foreach (string split in koordWerteInterior)
                                {
                                    var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                                    Line lineClIndu = geomBuilder.CreateLineRing(koordWerteInterior, R, transf, ib, zOffset);
                                    curveLoopInterior.Append(lineClIndu);

                                    if ((ib + 3) == (koordWerteInterior.Count() - 1))
                                    {
                                        break;
                                    }

                                    ib += 2;
                                }
                            }
                            ii++;
                        }
                    }
                    if (curveLoopInterior.GetExactLength() > 0)
                    {
                        curveLoopSurfaceList.Add(curveLoopInterior);
                    }
                }

                XmlNodeList surface = nodeSurf.SelectNodes("gml:LinearRing/gml:posList", nsmgr);
                XmlNodeList surfaceRing = nodeSurf.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);

                foreach (XmlNode child in nodeSurf.ParentNode.ParentNode.ParentNode)
                {
                    defFile = parameter.CreateDefinitionFile(sharedParamFile, app, doc, child.Name, "XPlanDaten");
                    if (child.Name != "#comment")
                    {
                        paramList.Add(child.Name);
                    }
                }

                foreach (XmlNode child in nodeSurf.ParentNode.ParentNode.ParentNode.ParentNode)
                {
                    defFile = parameter.CreateDefinitionFile(sharedParamFile, app, doc, child/*.ParentNode*/.Attributes["gml:id"].Name, "XPlanDaten");
                    if (child/*.ParentNode*/.Attributes["gml:id"].Name != "#comment")
                    {
                        paramList.Add(child/*.ParentNode*/.Attributes["gml:id"].Name);
                    }
                }

                foreach (XmlNode nodePosSurf in surface)
                {
                    positionList.Add(nodePosSurf.InnerText);
                    string[] koordWerte = positionList[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (koordWerte.Count() == 4)
                    {
                        var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                        Line lineStrasse = geomBuilder.CreateLineString(koordWerte, R, transf, zOffset);
                        curveLoop.Append(lineStrasse);
                    }

                    else if (koordWerte.Count() > 4)
                    {
                        int ic = 0;

                        foreach (string split in koordWerte)
                        {
                            var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                            Line lineClIndu = geomBuilder.CreateLineRing(koordWerte, R, transf, ic, zOffset);
                            curveLoop.Append(lineClIndu);

                            if ((ic + 3) == (koordWerte.Count() - 1))
                            {
                                break;
                            }
                            ic += 2;
                        }
                    }
                    i++;
                }

                foreach (XmlNode nodePosSurf in surfaceRing)
                {
                    positionList.Add(nodePosSurf.InnerText);
                    string[] koordWerte = positionList[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (koordWerte.Count() == 4)
                    {
                        var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                        Line lineStrasse = geomBuilder.CreateLineString(koordWerte, R, transf, zOffset);
                        curveLoop.Append(lineStrasse);
                    }

                    else if (koordWerte.Count() > 4)
                    {
                        int ie = 0;

                        foreach (string split in koordWerte)
                        {
                            var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);
                            Line lineClIndu = geomBuilder.CreateLineRing(koordWerte, R, transf, ie, zOffset);
                            curveLoop.Append(lineClIndu);

                            if ((ie + 3) == (koordWerte.Count() - 1))
                            {
                                break;
                            }

                            ie += 2;
                        }
                    }
                    i++;
                }

                System.Collections.IList selectedParams = GUI.Prop_NAS_settings.SelectedParams;


                //_________________________________
                // imports parameter (values later)
                //*********************************
                XPlan_Semantic xPlan_Semantic = new XPlan_Semantic(doc,app);
                Dictionary<string, string> paramDict = xPlan_Semantic.createParameter(xPlanObject, defFile, paramList, nodeSurf, xmlDoc, categorySet, logger);

                if (curveLoop.GetExactLength() > 0)
                {
                    curveLoopSurfaceList.Add(curveLoop);

                    Transaction topoTransaction = new Transaction(doc, "Create Exterior: " + xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1));
                    {
                        FailureHandlingOptions optionsExterior = topoTransaction.GetFailureHandlingOptions();
                        optionsExterior.SetFailuresPreprocessor(new GUI.XPlan2BIM.Wpf_XPlan.AxesFailure());
                        topoTransaction.SetFailureHandlingOptions(optionsExterior);

                        XmlElement root = nodeSurf.ParentNode.ParentNode.ParentNode as XmlElement;
                        string gmlid = root.GetAttribute("gml:id");

                        topoTransaction.Start();
                        SketchPlane sketchExterior = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopSurfaceList, refplaneId);

                        Parameter materialParamSurface = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);

                        ElementId farbe = default;
                        if (colorDict.ContainsKey(xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)))
                        {
                            farbe = colorDict[xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)];
                        }
                        else
                        {
                            farbe = colorDict["default"];
                        }
                        materialParamSurface.Set(farbe);

                        try
                        {
                            foreach (var x in paramDict)
                            {
                                Parameter jederParameter = siteSubRegion.TopographySurface.LookupParameter(x.Key);
                                jederParameter.Set(x.Value);
                            }
                        }
                        catch
                        {
                            System.Windows.Forms.MessageBox.Show("fehler");
                        }

                        Parameter exteriorName = siteSubRegion.TopographySurface.LookupParameter("Kommentare");
                        exteriorName.Set(xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1));

                        siteSubRegion.TopographySurface.Pinned = true;

                        logger.Info("Created sitesubregion for '" + xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1) + "' (Exterior). ");
                    }
                    topoTransaction.Commit();
                }
                paramDict.Clear();
            }
        }



        public void createLineSegments(string xPlanObject, XmlDocument xmlDoc, XmlNamespaceManager nsmgr, double zOffset, ElementId pickedId, bool drape_checked)
        {
            XmlNodeList bpLines = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "/xplan:position/gml:LineString", nsmgr);

            var transfClass = new Calc.Transformation();
            Transform transf = transfClass.transform(doc);

            // No UTM reduction at the moment, factor R = 1
            double R = 1;

            List<string> lineList = new List<String>();
            int il = 0;
            foreach (XmlNode nodeLine in bpLines)
            {
                lineList.Add(nodeLine.InnerText);
                string[] koordWerteLine = lineList[il].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                int ia = 0;

                using (Transaction txnCreateLine = new Transaction(doc, "Create Line for " + xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)))
                {
                    txnCreateLine.Start();
                    FailureHandlingOptions optionsLines = txnCreateLine.GetFailureHandlingOptions();
                    optionsLines.SetFailuresPreprocessor(new Wpf_XPlan.AxesFailure());
                    txnCreateLine.SetFailureHandlingOptions(optionsLines);

                    foreach (string split in koordWerteLine)
                    {
                        double xStart = Convert.ToDouble(koordWerteLine[ia], System.Globalization.CultureInfo.InvariantCulture);
                        double xStartMeter = xStart * feetToMeter;
                        double xStartMeterRedu = xStartMeter / R;
                        double yStart = Convert.ToDouble(koordWerteLine[ia + 1], System.Globalization.CultureInfo.InvariantCulture);
                        double yStartMeter = yStart * feetToMeter;
                        double yStartMeterRedu = yStartMeter / R;
                        double zStart = zOffset;
                        double zStartMeter = zStart * feetToMeter;

                        double xEnd = Convert.ToDouble(koordWerteLine[ia + 2], System.Globalization.CultureInfo.InvariantCulture);
                        double xEndMeter = xEnd * feetToMeter;
                        double xEndMeterRedu = xEndMeter / R;
                        double yEnd = Convert.ToDouble(koordWerteLine[ia + 3], System.Globalization.CultureInfo.InvariantCulture);
                        double yEndMeter = yEnd * feetToMeter;
                        double yEndMeterRedu = yEndMeter / R;
                        double zEnd = zOffset;
                        double zEndMeter = zEnd * feetToMeter;

                        XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
                        XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

                        XYZ tStartPoint = transf.OfPoint(startPoint);
                        XYZ tEndPoint = transf.OfPoint(endPoint);

                        Calc.Transformation transformation = new Calc.Transformation();
                        Plane geomPlane = default;
                        Line lineString = default;
                        if (drape_checked == true)
                        {
                            Element originalTerrain = doc.GetElement(pickedId);
                            TopographySurface terrain = originalTerrain as TopographySurface;

                            var terPoints = terrain.GetInteriorPoints();
                            List<XYZ> elevationList = new List<XYZ>();
                            foreach (var tp in terPoints)
                            {
                                elevationList.Add(tp);
                            }

                            var matchStart = elevationList.OrderBy(e => Math.Abs(e.DistanceTo(tStartPoint))).FirstOrDefault();
                            var matchEnd = elevationList.OrderBy(e => Math.Abs(e.DistanceTo(tEndPoint))).FirstOrDefault();

                            XYZ startPointTerrain = new XYZ(tStartPoint.X, tStartPoint.Y, matchStart.Z);
                            XYZ endPointTerrain = new XYZ(tEndPoint.X, tEndPoint.Y, matchEnd.Z);

                            XYZ norm = startPointTerrain.CrossProduct(endPointTerrain);
                            XYZ origin = endPointTerrain;


                            geomPlane = transformation.getGeomPlane(norm, origin);
                            lineString = Line.CreateBound(startPointTerrain, endPointTerrain);
                        }
                        else
                        {
                            XYZ origin = new XYZ(0, 0, tStartPoint.Z);
                            XYZ norm = new XYZ(0, 0, 1);
                            geomPlane = transformation.getGeomPlane(norm, origin);
                            lineString = Line.CreateBound(tStartPoint, tEndPoint);
                        }                        
                        
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                        ModelLine line = doc.Create.NewModelCurve(lineString, sketch) as ModelLine;

                        GraphicsStyle gs = line.LineStyle as GraphicsStyle;

                        gs.GraphicsStyleCategory.LineColor = new Color(250, 10, 10);
                        gs.GraphicsStyleCategory.SetLineWeight(10, GraphicsStyleType.Projection);                       

                        if ((ia + 3) == (koordWerteLine.Count() - 1))
                        {
                            break;
                        }
                        ia += 2;
                    }
                    txnCreateLine.Commit();
                }
                il++;
            }
        }
    }
}
