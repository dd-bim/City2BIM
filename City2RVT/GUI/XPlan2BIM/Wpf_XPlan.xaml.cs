using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan.xaml
    /// </summary>
    public partial class Wpf_XPlan : Window
    {
        ExternalCommandData commandData;
        double feetToMeter = 1.0 / 0.3048;
        //private readonly Dictionary<ColorType, ElementId> colors;


        public Wpf_XPlan(ExternalCommandData cData)
        {
            //this.colors = CreateMaterial();

            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        //private void Wpf_XPlan_Load(object sender, EventArgs e)
        //{
        //    this.size.Size = new System.Drawing.Size(300, 300);
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Reader.FileDialog winexp = new Reader.FileDialog();
            xplan_file.Text = winexp.ImportPath(Reader.FileDialog.Data.XPlanGML);
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            var colorList = new Dictionary<string, ElementId>();
            var materialBuilder = new Builder.RevitXPlanBuilder(doc);
            colorList = materialBuilder.CreateMaterial();

            #region Transformation und UTM-Reduktion

            //Zuerst wird die Position des Projektbasispunkts bestimmt
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;
            double elevation = position_data.Elevation;
            double easting = position_data.EastWest;
            double northing = position_data.NorthSouth;

            // Der Ostwert des PBB wird als mittlerer Ostwert für die UTM Reduktion verwendet.
            double xSchwPktFt = easting;
            double xSchwPktKm = (double)((xSchwPktFt / feetToMeter) / 1000);
            double xSchwPkt500 = xSchwPktKm - 500;
            double R = 1;

            Transform trot = Transform.CreateRotation(XYZ.BasisZ, -angle);
            XYZ vector = new XYZ(easting, northing, elevation);
            XYZ vectorRedu = vector / R;
            Transform ttrans = Transform.CreateTranslation(-vectorRedu);
            Transform transf = trot.Multiply(ttrans);
            #endregion Transformation und UTM-Reduktion  

            List<double> xList = new List<double>();
            List<double> yList = new List<double>();

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

            #region namespaces
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xplan", "http://www.xplanung.de/xplangml/5/2");
            #endregion namespaces

            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

            #region parameter

            Category category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography);
            CategorySet categorySet = app.Create.NewCategorySet();
            categorySet.Insert(category);

            string paramFile = @"D:\Daten\LandBIM\AP 1\Plugin\city2bim\SharedParameterFile.txt";
            var parameter = new XPlan2BIM.XPlan_Parameter();
            //DefinitionFile defFile = parameter.CreateDefinitionFile(paramFile, app, doc);
            DefinitionFile defFile = default(DefinitionFile);

            #endregion parameter

            List<string> xPlanObjectList = new List<string>();
            foreach (XmlNode x in allXPlanObjects)
            {
                if (x.SelectNodes("//gml:exterior", nsmgr) != null)
                {
                    if (xPlanObjectList.Contains(x.FirstChild.Name.ToString()) == false)
                    {
                        xPlanObjectList.Add(x.FirstChild.Name.ToString());
                    }
                }  
                
                foreach (XmlNode cn in x.FirstChild)
                {
                    //System.Windows.Forms.MessageBox.Show(cn.Name.ToString());
                }
            }

            ElementId xPlanReferencePlaneId = default(ElementId);
            double zOffset = 0.0;
            foreach (var xPlanObject in xPlanObjectList)
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

                    foreach (var referencePoints in xPlanPointDict)
                    {
                        Transaction referencePlanes = new Transaction(doc, "Reference plane: " + (referencePoints.Key).Substring(6));
                        {
                            FailureHandlingOptions options = referencePlanes.GetFailureHandlingOptions();
                            options.SetFailuresPreprocessor(new AxesFailure());
                            referencePlanes.SetFailureHandlingOptions(options);

                            referencePlanes.Start();
                            SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                            TopographySurface referencePlane = TopographySurface.Create(doc, referencePoints.Value);

                            ElementId farbeReference = colorList["transparent"];

                            Parameter materialParam = referencePlane.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                            materialParam.Set(farbeReference);

                            Parameter gesamt = referencePlane.LookupParameter("Kommentare");
                            gesamt.Set("Reference plane: " + (referencePoints.Key).Substring(6));
                            xPlanReferencePlaneId = referencePlane.Id;
                        }
                        referencePlanes.Commit();
                    }
                }
                #endregion reference plane

                #region exterior

                XmlNodeList bpEinzelnExterior = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "//gml:exterior", nsmgr);

                List<string> positionList = new List<String>();
                List<string> paramList = new List<String>();
                int i = 0;
                foreach (XmlNode nodeExt in bpEinzelnExterior)
                {
                    List<CurveLoop> curveLoopStrasseList = new List<CurveLoop>();
                    CurveLoop curveLoopStrasse = new CurveLoop();
                    XmlNodeList exterior = nodeExt.SelectNodes("gml:LinearRing/gml:posList", nsmgr);
                    XmlNodeList exteriorRing = nodeExt.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);

                    foreach (XmlNode child in nodeExt.ParentNode.ParentNode.ParentNode)
                    {
                        defFile = parameter.CreateDefinitionFile(paramFile, app, doc, child.Name.Substring(6));
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
                            curveLoopStrasse.Append(lineStrasse);
                        }

                        else if (koordWerte.Count() > 4)
                        {
                            int iSplit = 0;

                            foreach (string split in koordWerte)
                            {
                                var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                Line lineClIndu = geomBuilder.CreateLineRing(koordWerte, R, transf, iSplit, zOffset);
                                curveLoopStrasse.Append(lineClIndu);

                                if ((iSplit + 3) == (koordWerte.Count() - 1))
                                {
                                    break;
                                }
                                iSplit += 2;
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
                            curveLoopStrasse.Append(lineStrasse);
                        }

                        else if (koordWerte.Count() > 4)
                        {
                            int iSplit = 0;

                            foreach (string split in koordWerte)
                            {
                                var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                Line lineClIndu = geomBuilder.CreateLineRing(koordWerte, R, transf, iSplit, zOffset);
                                curveLoopStrasse.Append(lineClIndu);

                                if ((iSplit + 3) == (koordWerte.Count() - 1))
                                {
                                    break;
                                }

                                iSplit += 2;
                            }
                        }
                        i++;
                    }

                    #region parameter
                    string rechtsstandString = default;
                    string ebeneString = default;
                    string rechtscharakterString = default;
                    string flaechenschlussString = default;
                    string nutzungsformString = default;

                    foreach (DefinitionGroup dg in defFile.Groups)
                    {
                        foreach (var paramName in paramList)
                        {
                            if (dg.Name == "XPlanDaten")
                            {
                                XmlNode objektBezechnung = nodeExt.ParentNode.ParentNode.ParentNode;
                                var parameterBezeichnung = objektBezechnung.SelectNodes(paramName, nsmgr);

                                if (parameterBezeichnung != null)
                                {
                                    ExternalDefinition externalDefinition = dg.Definitions.get_Item(paramName.Substring(6)) as ExternalDefinition;
                                    ExternalDefinition rechtsstandExtDef = dg.Definitions.get_Item("rechtsstand") as ExternalDefinition;
                                    ExternalDefinition ebeneExtDef = dg.Definitions.get_Item("ebene") as ExternalDefinition;
                                    ExternalDefinition rechtscharakterExtDef = dg.Definitions.get_Item("rechtscharakter") as ExternalDefinition;
                                    ExternalDefinition flaechenschlussExtDef = dg.Definitions.get_Item("flaechenschluss") as ExternalDefinition;
                                    ExternalDefinition nutzungsformExtDef = dg.Definitions.get_Item("nutzungsform") as ExternalDefinition;

                                    var getNodeText = new XPlan2BIM.XPlan_Parameter();
                                    //rechtsstandString = getNodeText.getNodeText(nodeExt, nsmgr, xPlanObject, paramName.Substring(6));
                                    rechtsstandString = getNodeText.getNodeText(nodeExt, nsmgr, xPlanObject, "rechtsstand");
                                    ebeneString = getNodeText.getNodeText(nodeExt, nsmgr, xPlanObject, "ebene");
                                    rechtscharakterString = getNodeText.getNodeText(nodeExt, nsmgr, xPlanObject, "rechtscharakter");
                                    flaechenschlussString = getNodeText.getNodeText(nodeExt, nsmgr, xPlanObject, "flaechenschluss");
                                    nutzungsformString = getNodeText.getNodeText(nodeExt, nsmgr, xPlanObject, "nutzungsform");

                                    Transaction tParam = new Transaction(doc, "Insert Parameter");
                                    {
                                        tParam.Start();
                                        InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                                        //if (externalDefinition != null)
                                        //{
                                        //    doc.ParameterBindings.Insert(externalDefinition, newIB, BuiltInParameterGroup.PG_DATA);
                                        //}
                                        if (rechtsstandExtDef != null)
                                        {
                                            doc.ParameterBindings.Insert(rechtsstandExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        }
                                        if (ebeneExtDef != null)
                                        {
                                            doc.ParameterBindings.Insert(ebeneExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        }
                                        if (rechtscharakterExtDef != null)
                                        {
                                            doc.ParameterBindings.Insert(rechtscharakterExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        }
                                        if (flaechenschlussExtDef != null)
                                        {
                                            doc.ParameterBindings.Insert(flaechenschlussExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        }
                                        if (nutzungsformExtDef != null)
                                        {
                                            doc.ParameterBindings.Insert(nutzungsformExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        }
                                        //doc.ParameterBindings.Insert(ebeneExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        //doc.ParameterBindings.Insert(rechtscharakterExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        //doc.ParameterBindings.Insert(flaechenschlussExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                        //doc.ParameterBindings.Insert(nutzungsformExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                                    }
                                    tParam.Commit();
                                }
                            }   
                        }
                    }
                    #endregion parameter

                    if (curveLoopStrasse.GetExactLength() > 0)
                    {
                        curveLoopStrasseList.Add(curveLoopStrasse);

                        Transaction topoTransaction = new Transaction(doc, "Exterior");
                        {
                            FailureHandlingOptions optionsExterior = topoTransaction.GetFailureHandlingOptions();
                            optionsExterior.SetFailuresPreprocessor(new AxesFailure());
                            topoTransaction.SetFailureHandlingOptions(optionsExterior);

                            topoTransaction.Start();
                            SketchPlane sketchExterior = SketchPlane.Create(doc, geomPlane);
                            SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseList, xPlanReferencePlaneId);

                            Parameter materialParamExterior = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);

                            ElementId farbe = default(ElementId);
                            if (colorList.ContainsKey(xPlanObject.Substring(6)))
                            {
                                farbe = colorList[xPlanObject.Substring(6)];
                            }
                            else
                            {
                                farbe = colorList["default"];
                            }
                            materialParamExterior.Set(farbe);

                            try
                            {
                                Parameter rechtsstandParameter = siteSubRegion.TopographySurface.LookupParameter("rechtsstand");
                                rechtsstandParameter.Set(rechtsstandString);
                                Parameter ebeneParameter = siteSubRegion.TopographySurface.LookupParameter("ebene");
                                ebeneParameter.Set(ebeneString);
                                Parameter rechtscharakterParameter = siteSubRegion.TopographySurface.LookupParameter("rechtscharakter");
                                rechtscharakterParameter.Set(rechtscharakterString);
                                Parameter flaechenschlussParameter = siteSubRegion.TopographySurface.LookupParameter("flaechenschluss");
                                flaechenschlussParameter.Set(flaechenschlussString);
                                Parameter nutzungsformParameter = siteSubRegion.TopographySurface.LookupParameter("nutzungsform");
                                nutzungsformParameter.Set(nutzungsformString);
                            }
                            catch
                            {

                            }

                            Parameter exteriorName = siteSubRegion.TopographySurface.LookupParameter("Kommentare");
                            exteriorName.Set(xPlanObject.Substring(6));
                        }
                        topoTransaction.Commit();
                    }
                }
                #endregion exterior

                #region interior

                XmlNodeList bpEinzelnInterior = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "//gml:interior", nsmgr);

                List<string> strasseInteriorListe = new List<String>();

                int ii = 0;

                foreach (XmlNode xn in bpEinzelnInterior)
                {
                    List<CurveLoop> curveLoopStrasseInteriorList = new List<CurveLoop>();
                    CurveLoop curveLoopStrasseInterior = new CurveLoop();
                    XmlNodeList strassenVerkehrInterior = xn.SelectNodes("gml:LinearRing/gml:posList", nsmgr);
                    XmlNodeList strassenVerkehrInteriorRing = xn.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);

                    foreach (XmlNode xc in strassenVerkehrInterior)
                    {
                        strasseInteriorListe.Add(xc.InnerText);
                        string[] koordWerteInterior = strasseInteriorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (koordWerteInterior.Count() == 4)
                        {
                            var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                            Line lineStrasse = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                            curveLoopStrasseInterior.Append(lineStrasse);
                        }

                        else if (koordWerteInterior.Count() > 4)
                        {
                            int iSplit = 0;

                            foreach (string split in koordWerteInterior)
                            {
                                var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                Line lineClIndu = geomBuilder.CreateLineRing(koordWerteInterior, R, transf, iSplit, zOffset);
                                curveLoopStrasseInterior.Append(lineClIndu);

                                if ((iSplit + 3) == (koordWerteInterior.Count() - 1))
                                {
                                    break;
                                }
                                iSplit += 2;
                            }                          
                        }
                        ii++;
                    }

                    foreach (XmlNode xc in strassenVerkehrInteriorRing)
                    {
                        strasseInteriorListe.Add(xc.InnerText);
                        string[] koordWerteInterior = strasseInteriorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (koordWerteInterior.Count() == 4)
                        {
                            var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                            Line lineStrasse = geomBuilder.CreateLineString(koordWerteInterior, R, transf, zOffset);
                            curveLoopStrasseInterior.Append(lineStrasse);
                        }

                        else if (koordWerteInterior.Count() > 4)
                        {
                            int iSplit = 0;
                            foreach (string split in koordWerteInterior)
                            {
                                var geomBuilder = new Builder.RevitXPlanBuilder(doc);
                                Line lineClIndu = geomBuilder.CreateLineRing(koordWerteInterior, R, transf, iSplit, zOffset);
                                curveLoopStrasseInterior.Append(lineClIndu);

                                if ((iSplit + 3) == (koordWerteInterior.Count() - 1))
                                {
                                    break;
                                }

                                iSplit += 2;
                            }
                        }
                        ii++;
                    }

                    if (curveLoopStrasseInterior.GetExactLength() > 0)
                    {
                        curveLoopStrasseInteriorList.Add(curveLoopStrasseInterior);

                        if (curveLoopStrasseInteriorList.Count() > 0)
                        {
                            Transaction topoTransactionInterior = new Transaction(doc, "Interior");
                            {
                                FailureHandlingOptions optionsInt = topoTransactionInterior.GetFailureHandlingOptions();
                                optionsInt.SetFailuresPreprocessor(new AxesFailure());
                                topoTransactionInterior.SetFailureHandlingOptions(optionsInt);

                                topoTransactionInterior.Start();
                                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                                SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseInteriorList, xPlanReferencePlaneId);

                                ElementId farbeInterior = colorList["interior"];
                                Parameter materialParam = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                                materialParam.Set(farbeInterior);
                            }
                            topoTransactionInterior.Commit();
                        }
                    }
                }
                #endregion interior

                zOffset += 10.0;
            }
        }
    }
}
