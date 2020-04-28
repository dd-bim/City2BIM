using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

using NLog;
using NLog.Targets;
using NLog.Config;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan.xaml
    /// </summary>
    public partial class Wpf_XPlan : Window
    {
        ExternalCommandData commandData;
        double feetToMeter = 1.0 / 0.3048;

        public Wpf_XPlan(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();

            xplan_file.Text = Prop_XPLAN_settings.FileUrl;
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

        private void Button_Click_SetXPlanFile(object sender, RoutedEventArgs e)
        {
            Reader.FileDialog winexp = new Reader.FileDialog();
            xplan_file.Text = winexp.ImportPath(Reader.FileDialog.Data.XPlanGML);
        }


        private void Button_Click_StartXPlanImport(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            // NLog
            #region logging
            String tempPath = Path.GetTempPath();
            var configNLog = new LoggingConfiguration();

            var fileTarget = new FileTarget("target2")
            {
                FileName = Path.GetFullPath(Path.Combine(tempPath, @"XPlan2Revit_${shortdate}.log")),
                Layout = "${longdate} ${level} ${message}  ${exception}"
            };
            configNLog.AddTarget(fileTarget);
            configNLog.AddRuleForAllLevels(fileTarget);

            LogManager.Configuration = configNLog;
            Logger logger = LogManager.GetCurrentClassLogger();

            #endregion logging

            var colorDict = new Dictionary<string, ElementId>();
            var materialBuilder = new Builder.RevitXPlanBuilder(doc);
            colorDict = materialBuilder.CreateMaterial();

            // Transforms local coordinates to relative coordinates due to the fact that revit has a 20 miles limit for presentation of geometry. 
            var transfClass = new City2RVT.Calc.Transformation();
            Transform transf = transfClass.transform(doc);

            // No UTM reduction at the moment, factor R = 1
            double R = 1;

            //loads the selected GML file
            string xPlanGmlPath = xplan_file.Text;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            XmlDocument xmlDoc = new XmlDocument();

            using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
            {
                xmlDoc.Load(reader);
                xmlDoc.Load(xPlanGmlPath);
            }

            XYZ origin = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, feetToMeter);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            // Namespacemanager for used namespaces, e.g. in XPlanung GML or ALKIS XML files
            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            #region parameter

            Category category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography);
            Category projCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);
            CategorySet categorySet = app.Create.NewCategorySet();
            CategorySet projCategorySet = app.Create.NewCategorySet();
            categorySet.Insert(category);
            projCategorySet.Insert(projCategory);

            //create shared parameter file
            string modulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
            string sharedParamFile = Path.Combine(modulePath, "City2BIM_Parameters.txt");

            if (!File.Exists(sharedParamFile))
            {
                FileStream fs = File.Create(sharedParamFile);
                fs.Close();
            }

            #endregion parameter  

            // Creates Project Information for revit project like general data or postal address
            City2RVT.GUI.XPlan2BIM.XPlan_Parameter parameter = new XPlan_Parameter();
            DefinitionFile defFile = default(DefinitionFile);
            var projInformation = new City2RVT.Builder.Revit_Semantic(doc);
            projInformation.CreateProjectInformation(app, doc, projCategorySet, parameter, defFile);   

            // Selected Layer for beeing shown in revit view
            var selectedLayers = categoryListbox.SelectedItems;
            var revitView = commandData.Application.ActiveUIDocument.ActiveView as View3D;

            var selectedParams = GUI.Prop_NAS_settings.SelectedParams;

            List<string> xPlanObjectList = new List<string>();
            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

            // The big surfaces of BP_Bereich and BP_Plan getting brought to the bottom so they do not overlap other surfaces in Revit
            // Alternatively they could be represented as borders instead of areas 
            foreach (XmlNode x in allXPlanObjects)
            {
                if (x.FirstChild.SelectNodes(".//gml:exterior", nsmgr) != null)
                {
                    if (xPlanObjectList.Contains(x.FirstChild.Name.ToString()) == false)
                    {
                        if (x.FirstChild.Name.ToString() == "xplan:BP_Bereich")
                        {
                            xPlanObjectList.Insert(0, x.FirstChild.Name.ToString());
                        }
                        else if (x.FirstChild.Name.ToString() == "xplan:BP_Plan")
                        {
                            xPlanObjectList.Insert(0, x.FirstChild.Name.ToString());
                        }
                        else
                        {
                            xPlanObjectList.Add(x.FirstChild.Name.ToString());
                        }
                    }
                } 
            }

            ElementId xPlanReferencePlaneId = default(ElementId);
            double zOffset = 0.0;
            if (selectedLayers.Count != 0)
            {
                foreach (var xPlanObject in xPlanObjectList)
                {
                    if (selectedLayers.Contains(xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)))
                    {
                        #region reference plane
                        XmlNodeList xPlanExterior = xmlDoc.SelectNodes("//gml:featureMember//gml:exterior", nsmgr);
                        Dictionary<string, XYZ[]> xPlanPointDict = new Dictionary<string, XYZ[]>();

                        List<string> xPlanReference = new List<String>();
                        int xPlanCountReference = 0;

                        List<double> xPlanAllValues = new List<double>();
                        List<double> xPlanXValues = new List<double>();
                        List<double> xPlanYValues = new List<double>();

                        if (xPlanExterior.Count > 0)
                        {
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
                            var hideReferencePlanes = new List<ElementId>();

                            foreach (var referencePoints in xPlanPointDict)
                            {
                                Transaction referencePlanes = new Transaction(doc, "Reference plane: " + (referencePoints.Key).Substring((referencePoints.Key).LastIndexOf(':') + 1));
                                {
                                    FailureHandlingOptions options = referencePlanes.GetFailureHandlingOptions();
                                    options.SetFailuresPreprocessor(new AxesFailure());
                                    referencePlanes.SetFailureHandlingOptions(options);

                                    referencePlanes.Start();
                                    SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                                    TopographySurface referencePlane = TopographySurface.Create(doc, referencePoints.Value);

                                    ElementId farbeReference = colorDict["transparent"];

                                    Parameter materialParam = referencePlane.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                                    materialParam.Set(farbeReference);

                                    Parameter gesamt = referencePlane.LookupParameter("Kommentare");
                                    gesamt.Set("Reference plane: " + (referencePoints.Key).Substring(6));
                                    xPlanReferencePlaneId = referencePlane.Id;

                                    hideReferencePlanes.Add(referencePlane.Id);

                                    if (selectedLayers.Contains((referencePoints.Key).Substring((referencePoints.Key).LastIndexOf(':') + 1)) == false)
                                    {
                                        revitView.HideElements(hideReferencePlanes);
                                    }

                                    logger.Info("Reference plane: '" + (referencePoints.Key).Substring((referencePoints.Key).LastIndexOf(':') + 1) + "' created.");
                                }
                                referencePlanes.Commit();
                            }
                        }
                        #endregion reference plane

                        #region exterior

                        XmlNodeList bpEinzelnExterior = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "//gml:exterior", nsmgr);

                        List<string> positionList = new List<String>();
                        List<string> paramList = new List<String>();
                        List<string> interiorListe = new List<String>();
                        int i = 0;
                        foreach (XmlNode nodeExt in bpEinzelnExterior)
                        {
                            List<CurveLoop> curveLoopExteriorList = new List<CurveLoop>();
                            CurveLoop curveLoop = new CurveLoop();
                            int ii = 0;
                            foreach (XmlNode interiorNode in nodeExt.ParentNode.ChildNodes)
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
                                            var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                            Line lineStrasse = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                                            curveLoopInterior.Append(lineStrasse);
                                        }

                                        else if (koordWerteInterior.Count() > 4)
                                        {
                                            int ia = 0;

                                            foreach (string split in koordWerteInterior)
                                            {
                                                var geomBuilder = new Builder.RevitXPlanBuilder(doc);
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
                                            var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                            Line lineStrasse = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                                            curveLoopInterior.Append(lineStrasse);
                                        }

                                        else if (koordWerteInterior.Count() > 4)
                                        {
                                            int ib = 0;
                                            foreach (string split in koordWerteInterior)
                                            {
                                                var geomBuilder = new Builder.RevitXPlanBuilder(doc);
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
                                    curveLoopExteriorList.Add(curveLoopInterior);
                                }
                            }


                            XmlNodeList exterior = nodeExt.SelectNodes("gml:LinearRing/gml:posList", nsmgr);
                            XmlNodeList exteriorRing = nodeExt.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);

                            foreach (XmlNode child in nodeExt.ParentNode.ParentNode.ParentNode)
                            {
                                defFile = parameter.CreateDefinitionFile(sharedParamFile, app, doc, child.Name.Substring(child.Name.LastIndexOf(':') + 1), "XPlanDaten");
                                if (child.Name != "#comment")
                                {
                                    paramList.Add(child.Name);
                                }
                            }

                            foreach (XmlNode nodePosExt in exterior)
                            {
                                positionList.Add(nodePosExt.InnerText);
                                string[] koordWerte = positionList[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                if (koordWerte.Count() == 4)
                                {
                                    var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                    Line lineStrasse = geomBuilder.CreateLineString(koordWerte, R, transf, zOffset);
                                    curveLoop.Append(lineStrasse);
                                }

                                else if (koordWerte.Count() > 4)
                                {
                                    int ic = 0;

                                    foreach (string split in koordWerte)
                                    {
                                        var geomBuilder = new Builder.RevitXPlanBuilder(doc);
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

                            foreach (XmlNode nodePosExt in exteriorRing)
                            {
                                positionList.Add(nodePosExt.InnerText);
                                string[] koordWerte = positionList[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                if (koordWerte.Count() == 4)
                                {
                                    var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                    Line lineStrasse = geomBuilder.CreateLineString(koordWerte, R, transf, zOffset);
                                    curveLoop.Append(lineStrasse);
                                }

                                else if (koordWerte.Count() > 4)
                                {
                                    int ie = 0;

                                    foreach (string split in koordWerte)
                                    {
                                        var geomBuilder = new Builder.RevitXPlanBuilder(doc);
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

                            #region parameter
                            string nodeContent = default;
                            Dictionary<string, string> paramDict = new Dictionary<string, string>();

                            foreach (DefinitionGroup dg in defFile.Groups)
                            {
                                foreach (var paramName in paramList)
                                {
                                    if (dg.Name == "XPlanDaten")
                                    {
                                        XmlNode objektBezeichnung = nodeExt.ParentNode.ParentNode.ParentNode;
                                        var parameterBezeichnung = objektBezeichnung.SelectNodes(paramName, nsmgr);

                                        if (selectedParams.Contains(paramName))
                                        {
                                            if (parameterBezeichnung != null)
                                            {
                                                ExternalDefinition externalDefinition = dg.Definitions.get_Item(paramName.Substring(paramName.LastIndexOf(':') + 1)) as ExternalDefinition;

                                                var getNodeContent = new XPlan2BIM.XPlan_Parameter();
                                                nodeContent = getNodeContent.getNodeText(nodeExt, nsmgr, xPlanObject, paramName.Substring(paramName.LastIndexOf(':') + 1));

                                                if (paramDict.ContainsKey(paramName.Substring(paramName.LastIndexOf(':') + 1)) == false)
                                                {
                                                    paramDict.Add(paramName.Substring(paramName.LastIndexOf(':') + 1), nodeContent);
                                                }

                                                Transaction tParam = new Transaction(doc, "Insert Parameter");
                                                {
                                                    tParam.Start();
                                                    InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                                                    if (externalDefinition != null)
                                                    {
                                                        doc.ParameterBindings.Insert(externalDefinition, newIB, BuiltInParameterGroup.PG_DATA);
                                                    }
                                                    logger.Info("Applied Parameters to '" + paramName.Substring(paramName.LastIndexOf(':') + 1) + "'. ");
                                                }
                                                tParam.Commit();
                                            }
                                        }
                                    }
                                }
                            }
                            paramList.Clear();
                            #endregion parameter

                            if (curveLoop.GetExactLength() > 0)
                            {
                                curveLoopExteriorList.Add(curveLoop);
                                var hideReferenceSitesubregions = new List<ElementId>();

                                Transaction topoTransaction = new Transaction(doc, "Create Exterior");
                                {
                                    FailureHandlingOptions optionsExterior = topoTransaction.GetFailureHandlingOptions();
                                    optionsExterior.SetFailuresPreprocessor(new AxesFailure());
                                    topoTransaction.SetFailureHandlingOptions(optionsExterior);

                                    topoTransaction.Start();
                                    SketchPlane sketchExterior = SketchPlane.Create(doc, geomPlane);
                                    SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopExteriorList, xPlanReferencePlaneId);

                                    Parameter materialParamExterior = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);

                                    ElementId farbe = default(ElementId);
                                    if (colorDict.ContainsKey(xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)))
                                    {
                                        farbe = colorDict[xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)];
                                    }
                                    else
                                    {
                                        farbe = colorDict["default"];
                                    }
                                    materialParamExterior.Set(farbe);

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

                                    }

                                    Parameter exteriorName = siteSubRegion.TopographySurface.LookupParameter("Kommentare");
                                    exteriorName.Set(xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1));

                                    hideReferenceSitesubregions.Add(siteSubRegion.TopographySurface.Id);

                                    if (selectedLayers.Contains(xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)) == false)
                                    {
                                        revitView.HideElements(hideReferenceSitesubregions);
                                    }

                                    logger.Info("Created sitesubregion for '" + xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1) + "' (Exterior). ");
                                }
                                topoTransaction.Commit();
                            }
                            paramDict.Clear();
                        }
                        #endregion exterior             

                        if (checkBoxZOffset.IsChecked == true)
                        {
                            zOffset += 10.0;
                        }
                        else
                        {
                            zOffset += 0;
                        }
                    }
                }
            }
            else
            {
                TaskDialog.Show("No layer selected", "You have to select at least one layer to start the import. ");
            }

        }

        private void Xplan_file_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_ClearSelection(object sender, RoutedEventArgs e)
        {
            categoryListbox.UnselectAll();
            radioButton1.IsChecked = false;
        }

        private void Button_Click_ApplyXPlanFile(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            string xPlanGmlPath = xplan_file.Text;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            XmlDocument xmlDoc = new XmlDocument();

            using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
            {
                xmlDoc.Load(reader);
                xmlDoc.Load(xPlanGmlPath);
            }

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            List<string> xPlanObjectList = new List<string>();
            List<string> allParamList = new List<string>();

            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

            foreach (XmlNode xmlNode in allXPlanObjects)
            {
                if (xmlNode.FirstChild.SelectNodes(".//gml:exterior", nsmgr) != null)
                {
                    if (xPlanObjectList.Contains(xmlNode.FirstChild.Name.Substring((xmlNode.FirstChild.Name).LastIndexOf(':') + 1 )) == false)
                    {
                        xPlanObjectList.Add(xmlNode.FirstChild.Name.Substring((xmlNode.FirstChild.Name).LastIndexOf(':') + 1 ));
                    }
                }

                foreach (XmlNode child in xmlNode.FirstChild)
                {
                    if (child.Name != "#comment")
                    {
                        if (allParamList.Contains(child.Name) == false)
                        {
                            allParamList.Add(child.Name);
                        }                        
                    }
                }
            }



            xPlanObjectList.Sort();
            allParamList.Sort();

            GUI.Prop_NAS_settings.ParamList = allParamList;

            int ix = 0;
            foreach (string item in xPlanObjectList)
            {
                categoryListbox.Items.Add(xPlanObjectList[ix]);
                ix++;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryListbox.SelectedItems.Count < categoryListbox.Items.Count)
            {
                radioButton1.IsChecked = false;
            }
        }

        private void Button_Click_CloseXplanForm(object sender, RoutedEventArgs e)
        {
            Prop_XPLAN_settings.FileUrl = xplan_file.Text;
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (radioButton1.IsChecked == true)
            {
                categoryListbox.SelectAll();
            }
            else
            {
                categoryListbox.UnselectAll();

            }
        }

        private void checkBoxZOffset_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Modify.Form1 f1 = new Modify.Form1();
            f1.ShowDialog();
        }
    }
}
