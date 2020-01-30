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

        //public class AxesFailure : IFailuresPreprocessor
        //{
        //    //Eventhandler, der eine ignorierbare Warnung, die nur auf einzelnen Geräten auftrat, überspringt.
        //    public FailureProcessingResult PreprocessFailures(
        //      FailuresAccessor a)
        //    {
        //        // inside event handler, get all warnings
        //        IList<FailureMessageAccessor> failures
        //          = a.GetFailureMessages();

        //        foreach (FailureMessageAccessor f in failures)
        //        {
        //            // check failure definition ids 
        //            // against ones to dismiss:

        //            FailureDefinitionId id
        //              = f.GetFailureDefinitionId();

        //            if (BuiltInFailures.InaccurateFailures.InaccurateSketchLine
        //              == id)
        //            {
        //                a.DeleteWarning(f);
        //            }
        //        }
        //        return FailureProcessingResult.Continue;
        //    }
        //}

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

            XmlDocument xmlDoc = new XmlDocument();
            string xPlanGmlPath = xplan_file.Text;
            xmlDoc.Load(xPlanGmlPath);

            #region namespaces
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xplan", "http://www.xplanung.de/xplangml/5/2");

            #endregion namespaces

            //XmlNodeList strasseExterior = xmlDoc.SelectNodes("//gml:featureMember/xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:exterior/gml:LinearRing/gml:posList", nsmgr);
            XmlNodeList strasseExterior = xmlDoc.SelectNodes("//gml:exterior/gml:LinearRing/gml:posList", nsmgr);


            //XmlNodeList strasseInterior = xmlDoc.SelectNodes("//gml:featureMember/xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:interior//gml:posList", nsmgr);
            XmlNodeList strassenVerkehrsFlaeche = xmlDoc.SelectNodes("//xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:interior", nsmgr);

            #region TopoGesamtNutz            

            List<string> strasseBezug = new List<String>();
            int countBezug = 0;

            List<double> allValues = new List<double>();
            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();

            foreach (XmlNode strasseNode in strasseExterior)
            {
                strasseBezug.Add(strasseNode.InnerText);
                string[] koordWerteBezug = strasseBezug[countBezug].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var x in koordWerteBezug)
                {
                    double values_double = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
                    allValues.Add(values_double);
                }

                for (int ix = 0; ix < allValues.Count; ix+=2)
                {
                    xValues.Add(allValues[ix]);
                }

                for (int iy = 1; iy < allValues.Count; iy += 2)
                {
                    yValues.Add(allValues[iy]);
                }

                countBezug++;
            }

            double xMin = (xValues.Min() * feetToMeter) / R;
            double xMax = (xValues.Max() * feetToMeter) / R;
            double yMin = (yValues.Min() * feetToMeter) / R;
            double yMax = (yValues.Max() * feetToMeter) / R;

            XYZ[] pointsNutz = new XYZ[4];
            pointsNutz[0] = transf.OfPoint(new XYZ(xMin, yMin, 0.0));
            pointsNutz[1] = transf.OfPoint(new XYZ(xMax, yMin, 0.0));
            pointsNutz[2] = transf.OfPoint(new XYZ(xMax, yMax, 0.0));
            pointsNutz[3] = transf.OfPoint(new XYZ(xMin, yMax, 0.0));

            XYZ origin = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, feetToMeter);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            ElementId elementId = default(ElementId);
            Transaction tTopoGes = new Transaction(doc, "Create Topography Gesamt Nutzung");
            {
                //FailureHandlingOptions options = tTopoGes.GetFailureHandlingOptions();
                //options.SetFailuresPreprocessor(new AxesFailure());
                //tTopoGes.SetFailureHandlingOptions(options);

                tTopoGes.Start();
                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                TopographySurface strasseGesamt = TopographySurface.Create(doc, pointsNutz);
                Parameter gesamt = strasseGesamt.LookupParameter("Kommentare");
                gesamt.Set("strasseGesamt");
                elementId = strasseGesamt.Id;
            }
            tTopoGes.Commit();

            #endregion TopoGesamtNutz

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
                }

                if (curveLoopStrasseInteriorList.Count() > 0)
                {
                    Transaction topoTransactionInterior = new Transaction(doc, "StrassenInterior");
                    {
                        //FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
                        //options.SetFailuresPreprocessor(new AxesFailure());
                        //topoTransaction.SetFailureHandlingOptions(options);

                        topoTransactionInterior.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseInteriorList, elementId);
                    }
                    topoTransactionInterior.Commit();
                }
            }

            #endregion interior

            #region exterior

            List<string> list = new List<String>();



            int i = 0;
            foreach (XmlNode node in strasseExterior)
            {
                List<CurveLoop> curveLoopStrasseList = new List<CurveLoop>();
                CurveLoop curveLoopStrasse = new CurveLoop();

                list.Add(node.InnerText);

                string[] koordWerte = list[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (koordWerte.Count() == 4)
                {
                    double xStart = Convert.ToDouble(koordWerte[0], System.Globalization.CultureInfo.InvariantCulture);
                    System.Windows.Forms.MessageBox.Show(xStart.ToString());
                    double xStartMeter = xStart * feetToMeter;
                    double xStartMeterRedu = xStartMeter / R;
                    double yStart = Convert.ToDouble(koordWerte[1], System.Globalization.CultureInfo.InvariantCulture);
                    System.Windows.Forms.MessageBox.Show(yStart.ToString());
                    double yStartMeter = yStart * feetToMeter;
                    double yStartMeterRedu = yStartMeter / R;
                    double zStart = 0.000;
                    double zStartMeter = zStart * feetToMeter;

                    double xEnd = Convert.ToDouble(koordWerte[2], System.Globalization.CultureInfo.InvariantCulture);
                    System.Windows.Forms.MessageBox.Show(xEnd.ToString());
                    double xEndMeter = xEnd * feetToMeter;
                    double xEndMeterRedu = xEndMeter / R;
                    double yEnd = Convert.ToDouble(koordWerte[3], System.Globalization.CultureInfo.InvariantCulture);
                    System.Windows.Forms.MessageBox.Show(yEnd.ToString());
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


                        if ((iSplit + 3) == (koordWerte.Count() -1 ))
                        {
                            break;
                        }

                        iSplit += 2;
                    }
                }

                if (curveLoopStrasse.GetExactLength() > 0)
                {
                    curveLoopStrasseList.Add(curveLoopStrasse);
                }


                Transaction topoTransaction = new Transaction(doc, "Strassen");
                {
                    //FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
                    //options.SetFailuresPreprocessor(new AxesFailure());
                    //topoTransaction.SetFailureHandlingOptions(options);

                    topoTransaction.Start();
                    SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                    SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, curveLoopStrasseList, elementId);
                }
                topoTransaction.Commit();

                i++;
            }

            

            #endregion exterior
        }
    }
}
