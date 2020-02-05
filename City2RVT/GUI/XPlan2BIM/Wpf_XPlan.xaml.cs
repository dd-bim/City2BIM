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

        public Wpf_XPlan(ExternalCommandData cData)
        {
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
            //Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            var transparentMaterialId = default(ElementId);
            var exteriorMaterialId = default(ElementId);
            var interiorMaterialId = default(ElementId);


            #region material
            Transaction tMaterial = new Transaction(doc, "Creates Material");
            {
                FailureHandlingOptions options = tMaterial.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new AxesFailure());
                tMaterial.SetFailureHandlingOptions(options);

                tMaterial.Start();

                transparentMaterialId = Material.Create(doc, "transparent");
                Material referenceMaterial = doc.GetElement(transparentMaterialId) as Material;
                referenceMaterial.Transparency = 100;

                exteriorMaterialId = Material.Create(doc, "exterior");
                Material exteriorMaterial = doc.GetElement(exteriorMaterialId) as Material;
                exteriorMaterial.Color = new Color(132, 112, 255);

                interiorMaterialId = Material.Create(doc, "interior");
                Material interiorMaterial = doc.GetElement(interiorMaterialId) as Material;
                interiorMaterial.Color = new Color(240, 230, 140);


                ////Create a new property set that can be used by this material
                //StructuralAsset strucAsset = new StructuralAsset("My Property Set", StructuralAssetClass.Concrete);
                //strucAsset.Behavior = StructuralBehavior.Isotropic;
                //strucAsset.Density = 232.0;

                ////Assign the property set to the material.
                //PropertySetElement pse = PropertySetElement.Create(doc, strucAsset);
                //referenceMaterial.SetMaterialAspectByPropertySet(MaterialAspect.Structural, pse.Id);

            }
            tMaterial.Commit();
            #endregion material

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

            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.Load(xPlanGmlPath);

            #region namespaces
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xplan", "http://www.xplanung.de/xplangml/5/2");

            #endregion namespaces

            //XmlNodeList strasseExterior = xmlDoc.SelectNodes("//gml:featureMember/xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:exterior/gml:LinearRing/gml:posList", nsmgr);
            XmlNodeList exteriorGesamt = xmlDoc.SelectNodes("//gml:exterior", nsmgr);
            XmlNodeList interiorGesamt = xmlDoc.SelectNodes("//gml:exterior", nsmgr);

            //XmlNodeList strasseExterior = xmlDoc.SelectNodes("//gml:featureMember/xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:exterior", nsmgr);
            XmlNodeList strasseExterior = xmlDoc.SelectNodes("//gml:exterior", nsmgr);

            //xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/
            //xplan:BP_UeberbaubareGrundstuecksFlaeche/xplan:position/gml:Polygon/

            XmlNodeList testExterior = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

            //if (testExterior.)

            List<string> bpObjects = new List<string>();
            foreach (XmlNode x in testExterior)
            {
                if (x.SelectNodes("//gml:exterior", nsmgr) != null)
                {
                    if (bpObjects.Contains(x.FirstChild.Name.ToString()) == false)
                    {
                        bpObjects.Add(x.FirstChild.Name.ToString());
                        //System.Windows.Forms.MessageBox.Show(x.FirstChild.Name.ToString());

                    }
                }                
            }

            ElementId bpReferencePlaneId = default(ElementId);
            foreach (var y in bpObjects)
            {
                XmlNodeList bpExterior = xmlDoc.SelectNodes("//gml:featureMember//gml:exterior", nsmgr);

                //XmlNodeList bpExterior = xmlDoc.SelectNodes("//gml:featureMember/" + y + "//gml:exterior", nsmgr);
                //XmlNodeList bpExterior = xmlDoc.SelectNodes("//gml:featureMember/" + y + "/xplan:position/gml:Polygon/gml:exterior", nsmgr);

                #region reference plane
                Dictionary<string, XYZ[]> bpPointList = new Dictionary<string, XYZ[]>();

                List<string> bpStrasseBezug = new List<String>();
                int bpCountBezug = 0;

                List<double> bpAllValues = new List<double>();
                List<double> bpxValues = new List<double>();
                List<double> bpyValues = new List<double>();

                //foreach (XmlNode strasseNode in exteriorGesamt)
                //foreach (XmlNode strasseNode in strasseExterior)

                if (bpExterior.Count > 0)
                {
                    foreach (XmlNode strasseNode in bpExterior)
                    {
                        bpStrasseBezug.Add(strasseNode.InnerText);
                        string[] koordWerteBezug = bpStrasseBezug[bpCountBezug].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var x in koordWerteBezug)
                        {
                            double values_double = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
                            bpAllValues.Add(values_double);
                        }

                        for (int ix = 0; ix < bpAllValues.Count; ix += 2)
                        {
                            bpxValues.Add(bpAllValues[ix]);
                        }

                        for (int iy = 1; iy < bpAllValues.Count; iy += 2)
                        {
                            bpyValues.Add(bpAllValues[iy]);
                        }

                        bpCountBezug++;
                    }

                    //System.Windows.Forms.MessageBox.Show(y.ToString());
                    //System.Windows.Forms.MessageBox.Show(bpxValues.Min().ToString());

                    double bpxMin = (bpxValues.Min() * feetToMeter) / R;
                    double bpxMax = (bpxValues.Max() * feetToMeter) / R;
                    double bpyMin = (bpyValues.Min() * feetToMeter) / R;
                    double bpyMax = (bpyValues.Max() * feetToMeter) / R;

                    XYZ[] bpPointsExterior = new XYZ[4];
                    bpPointsExterior[0] = transf.OfPoint(new XYZ(bpxMin, bpyMin, 0.0));
                    bpPointsExterior[1] = transf.OfPoint(new XYZ(bpxMax, bpyMin, 0.0));
                    bpPointsExterior[2] = transf.OfPoint(new XYZ(bpxMax, bpyMax, 0.0));
                    bpPointsExterior[3] = transf.OfPoint(new XYZ(bpxMin, bpyMax, 0.0));

                    bpPointList.Add(y, bpPointsExterior);

                    foreach (var referencePoints in bpPointList)
                    {
                        Transaction referencePlanes = new Transaction(doc, "Reference plane: " + referencePoints.Key);
                        {
                            FailureHandlingOptions options = referencePlanes.GetFailureHandlingOptions();
                            options.SetFailuresPreprocessor(new AxesFailure());
                            referencePlanes.SetFailureHandlingOptions(options);

                            referencePlanes.Start();
                            SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                            TopographySurface referencePlane = TopographySurface.Create(doc, referencePoints.Value);

                            Parameter materialParam = referencePlane.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                            materialParam.Set(transparentMaterialId);

                            Parameter gesamt = referencePlane.LookupParameter("Kommentare");
                            gesamt.Set("Reference plane: " + referencePoints.Key);
                            bpReferencePlaneId = referencePlane.Id;
                        }
                        referencePlanes.Commit();
                    }
                }
                #endregion reference plane

                XmlNodeList bpEinzelnExterior = xmlDoc.SelectNodes("//gml:featureMember/" + y + "//gml:exterior", nsmgr);

                #region exterior

                List<string> list = new List<String>();

                int i = 0;
                //foreach (XmlNode nodeExt in strasseExterior)
                foreach (XmlNode nodeExt in bpEinzelnExterior)
                {
                    List<CurveLoop> curveLoopStrasseList = new List<CurveLoop>();
                    CurveLoop curveLoopStrasse = new CurveLoop();

                    XmlNodeList strassenVerkehrExterior = nodeExt.SelectNodes("gml:LinearRing/gml:posList", nsmgr);

                    foreach (XmlNode nodePosExt in strassenVerkehrExterior)
                    {
                        list.Add(nodePosExt.InnerText);

                        string[] koordWerte = list[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (koordWerte.Count() == 4)
                        {
                            double xStart = Convert.ToDouble(koordWerte[0], System.Globalization.CultureInfo.InvariantCulture);
                            double xStartMeter = xStart * feetToMeter;
                            double xStartMeterRedu = xStartMeter / R;
                            double yStart = Convert.ToDouble(koordWerte[1], System.Globalization.CultureInfo.InvariantCulture);
                            double yStartMeter = yStart * feetToMeter;
                            double yStartMeterRedu = yStartMeter / R;
                            double zStart = 0.000;
                            double zStartMeter = zStart * feetToMeter;

                            double xEnd = Convert.ToDouble(koordWerte[2], System.Globalization.CultureInfo.InvariantCulture);
                            double xEndMeter = xEnd * feetToMeter;
                            double xEndMeterRedu = xEndMeter / R;
                            double yEnd = Convert.ToDouble(koordWerte[3], System.Globalization.CultureInfo.InvariantCulture);
                            double yEndMeter = yEnd * feetToMeter;
                            double yEndMeterRedu = yEndMeter / R;
                            double zEnd = 0.000;
                            double zEndMeter = zEnd * feetToMeter;

                            XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
                            XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

                            XYZ transfStartPoint = transf.OfPoint(startPoint);
                            XYZ transfEndPoint = transf.OfPoint(endPoint);

                            Line lineStrasse = Line.CreateBound(transfStartPoint, transfEndPoint);

                            curveLoopStrasse.Append(lineStrasse);
                        }

                        else if (koordWerte.Count() > 4)
                        {
                            int iSplit = 0;

                            foreach (string split in koordWerte)
                            {
                                double xStart = Convert.ToDouble(koordWerte[iSplit], System.Globalization.CultureInfo.InvariantCulture);
                                double xStartMeter = xStart * feetToMeter;
                                double xStartMeterRedu = xStartMeter / R;
                                double yStart = Convert.ToDouble(koordWerte[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
                                double yStartMeter = yStart * feetToMeter;
                                double yStartMeterRedu = yStartMeter / R;
                                double zStart = 0.000;
                                double zStartMeter = zStart * feetToMeter;

                                double xEnd = Convert.ToDouble(koordWerte[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
                                double xEndMeter = xEnd * feetToMeter;
                                double xEndMeterRedu = xEndMeter / R;
                                double yEnd = Convert.ToDouble(koordWerte[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
                                double yEndMeter = yEnd * feetToMeter;
                                double yEndMeterRedu = yEndMeter / R;
                                double zEnd = 0.000;
                                double zEndMeter = zEnd * feetToMeter;

                                XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
                                XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

                                XYZ tStartPoint = transf.OfPoint(startPoint);
                                XYZ tEndPoint = transf.OfPoint(endPoint);

                                if (tStartPoint.DistanceTo(tEndPoint) > 0)
                                {
                                    Line lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);

                                    curveLoopStrasse.Append(lineClIndu);
                                }

                                if ((iSplit + 3) == (koordWerte.Count() - 1))
                                {
                                    break;
                                }

                                iSplit += 2;
                            }
                        }
                        i++;
                    }

                    if (curveLoopStrasse.GetExactLength() > 0)
                    {
                        curveLoopStrasseList.Add(curveLoopStrasse);
                        //System.Windows.Forms.MessageBox.Show(curveLoopStrasse.Count().ToString());

                        Transaction topoTransaction = new Transaction(doc, "Strassen Exterior");
                        {
                            FailureHandlingOptions optionsExterior = topoTransaction.GetFailureHandlingOptions();
                            optionsExterior.SetFailuresPreprocessor(new AxesFailure());
                            topoTransaction.SetFailureHandlingOptions(optionsExterior);

                            topoTransaction.Start();
                            SketchPlane sketchExterior = SketchPlane.Create(doc, geomPlane);
                            SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseList, bpReferencePlaneId);

                            Parameter materialParamExterior = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                            materialParamExterior.Set(exteriorMaterialId);
                        }
                        topoTransaction.Commit();
                    }
                }

                #endregion exterior
            }

            //XmlNodeList strasseInterior = xmlDoc.SelectNodes("//gml:featureMember/xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:interior//gml:posList", nsmgr);
            XmlNodeList strassenVerkehrsFlaeche = xmlDoc.SelectNodes("//xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:interior", nsmgr);

            #region reference plane
            //Dictionary<string,XYZ[]> pointList = new Dictionary<string,XYZ[]>();


            ////XYZ origin = new XYZ(0, 0, 0);
            ////XYZ normal = new XYZ(0, 0, feetToMeter);
            ////Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            //List<string> strasseBezug = new List<String>();
            //int countBezug = 0;

            //List<double> allValues = new List<double>();
            //List<double> xValues = new List<double>();
            //List<double> yValues = new List<double>();

            ////foreach (XmlNode strasseNode in exteriorGesamt)
            //foreach (XmlNode strasseNode in strasseExterior)
            //{
            //    strasseBezug.Add(strasseNode.InnerText);
            //    string[] koordWerteBezug = strasseBezug[countBezug].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //    foreach (var x in koordWerteBezug)
            //    {
            //        double values_double = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
            //        allValues.Add(values_double);
            //    }

            //    for (int ix = 0; ix < allValues.Count; ix += 2)
            //    {
            //        xValues.Add(allValues[ix]);
            //    }

            //    for (int iy = 1; iy < allValues.Count; iy += 2)
            //    {
            //        yValues.Add(allValues[iy]);
            //    }

            //    countBezug++;
            //}

            //double xMin = (xValues.Min() * feetToMeter) / R;
            //double xMax = (xValues.Max() * feetToMeter) / R;
            //double yMin = (yValues.Min() * feetToMeter) / R;
            //double yMax = (yValues.Max() * feetToMeter) / R;

            //XYZ[] pointsExterior = new XYZ[4];
            //pointsExterior[0] = transf.OfPoint(new XYZ(xMin, yMin, 0.0));
            //pointsExterior[1] = transf.OfPoint(new XYZ(xMax, yMin, 0.0));
            //pointsExterior[2] = transf.OfPoint(new XYZ(xMax, yMax, 0.0));
            //pointsExterior[3] = transf.OfPoint(new XYZ(xMin, yMax, 0.0));

            //pointList.Add("BP_StrassenVerkehrsFlaeche", pointsExterior);


            //ElementId referencePlaneId = default(ElementId);
            //foreach (var referencePoints in pointList)
            //{
            //    Transaction referencePlanes = new Transaction(doc, "Reference plane for: " + referencePoints.Key);
            //    {
            //        FailureHandlingOptions options = referencePlanes.GetFailureHandlingOptions();
            //        options.SetFailuresPreprocessor(new AxesFailure());
            //        referencePlanes.SetFailureHandlingOptions(options);

            //        referencePlanes.Start();
            //        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

            //        TopographySurface referencePlane = TopographySurface.Create(doc, referencePoints.Value);

            //        Parameter materialParam = referencePlane.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            //        materialParam.Set(transparentMaterialId);

            //        Parameter gesamt = referencePlane.LookupParameter("Kommentare");
            //        gesamt.Set("Reference plane test for: " + referencePoints.Key);
            //        referencePlaneId = referencePlane.Id;


            //    }
            //    referencePlanes.Commit();
            //}
            #endregion reference plane

            






            #region TopoGesamtExterior            

            //List<string> strasseBezug = new List<String>();
            //int countBezug = 0;

            //List<double> allValues = new List<double>();
            //List<double> xValues = new List<double>();
            //List<double> yValues = new List<double>();

            //foreach (XmlNode strasseNode in exteriorGesamt)
            //{
            //    strasseBezug.Add(strasseNode.InnerText);
            //    string[] koordWerteBezug = strasseBezug[countBezug].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //    foreach (var x in koordWerteBezug)
            //    {
            //        double values_double = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
            //        allValues.Add(values_double);
            //    }

            //    for (int ix = 0; ix < allValues.Count; ix += 2)
            //    {
            //        xValues.Add(allValues[ix]);
            //    }

            //    for (int iy = 1; iy < allValues.Count; iy += 2)
            //    {
            //        yValues.Add(allValues[iy]);
            //    }

            //    countBezug++;
            //}

            //double xMin = (xValues.Min() * feetToMeter) / R;
            //double xMax = (xValues.Max() * feetToMeter) / R;
            //double yMin = (yValues.Min() * feetToMeter) / R;
            //double yMax = (yValues.Max() * feetToMeter) / R;

            //XYZ[] pointsExterior = new XYZ[4];
            //pointsExterior[0] = transf.OfPoint(new XYZ(xMin, yMin, 0.0));
            //pointsExterior[1] = transf.OfPoint(new XYZ(xMax, yMin, 0.0));
            //pointsExterior[2] = transf.OfPoint(new XYZ(xMax, yMax, 0.0));
            //pointsExterior[3] = transf.OfPoint(new XYZ(xMin, yMax, 0.0));

            //XYZ origin = new XYZ(0, 0, 0);
            //XYZ normal = new XYZ(0, 0, feetToMeter);
            //Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            //ElementId elementId = default(ElementId);
            //Transaction tTopoGes = new Transaction(doc, "Topo Gesamt Exterior");
            //{
            //    FailureHandlingOptions options = tTopoGes.GetFailureHandlingOptions();
            //    options.SetFailuresPreprocessor(new AxesFailure());
            //    tTopoGes.SetFailureHandlingOptions(options);

            //    tTopoGes.Start();
            //    SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

            //    TopographySurface strasseGesamt = TopographySurface.Create(doc, pointsExterior);

            //    Parameter materialParam = strasseGesamt.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            //    materialParam.Set(transparentMaterialId);

            //    Parameter gesamt = strasseGesamt.LookupParameter("Kommentare");
            //    gesamt.Set("strasseGesamt");
            //    elementId = strasseGesamt.Id;
            //}
            //tTopoGes.Commit();

            #endregion TopoGesamtExterior

            #region TopoGesamtInterior            

            List<string> strasseBezugInt = new List<String>();
            int countBezugInt = 0;

            List<double> allValuesInt = new List<double>();
            List<double> xValuesInt = new List<double>();
            List<double> yValuesInt = new List<double>();

            foreach (XmlNode strasseNodeInt in interiorGesamt)
            {
                strasseBezugInt.Add(strasseNodeInt.InnerText);
                string[] koordWerteBezugInt = strasseBezugInt[countBezugInt].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var x in koordWerteBezugInt)
                {
                    double values_doubleInt = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
                    allValuesInt.Add(values_doubleInt);
                }

                for (int ix = 0; ix < allValuesInt.Count; ix += 2)
                {
                    xValuesInt.Add(allValuesInt[ix]);
                }

                for (int iy = 1; iy < allValuesInt.Count; iy += 2)
                {
                    yValuesInt.Add(allValuesInt[iy]);
                }

                countBezugInt++;
            }

            double xMinInt = (xValuesInt.Min() * feetToMeter) / R;
            double xMaxInt = (xValuesInt.Max() * feetToMeter) / R;
            double yMinInt = (yValuesInt.Min() * feetToMeter) / R;
            double yMaxInt = (yValuesInt.Max() * feetToMeter) / R;

            XYZ[] pointsNutzInt = new XYZ[4];
            pointsNutzInt[0] = transf.OfPoint(new XYZ(xMinInt, yMinInt, 0.0));
            pointsNutzInt[1] = transf.OfPoint(new XYZ(xMaxInt, yMinInt, 0.0));
            pointsNutzInt[2] = transf.OfPoint(new XYZ(xMaxInt, yMaxInt, 0.0));
            pointsNutzInt[3] = transf.OfPoint(new XYZ(xMinInt, yMaxInt, 0.0));

            //XYZ originInt = new XYZ(0, 0, 0);
            //XYZ normalInt = new XYZ(0, 0, feetToMeter);
            //Plane geomPlaneInt = Plane.CreateByNormalAndOrigin(normal, origin);

            //ElementId elementIdInt = default(ElementId);
            //Transaction tTopoGesInt = new Transaction(doc, "Topo Gesamt Interior");
            //{
            //    FailureHandlingOptions options = tTopoGes.GetFailureHandlingOptions();
            //    options.SetFailuresPreprocessor(new AxesFailure());
            //    tTopoGes.SetFailureHandlingOptions(options);

            //    tTopoGesInt.Start();
            //    SketchPlane sketch = SketchPlane.Create(doc, geomPlaneInt);


            //    TopographySurface strasseGesamtInt = TopographySurface.Create(doc, pointsNutzInt);

            //    //strasseGesamtInt.

            //    Parameter materialParam = strasseGesamtInt.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            //    materialParam.Set(transparentMaterialId);

            //    Parameter gesamtInt = strasseGesamtInt.LookupParameter("Kommentare");
            //    gesamtInt.Set("strasseGesamt");
            //    elementIdInt = strasseGesamtInt.Id;
            //}
            //tTopoGesInt.Commit();

            #endregion TopoGesamtInterior

            #region interior

            List<string> strasseInteriorListe = new List<String>();

            int ii = 0;

            foreach (XmlNode xn in strassenVerkehrsFlaeche)
            {

                List<CurveLoop> curveLoopStrasseInteriorList = new List<CurveLoop>();
                CurveLoop curveLoopStrasseInterior = new CurveLoop();

                XmlNodeList strassenVerkehrInterior = xn.SelectNodes("gml:Ring/gml:curveMember//gml:posList", nsmgr);
                //strassenVerkehrInterior = xn.SelectNodes("gml:LinearRing//gml:posList", nsmgr);
                //System.Windows.Forms.MessageBox.Show(strassenVerkehrInterior.Count.ToString());

                foreach (XmlNode xc in strassenVerkehrInterior)
                {
                    strasseInteriorListe.Add(xc.InnerText);

                    string[] koordWerteInterior = strasseInteriorListe[ii].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (koordWerteInterior.Count() == 4)
                    {
                        double xStart = Convert.ToDouble(koordWerteInterior[0], System.Globalization.CultureInfo.InvariantCulture);
                        double xStartMeter = xStart * feetToMeter;
                        double xStartMeterRedu = xStartMeter / R;
                        double yStart = Convert.ToDouble(koordWerteInterior[1], System.Globalization.CultureInfo.InvariantCulture);
                        double yStartMeter = yStart * feetToMeter;
                        double yStartMeterRedu = yStartMeter / R;
                        double zStart = 0.000;
                        double zStartMeter = zStart * feetToMeter;

                        double xEnd = Convert.ToDouble(koordWerteInterior[2], System.Globalization.CultureInfo.InvariantCulture);
                        double xEndMeter = xEnd * feetToMeter;
                        double xEndMeterRedu = xEndMeter / R;
                        double yEnd = Convert.ToDouble(koordWerteInterior[3], System.Globalization.CultureInfo.InvariantCulture);
                        double yEndMeter = yEnd * feetToMeter;
                        double yEndMeterRedu = yEndMeter / R;
                        double zEnd = 0.000;
                        double zEndMeter = zEnd * feetToMeter;

                        XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
                        XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

                        XYZ transfStartPoint = transf.OfPoint(startPoint);
                        XYZ transfEndPoint = transf.OfPoint(endPoint);

                        if (transfStartPoint.DistanceTo(transfEndPoint) > 0)
                        {
                            Line lineStrasse = Line.CreateBound(transfStartPoint, transfEndPoint);

                            curveLoopStrasseInterior.Append(lineStrasse);
                        }
                    }

                    else if (koordWerteInterior.Count() > 4)
                    {
                        int iSplitInterior = 0;

                        foreach (string split in koordWerteInterior)
                        {
                            double xStart = Convert.ToDouble(koordWerteInterior[iSplitInterior], System.Globalization.CultureInfo.InvariantCulture);
                            double xStartMeter = xStart * feetToMeter;
                            double xStartMeterRedu = xStartMeter / R;
                            double yStart = Convert.ToDouble(koordWerteInterior[iSplitInterior + 1], System.Globalization.CultureInfo.InvariantCulture);
                            double yStartMeter = yStart * feetToMeter;
                            double yStartMeterRedu = yStartMeter / R;
                            double zStart = 0.000;
                            double zStartMeter = zStart * feetToMeter;

                            double xEnd = Convert.ToDouble(koordWerteInterior[iSplitInterior + 2], System.Globalization.CultureInfo.InvariantCulture);
                            double xEndMeter = xEnd * feetToMeter;
                            double xEndMeterRedu = xEndMeter / R;
                            double yEnd = Convert.ToDouble(koordWerteInterior[iSplitInterior + 3], System.Globalization.CultureInfo.InvariantCulture);
                            double yEndMeter = yEnd * feetToMeter;
                            double yEndMeterRedu = yEndMeter / R;
                            double zEnd = 0.000;
                            double zEndMeter = zEnd * feetToMeter;

                            XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
                            XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            if (tStartPoint.DistanceTo(tEndPoint) > 0)
                            {
                                Line lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);

                                curveLoopStrasseInterior.Append(lineClIndu);
                            }

                            if ((iSplitInterior + 3) == (koordWerteInterior.Count() - 1))
                            {
                                break;
                            }

                            iSplitInterior += 2;
                        }
                    }

                    ii++;
                }

                if (curveLoopStrasseInterior.GetExactLength() > 0)
                {
                    curveLoopStrasseInteriorList.Add(curveLoopStrasseInterior);

                    if (curveLoopStrasseInteriorList.Count() > 0)
                    {
                        Transaction topoTransactionInterior = new Transaction(doc, "StrassenInterior");
                        {
                            //FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
                            //options.SetFailuresPreprocessor(new AxesFailure());
                            //topoTransaction.SetFailureHandlingOptions(options);

                            topoTransactionInterior.Start();
                            SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                            SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseInteriorList, bpReferencePlaneId);

                            Parameter materialParam = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                            materialParam.Set(interiorMaterialId);
                        }
                        topoTransactionInterior.Commit();
                    }
                }

                
            }

            #endregion interior

            #region exterior

            //List<string> list = new List<String>();

            //int i = 0;
            //foreach (XmlNode nodeExt in strasseExterior)
            //{
            //    List<CurveLoop> curveLoopStrasseList = new List<CurveLoop>();
            //    CurveLoop curveLoopStrasse = new CurveLoop();

            //    XmlNodeList strassenVerkehrExterior = nodeExt.SelectNodes("gml:LinearRing/gml:posList", nsmgr);

            //    foreach (XmlNode nodePosExt in strassenVerkehrExterior)
            //    {
            //        list.Add(nodePosExt.InnerText);

            //        string[] koordWerte = list[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //        if (koordWerte.Count() == 4)
            //        {
            //            double xStart = Convert.ToDouble(koordWerte[0], System.Globalization.CultureInfo.InvariantCulture);
            //            double xStartMeter = xStart * feetToMeter;
            //            double xStartMeterRedu = xStartMeter / R;
            //            double yStart = Convert.ToDouble(koordWerte[1], System.Globalization.CultureInfo.InvariantCulture);
            //            double yStartMeter = yStart * feetToMeter;
            //            double yStartMeterRedu = yStartMeter / R;
            //            double zStart = 0.000;
            //            double zStartMeter = zStart * feetToMeter;

            //            double xEnd = Convert.ToDouble(koordWerte[2], System.Globalization.CultureInfo.InvariantCulture);
            //            double xEndMeter = xEnd * feetToMeter;
            //            double xEndMeterRedu = xEndMeter / R;
            //            double yEnd = Convert.ToDouble(koordWerte[3], System.Globalization.CultureInfo.InvariantCulture);
            //            double yEndMeter = yEnd * feetToMeter;
            //            double yEndMeterRedu = yEndMeter / R;
            //            double zEnd = 0.000;
            //            double zEndMeter = zEnd * feetToMeter;

            //            XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
            //            XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

            //            XYZ transfStartPoint = transf.OfPoint(startPoint);
            //            XYZ transfEndPoint = transf.OfPoint(endPoint);

            //            Line lineStrasse = Line.CreateBound(transfStartPoint, transfEndPoint);

            //            curveLoopStrasse.Append(lineStrasse);
            //        }

            //        else if (koordWerte.Count() > 4)
            //        {
            //            int iSplit = 0;

            //            foreach (string split in koordWerte)
            //            {
            //                double xStart = Convert.ToDouble(koordWerte[iSplit], System.Globalization.CultureInfo.InvariantCulture);
            //                double xStartMeter = xStart * feetToMeter;
            //                double xStartMeterRedu = xStartMeter / R;
            //                double yStart = Convert.ToDouble(koordWerte[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
            //                double yStartMeter = yStart * feetToMeter;
            //                double yStartMeterRedu = yStartMeter / R;
            //                double zStart = 0.000;
            //                double zStartMeter = zStart * feetToMeter;

            //                double xEnd = Convert.ToDouble(koordWerte[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
            //                double xEndMeter = xEnd * feetToMeter;
            //                double xEndMeterRedu = xEndMeter / R;
            //                double yEnd = Convert.ToDouble(koordWerte[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
            //                double yEndMeter = yEnd * feetToMeter;
            //                double yEndMeterRedu = yEndMeter / R;
            //                double zEnd = 0.000;
            //                double zEndMeter = zEnd * feetToMeter;

            //                XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
            //                XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

            //                XYZ tStartPoint = transf.OfPoint(startPoint);
            //                XYZ tEndPoint = transf.OfPoint(endPoint);

            //                if (tStartPoint.DistanceTo(tEndPoint) > 0)
            //                {
            //                    Line lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);

            //                    curveLoopStrasse.Append(lineClIndu);
            //                }

            //                if ((iSplit + 3) == (koordWerte.Count() - 1))
            //                {
            //                    break;
            //                }

            //                iSplit += 2;
            //            }
            //        }
            //        i++;
            //    }

            //    if (curveLoopStrasse.GetExactLength() > 0)
            //    {
            //        curveLoopStrasseList.Add(curveLoopStrasse);
            //        //System.Windows.Forms.MessageBox.Show(curveLoopStrasse.Count().ToString());

            //        Transaction topoTransaction = new Transaction(doc, "Strassen Exterior");
            //        {
            //            FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
            //            options.SetFailuresPreprocessor(new AxesFailure());
            //            topoTransaction.SetFailureHandlingOptions(options);

            //            topoTransaction.Start();
            //            SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
            //            SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseList, referencePlaneId);

            //            Parameter materialParam = siteSubRegion.TopographySurface.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            //            materialParam.Set(exteriorMaterialId);
            //        }
            //        topoTransaction.Commit();
            //    }
            //}            

            #endregion exterior
        }
    }
}
