using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace NasImport
{
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public partial class NasImportForm : System.Windows.Forms.Form

    {
        ExternalCommandData commandData;
        MainData m_dataBuffer;
        double feetToMeter = 1.0 / 0.3048;
        double RE = 6380;       //mittlerer Erdradius in km

        public NasImportForm(MainData dataBuffer)
        {
            // Required for Windows Form Designer support
            InitializeComponent();
            //Get a reference of ModelLines
            m_dataBuffer = dataBuffer;
        }

        public NasImportForm(UIApplication uiApp)
        {
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

        public NasImportForm(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            String selectedFormat = String.Empty;
            DialogResult result = DialogResult.OK;
            this.DialogResult = (result != DialogResult.Cancel ? DialogResult.OK : DialogResult.None);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
        OpenFileDialog ofd = new OpenFileDialog();

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }
        OpenFileDialog ofdParam = new OpenFileDialog();

        private void button5_Click(object sender, EventArgs e)
        {
            ofd.Filter = "XML|*.xml";
            if (ofd.ShowDialog() == DialogResult.OK && ofd.FileName.Length > 0)
            {
                textBox1.Text = ofd.FileName;                
            }
        }

        private void textBoxFilesource(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            ofdParam.Filter = "TXT|*.txt";
            if (ofdParam.ShowDialog() == DialogResult.OK && ofdParam.FileName.Length > 0)
            {
                textBox3.Text = ofdParam.FileName;
            }
        }
        
        public ICollection<Element> SelectAllElements(UIDocument uidoc, Document doc)
        {
            FilteredElementCollector allTopos = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            ICollection<Element> allToposList = allTopos.ToElements();
            return allToposList;
        }       
        
        private void button1_Click(object sender, EventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            XmlDocument xmlDoc = new XmlDocument();
            string str = ofd.FileName;
            xmlDoc.Load(str);
            #region namespaces
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            #endregion namespaces
            XmlNodeList flst_list = xmlDoc.SelectNodes("//ns2:AX_Flurstueck", nsmgr);
            XmlNodeList flst_listInt = xmlDoc.SelectNodes("//ns2:AX_Flurstueck/ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:interior", nsmgr);

            #region Transformation und UTM-Reduktion
            //Zuerst wird die Position des Projektbasispunkts bestimmt
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;
            double elevation = position_data.Elevation;
            double easting = position_data.EastWest;
            double northing = position_data.NorthSouth;
            //MessageBox.Show((elevation/feetToMeter).ToString());
            //MessageBox.Show((easting/feetToMeter).ToString());
            //MessageBox.Show((northing/feetToMeter).ToString());


            // Der Ostwert des PBB wird als mittlerer Ostwert für die UTM Reduktion verwendet.
            double xSchwPktFt = easting;
            double xSchwPktKm = (double)((xSchwPktFt / feetToMeter) / 1000);
            double xSchwPkt500 = xSchwPktKm - 500;
            double R = default(double);
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                R = 1;
            }
            else
            {
                double mittlHoeheKm = (Convert.ToDouble(textBox2.Text, System.Globalization.CultureInfo.InvariantCulture)) / 1000;
                double kR = (-mittlHoeheKm / RE);
                double kAbb = ((xSchwPkt500) * (xSchwPkt500)) / (2 * RE * RE);
                R = (0.9996 * (1 + kAbb + kR));
            }

            Transform trot = Transform.CreateRotation(XYZ.BasisZ, -angle);
            XYZ vector = new XYZ(easting, northing, elevation);
            XYZ vectorRedu = vector / R;
            Transform ttrans = Transform.CreateTranslation(-vectorRedu);
            Transform transf = trot.Multiply(ttrans);
            #endregion Transformation und UTM-Reduktion         

            #region Topogesamt
            //Topogesamt wird benötigt, da die Topographien der Flurstücke über "Sitesubregion" (Unterregion) erstellt wurden. Diese benötigen eine Art Bezugsfläche, denn sie können nicht in den "leeren Raum"
            //gezeichnet werden. Eine Gesamttopographie konnte Programmtechnisch nicht für Alles genutzt werden, deswegen gibt es die Topographie seperat für Flurstücke und für Nutzungsarten.
            #region TopoGesamt Flurstücke          

            List<string> topoGesamtListFlst = new List<String>();
            List<double> xListFlst = new List<double>();
            List<double> yListFlst = new List<double>();
            int countTopoFlst = 0;

            XmlNodeList topoGesamt = xmlDoc.SelectNodes("//ns2:AX_Flurstueck/ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);
            XmlNodeList Knz_list = xmlDoc.SelectNodes("//ns2:AX_Flurstueck", nsmgr);

            foreach (XmlNode topoGesamtNode in topoGesamt)
            {
                topoGesamtListFlst.Add(topoGesamtNode.InnerText);
                string[] ssizeTopo = topoGesamtListFlst[countTopoFlst].Split(' ');

                double startXTopo = Convert.ToDouble(ssizeTopo[0], System.Globalization.CultureInfo.InvariantCulture);
                double startXmTopo = startXTopo * feetToMeter;
                double startXmTopoRedu = startXmTopo / R;
                double startYTopo = Convert.ToDouble(ssizeTopo[1], System.Globalization.CultureInfo.InvariantCulture);
                double startYmTopo = startYTopo * feetToMeter;
                double startYmTopoRedu = startYmTopo / R;
                double startZTopo = 0.000;
                double startZmTopo = startZTopo * feetToMeter;

                double endXTopo = Convert.ToDouble(ssizeTopo[2], System.Globalization.CultureInfo.InvariantCulture);
                double endXmTopo = endXTopo * feetToMeter;
                double endXmTopoRedu = endXmTopo / R;
                double endYTopo = Convert.ToDouble(ssizeTopo[3], System.Globalization.CultureInfo.InvariantCulture);
                double endYmTopo = endYTopo * feetToMeter;
                double endYmTopoRedu = endYmTopo / R;
                double endZTopo = 0.000;
                double endZmTopo = endZTopo * feetToMeter;

                xListFlst.Add(startXmTopoRedu);
                yListFlst.Add(startYmTopoRedu);
                xListFlst.Add(endXmTopoRedu);
                yListFlst.Add(endYmTopoRedu);

                countTopoFlst++;
            }

            XYZ[] pointsFlst = new XYZ[4];
            pointsFlst[0] = transf.OfPoint(new XYZ(xListFlst.Min(), yListFlst.Min(), 0.0));
            pointsFlst[1] = transf.OfPoint(new XYZ(xListFlst.Max(), yListFlst.Min(), 0.0));
            pointsFlst[2] = transf.OfPoint(new XYZ(xListFlst.Max(), yListFlst.Max(), 0.0));
            pointsFlst[3] = transf.OfPoint(new XYZ(xListFlst.Min(), yListFlst.Max(), 0.0));

            XYZ origin = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, feetToMeter);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            ElementId elementIdFlst = default(ElementId);

            Transaction topoTransaction = new Transaction(doc, "Create Topography Gesamt Flurstücke");
            {
                FailureHandlingOptions options = topoTransaction.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new AxesFailure());
                topoTransaction.SetFailureHandlingOptions(options);

                topoTransaction.Start();
                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                TopographySurface flst = TopographySurface.Create(doc, pointsFlst);
                Parameter gesamt = flst.LookupParameter("Kommentare");
                gesamt.Set("TopoGesamt");
                elementIdFlst = flst.Id;
            }
            topoTransaction.Commit();
            this.Refresh();

            #endregion TopoGesamt Flurstücke
            #region TopoGesamtNutz            

            List<string> topoGesamtList = new List<String>();
            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            int countTopo = 0;

            foreach (XmlNode topoGesamtNode in topoGesamt)
            {
                topoGesamtList.Add(topoGesamtNode.InnerText);
                string[] ssizeTopo = topoGesamtList[countTopo].Split(' ');

                double startXTopo = Convert.ToDouble(ssizeTopo[0], System.Globalization.CultureInfo.InvariantCulture);
                double startXmTopo = startXTopo * feetToMeter;
                double startXmTopoRedu = startXmTopo / R;
                double startYTopo = Convert.ToDouble(ssizeTopo[1], System.Globalization.CultureInfo.InvariantCulture);
                double startYmTopo = startYTopo * feetToMeter;
                double startYmTopoRedu = startYmTopo / R;
                double startZTopo = 0.000;
                double startZmTopo = startZTopo * feetToMeter;

                double endXTopo = Convert.ToDouble(ssizeTopo[2], System.Globalization.CultureInfo.InvariantCulture);
                double endXmTopo = endXTopo * feetToMeter;
                double endXmTopoRedu = endXmTopo / R;
                double endYTopo = Convert.ToDouble(ssizeTopo[3], System.Globalization.CultureInfo.InvariantCulture);
                double endYmTopo = endYTopo * feetToMeter;
                double endYmTopoRedu = endYmTopo / R;
                double endZTopo = 0.000;
                double endZmTopo = endZTopo * feetToMeter;

                xList.Add(startXmTopoRedu);
                yList.Add(startYmTopoRedu);
                xList.Add(endXmTopoRedu);
                yList.Add(endYmTopoRedu);

                countTopo++;
            }

            XYZ[] pointsNutz = new XYZ[4];
            pointsNutz[0] = transf.OfPoint(new XYZ(xList.Min(), yList.Min(), 0.0));
            pointsNutz[1] = transf.OfPoint(new XYZ(xList.Max(), yList.Min(), 0.0));
            pointsNutz[2] = transf.OfPoint(new XYZ(xList.Max(), yList.Max(), 0.0));
            pointsNutz[3] = transf.OfPoint(new XYZ(xList.Min(), yList.Max(), 0.0));

            ElementId elementId = default(ElementId);
            Transaction tTopoGes = new Transaction(doc, "Create Topography Gesamt Nutzung");
            {
                FailureHandlingOptions options = tTopoGes.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new AxesFailure());
                tTopoGes.SetFailureHandlingOptions(options);

                tTopoGes.Start();
                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                TopographySurface flst = TopographySurface.Create(doc, pointsNutz);
                Parameter gesamt = flst.LookupParameter("Kommentare");
                gesamt.Set("TopoGesamt");
                elementId = flst.Id;
            }
            tTopoGes.Commit();
            this.Refresh();

            #endregion TopoGesamtNutz
            #endregion Topogesamt
            #region Nutzungsarten
            //Im Folgenden werden die Nutzungsarten eingelesen. Es wurde sich auf die in der ALKI Datei vorkommenden Nutzungsarten beschränkt, d.h. Nutzungsarten die im Datenbestand nicht vorkamen wurden
            //weggelassen. Das Programm müsste daher, um universal einsetzbar zu sein, noch um weitere Nutzungsarten wie "Bergbaubetrieb" oder "Halde" analog zu den realisierten Nutzungsarten erweitert werden. 
            #region Industrieflächen 
            XmlNodeList indu_list = xmlDoc.SelectNodes("//ns2:AX_IndustrieUndGewerbeflaeche", nsmgr);

            foreach (XmlNode induNode in indu_list)
            {
                XmlNodeList posIndu = induNode.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);
                List<string> listIndu = new List<String>();
                List<string> lineSplit = new List<string>();

                int countIndu = 0;

                List<CurveLoop> cLoopListIndu = new List<CurveLoop>();
                CurveLoop cLoopLineIndu = new CurveLoop();

                foreach (XmlNode nodeExt in posIndu)
                {
                    listIndu.Add(nodeExt.InnerText);

                    string[] ssizeIndu = listIndu[countIndu].Split(' ');

                    if (ssizeIndu.Count() == 4)
                    {
                        double startX = Convert.ToDouble(ssizeIndu[0], System.Globalization.CultureInfo.InvariantCulture);
                        double startXm = startX * feetToMeter;
                        double startXmRedu = startXm / R;
                        double startY = Convert.ToDouble(ssizeIndu[1], System.Globalization.CultureInfo.InvariantCulture);
                        double startYm = startY * feetToMeter;
                        double startYmRedu = startYm / R;
                        double startZ = 0.000;
                        double startZm = startZ * feetToMeter;

                        double endX = Convert.ToDouble(ssizeIndu[2], System.Globalization.CultureInfo.InvariantCulture);
                        double endXm = endX * feetToMeter;
                        double endXmRedu = endXm / R;
                        double endY = Convert.ToDouble(ssizeIndu[3], System.Globalization.CultureInfo.InvariantCulture);
                        double endYm = endY * feetToMeter;
                        double endYmRedu = endYm / R;
                        double endZ = 0.000;
                        double endZm = endZ * feetToMeter;

                        XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                        XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                        XYZ tStartPoint = transf.OfPoint(startPoint);
                        XYZ tEndPoint = transf.OfPoint(endPoint);

                        Line lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);

                        cLoopLineIndu.Append(lineClIndu);
                    }

                    else if (ssizeIndu.Count() > 4)
                    {
                        int iSplit = 0;

                        foreach (string split in ssizeIndu)
                        {
                            double startX = Convert.ToDouble(ssizeIndu[iSplit], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeIndu[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeIndu[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeIndu[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClIndu = Line.CreateBound(tStartPoint, tEndPoint);

                            cLoopLineIndu.Append(lineClIndu);

                            if ((iSplit + 3) == (ssizeIndu.Count() - 1))
                            {
                                break;
                            }

                            iSplit += 2;
                        }
                    }
                    countIndu++;
                }

                if (cLoopLineIndu.GetExactLength() > 0)
                {
                    cLoopListIndu.Add(cLoopLineIndu);
                }

                if (cLoopListIndu.Count() > 0)
                {
                    Transaction transIndu = new Transaction(doc, "Create Industry Areas");
                    {
                        FailureHandlingOptions options = transIndu.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new AxesFailure());
                        transIndu.SetFailureHandlingOptions(options);

                        transIndu.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListIndu, elementId);

                    }
                    transIndu.Commit();
                    this.Refresh();

                    ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                    foreach (Element el in eleColle)
                    {
                        #region Parameter
                        Parameter parameter0 = el.LookupParameter("Kommentare");
                        Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                        Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                        Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                        Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                        Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                        Parameter parameter6 = el.LookupParameter("Flurnummer");
                        Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                        Parameter parameter8 = el.LookupParameter("Land");
                        Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                        Parameter parameter10 = el.LookupParameter("Kreis");
                        Parameter parameter11 = el.LookupParameter("Gemeinde");
                        Parameter parameter12 = el.LookupParameter("Dienststelle");
                        Parameter parameter13 = el.LookupParameter("Vorname");
                        Parameter parameter14 = el.LookupParameter("Nachname");

                        #endregion parameter

                        using (Transaction t = new Transaction(doc, "parameter"))
                        {
                            t.Start("Parameterwerte hinzufügen");
                            try
                            {
                                if (parameter0.HasValue.Equals(false))
                                {
                                    parameter0.Set("Industrieflaeche");
                                }
                            }
                            catch { }
                            t.Commit();
                            this.Refresh();

                        }
                    }
                }
            }
            #endregion Industrieflächen 
            #region Wohnbauflächen 
            XmlNodeList wohnList = xmlDoc.SelectNodes("//ns2:AX_Wohnbauflaeche", nsmgr);
            foreach (XmlNode wohnNode in wohnList)
            {
                XmlNodeList posWohn = wohnNode.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);
                List<string> listWohn = new List<String>();
                List<string> lineSplit = new List<string>();

                int countWohn = 0;

                List<CurveLoop> cLoopListWohn = new List<CurveLoop>();
                CurveLoop cLoopLineWohn = new CurveLoop();

                foreach (XmlNode nodeWohn in posWohn)
                {
                    listWohn.Add(nodeWohn.InnerText);

                    string[] ssizeWohn = listWohn[countWohn].Split(' ');

                    if (ssizeWohn.Count() == 4)
                    {
                        double startX = Convert.ToDouble(ssizeWohn[0], System.Globalization.CultureInfo.InvariantCulture);
                        double startXm = startX * feetToMeter;
                        double startXmRedu = startXm / R;
                        double startY = Convert.ToDouble(ssizeWohn[1], System.Globalization.CultureInfo.InvariantCulture);
                        double startYm = startY * feetToMeter;
                        double startYmRedu = startYm / R;
                        double startZ = 0.000;
                        double startZm = startZ * feetToMeter;

                        double endX = Convert.ToDouble(ssizeWohn[2], System.Globalization.CultureInfo.InvariantCulture);
                        double endXm = endX * feetToMeter;
                        double endXmRedu = endXm / R;
                        double endY = Convert.ToDouble(ssizeWohn[3], System.Globalization.CultureInfo.InvariantCulture);
                        double endYm = endY * feetToMeter;
                        double endYmRedu = endYm / R;
                        double endZ = 0.000;
                        double endZm = endZ * feetToMeter;

                        XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                        XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                        XYZ tStartPoint = transf.OfPoint(startPoint);
                        XYZ tEndPoint = transf.OfPoint(endPoint);

                        Line lineClWohn = Line.CreateBound(tStartPoint, tEndPoint);
                        cLoopLineWohn.Append(lineClWohn);
                    }

                    else if (ssizeWohn.Count() > 4)
                    {
                        int iSplit = 0;

                        foreach (string split in ssizeWohn)
                        {
                            double startX = Convert.ToDouble(ssizeWohn[iSplit], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeWohn[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeWohn[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeWohn[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClWohn = Line.CreateBound(tStartPoint, tEndPoint);

                            cLoopLineWohn.Append(lineClWohn);

                            if ((iSplit + 3) == (ssizeWohn.Count() - 1))
                            {
                                break;
                            }

                            iSplit += 2;
                        }
                    }
                    countWohn++;
                }

                if (cLoopLineWohn.GetExactLength() > 0)
                {
                    cLoopListWohn.Add(cLoopLineWohn);
                }

                if (cLoopListWohn.Count() > 0)
                {
                    Transaction transWohn = new Transaction(doc, "Create Industry Areas");
                    {

                        transWohn.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListWohn, elementId);

                        FailureHandlingOptions options = transWohn.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new AxesFailure());
                        transWohn.SetFailureHandlingOptions(options);

                    }
                    transWohn.Commit();
                    this.Refresh();

                    ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                    foreach (Element el in eleColle)
                    {
                        #region Parameter
                        Parameter parameter0 = el.LookupParameter("Kommentare");
                        Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                        Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                        Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                        Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                        Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                        Parameter parameter6 = el.LookupParameter("Flurnummer");
                        Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                        Parameter parameter8 = el.LookupParameter("Land");
                        Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                        Parameter parameter10 = el.LookupParameter("Kreis");
                        Parameter parameter11 = el.LookupParameter("Gemeinde");
                        Parameter parameter12 = el.LookupParameter("Dienststelle");
                        Parameter parameter13 = el.LookupParameter("Vorname");
                        Parameter parameter14 = el.LookupParameter("Nachname");

                        #endregion parameter

                        using (Transaction t = new Transaction(doc, "parameter"))
                        {
                            t.Start("Parameterwerte hinzufügen");
                            try
                            {
                                if (parameter0.HasValue.Equals(false))
                                {
                                    parameter0.Set("Wohnbauflaeche");
                                }
                            }
                            catch { }
                            t.Commit();
                            this.Refresh();
                        }
                    }
                }
            }
            #endregion Wohnbauflächen

            #region SportFreizeit
            XmlNodeList sportList = xmlDoc.SelectNodes("//ns2:AX_SportFreizeitUndErholungsflaeche", nsmgr);

            foreach (XmlNode sportNode in sportList)
            {
                XmlNodeList posSport = sportNode.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);
                List<string> listSport = new List<String>();
                List<string> lineSplit = new List<string>();

                int countSport = 0;

                List<CurveLoop> cLoopListSport = new List<CurveLoop>();
                CurveLoop cLoopLineSport = new CurveLoop();

                foreach (XmlNode nodeSport in posSport)
                {
                    listSport.Add(nodeSport.InnerText);

                    string[] ssizeSport = listSport[countSport].Split(' ');

                    if (ssizeSport.Count() == 4)
                    {
                        double startX = Convert.ToDouble(ssizeSport[0], System.Globalization.CultureInfo.InvariantCulture);
                        double startXm = startX * feetToMeter;
                        double startXmRedu = startXm / R;
                        double startY = Convert.ToDouble(ssizeSport[1], System.Globalization.CultureInfo.InvariantCulture);
                        double startYm = startY * feetToMeter;
                        double startYmRedu = startYm / R;
                        double startZ = 0.000;
                        double startZm = startZ * feetToMeter;

                        double endX = Convert.ToDouble(ssizeSport[2], System.Globalization.CultureInfo.InvariantCulture);
                        double endXm = endX * feetToMeter;
                        double endXmRedu = endXm / R;
                        double endY = Convert.ToDouble(ssizeSport[3], System.Globalization.CultureInfo.InvariantCulture);
                        double endYm = endY * feetToMeter;
                        double endYmRedu = endYm / R;
                        double endZ = 0.000;
                        double endZm = endZ * feetToMeter;

                        XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                        XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                        XYZ tStartPoint = transf.OfPoint(startPoint);
                        XYZ tEndPoint = transf.OfPoint(endPoint);

                        Line lineClSport = Line.CreateBound(tStartPoint, tEndPoint);
                        cLoopLineSport.Append(lineClSport);
                    }

                    else if (ssizeSport.Count() > 4)
                    {
                        int iSplit = 0;

                        foreach (string split in ssizeSport)
                        {
                            double startX = Convert.ToDouble(ssizeSport[iSplit], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeSport[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeSport[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeSport[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClSport = Line.CreateBound(tStartPoint, tEndPoint);

                            cLoopLineSport.Append(lineClSport);

                            if ((iSplit + 3) == (ssizeSport.Count() - 1))
                            {
                                break;
                            }

                            iSplit += 2;
                        }
                    }
                    countSport++;
                }

                if (cLoopLineSport.GetExactLength() > 0)
                {
                    cLoopListSport.Add(cLoopLineSport);
                }

                if (cLoopListSport.Count() > 0)
                {
                    Transaction transSport = new Transaction(doc, "Create Sport Areas");
                    {
                        transSport.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListSport, elementId);

                        FailureHandlingOptions options = transSport.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new AxesFailure());
                        transSport.SetFailureHandlingOptions(options);

                    }
                    transSport.Commit();
                    this.Refresh();

                    ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                    foreach (Element el in eleColle)
                    {
                        #region Parameter
                        Parameter parameter0 = el.LookupParameter("Kommentare");
                        Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                        Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                        Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                        Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                        Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                        Parameter parameter6 = el.LookupParameter("Flurnummer");
                        Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                        Parameter parameter8 = el.LookupParameter("Land");
                        Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                        Parameter parameter10 = el.LookupParameter("Kreis");
                        Parameter parameter11 = el.LookupParameter("Gemeinde");
                        Parameter parameter12 = el.LookupParameter("Dienststelle");
                        Parameter parameter13 = el.LookupParameter("Vorname");
                        Parameter parameter14 = el.LookupParameter("Nachname");

                        #endregion parameter

                        using (Transaction t = new Transaction(doc, "parameter"))
                        {
                            t.Start("Parameterwerte hinzufügen");
                            try
                            {
                                if (parameter0.HasValue.Equals(false))
                                {
                                    parameter0.Set("SportFreizeitUndErholungsflaeche");
                                }
                            }
                            catch { }
                            t.Commit();
                            this.Refresh();

                        }
                    }
                }
            }
            #endregion Sportfreizeit

            #region Strassenverkehr
            XmlNodeList straList = xmlDoc.SelectNodes("//ns2:AX_Strassenverkehr", nsmgr);

            foreach (XmlNode straNode in straList)
            {
                XmlNodeList posStra = straNode.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);
                List<string> listStra = new List<String>();
                List<string> lineSplit = new List<string>();

                int countStra = 0;

                List<CurveLoop> cLoopListStra = new List<CurveLoop>();
                CurveLoop cLoopLineStra = new CurveLoop();

                foreach (XmlNode nodeStra in posStra)
                {
                    listStra.Add(nodeStra.InnerText);

                    string[] ssizeStra = listStra[countStra].Split(' ');

                    if (ssizeStra.Count() == 4)
                    {
                        double startX = Convert.ToDouble(ssizeStra[0], System.Globalization.CultureInfo.InvariantCulture);
                        double startXm = startX * feetToMeter;
                        double startXmRedu = startXm / R;
                        double startY = Convert.ToDouble(ssizeStra[1], System.Globalization.CultureInfo.InvariantCulture);
                        double startYm = startY * feetToMeter;
                        double startYmRedu = startYm / R;
                        double startZ = 0.000;
                        double startZm = startZ * feetToMeter;

                        double endX = Convert.ToDouble(ssizeStra[2], System.Globalization.CultureInfo.InvariantCulture);
                        double endXm = endX * feetToMeter;
                        double endXmRedu = endXm / R;
                        double endY = Convert.ToDouble(ssizeStra[3], System.Globalization.CultureInfo.InvariantCulture);
                        double endYm = endY * feetToMeter;
                        double endYmRedu = endYm / R;
                        double endZ = 0.000;
                        double endZm = endZ * feetToMeter;

                        XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                        XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                        XYZ tStartPoint = transf.OfPoint(startPoint);
                        XYZ tEndPoint = transf.OfPoint(endPoint);

                        Line lineClStra = Line.CreateBound(tStartPoint, tEndPoint);
                        cLoopLineStra.Append(lineClStra);
                    }

                    else if (ssizeStra.Count() > 4)
                    {
                        int iSplit = 0;

                        foreach (string split in ssizeStra)
                        {
                            double startX = Convert.ToDouble(ssizeStra[iSplit], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeStra[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeStra[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeStra[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClStra = Line.CreateBound(tStartPoint, tEndPoint);

                            cLoopLineStra.Append(lineClStra);

                            if ((iSplit + 3) == (ssizeStra.Count() - 1))
                            {
                                break;
                            }

                            iSplit += 2;
                        }
                    }
                    countStra++;
                }

                if (cLoopLineStra.GetExactLength() > 0)
                {
                    cLoopListStra.Add(cLoopLineStra);
                }

                if (cLoopListStra.Count() > 0)
                {
                    Transaction transStrass = new Transaction(doc, "Create Strassenverkehr Areas");
                    {
                        transStrass.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListStra, elementId);

                        FailureHandlingOptions options = transStrass.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new AxesFailure());
                        transStrass.SetFailureHandlingOptions(options);

                    }
                    transStrass.Commit();
                    this.Refresh();

                    ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                    foreach (Element el in eleColle)
                    {
                        #region Parameter
                        Parameter parameter0 = el.LookupParameter("Kommentare");
                        Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                        Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                        Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                        Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                        Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                        Parameter parameter6 = el.LookupParameter("Flurnummer");
                        Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                        Parameter parameter8 = el.LookupParameter("Land");
                        Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                        Parameter parameter10 = el.LookupParameter("Kreis");
                        Parameter parameter11 = el.LookupParameter("Gemeinde");
                        Parameter parameter12 = el.LookupParameter("Dienststelle");
                        Parameter parameter13 = el.LookupParameter("Vorname");
                        Parameter parameter14 = el.LookupParameter("Nachname");
                        #endregion parameter

                        using (Transaction t = new Transaction(doc, "parameter"))
                        {
                            t.Start("Parameterwerte hinzufügen");
                            try
                            {
                                if (parameter0.HasValue.Equals(false))
                                {
                                    parameter0.Set("Strassenverkehr");
                                }
                            }
                            catch { }
                            t.Commit();
                            this.Refresh();

                        }
                    }
                }
            }
            #endregion Strassenverkehr

            #region FlächeGemischterNutzung
            XmlNodeList gemList = xmlDoc.SelectNodes("//ns2:AX_FlaecheGemischterNutzung", nsmgr);

            foreach (XmlNode gemNode in gemList)
            {
                XmlNodeList posGem = gemNode.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);
                List<string> listGem = new List<String>();
                List<string> lineSplit = new List<string>();

                int countGem = 0;

                List<CurveLoop> cLoopListGem = new List<CurveLoop>();
                CurveLoop cLoopLineGem = new CurveLoop();

                foreach (XmlNode nodeGem in posGem)
                {
                    listGem.Add(nodeGem.InnerText);

                    string[] ssizeGem = listGem[countGem].Split(' ');

                    if (ssizeGem.Count() == 4)
                    {
                        double startX = Convert.ToDouble(ssizeGem[0], System.Globalization.CultureInfo.InvariantCulture);
                        double startXm = startX * feetToMeter;
                        double startXmRedu = startXm / R;
                        double startY = Convert.ToDouble(ssizeGem[1], System.Globalization.CultureInfo.InvariantCulture);
                        double startYm = startY * feetToMeter;
                        double startYmRedu = startYm / R;
                        double startZ = 0.000;
                        double startZm = startZ * feetToMeter;

                        double endX = Convert.ToDouble(ssizeGem[2], System.Globalization.CultureInfo.InvariantCulture);
                        double endXm = endX * feetToMeter;
                        double endXmRedu = endXm / R;
                        double endY = Convert.ToDouble(ssizeGem[3], System.Globalization.CultureInfo.InvariantCulture);
                        double endYm = endY * feetToMeter;
                        double endYmRedu = endYm / R;
                        double endZ = 0.000;
                        double endZm = endZ * feetToMeter;

                        XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                        XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                        XYZ tStartPoint = transf.OfPoint(startPoint);
                        XYZ tEndPoint = transf.OfPoint(endPoint);

                        Line lineClGem = Line.CreateBound(tStartPoint, tEndPoint);

                        cLoopLineGem.Append(lineClGem);
                    }

                    else if (ssizeGem.Count() > 4)
                    {
                        int iSplit = 0;

                        foreach (string split in ssizeGem)
                        {
                            double startX = Convert.ToDouble(ssizeGem[iSplit], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeGem[iSplit + 1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeGem[iSplit + 2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeGem[iSplit + 3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            XYZ startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            XYZ endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClGem = Line.CreateBound(tStartPoint, tEndPoint);

                            cLoopLineGem.Append(lineClGem);

                            if ((iSplit + 3) == (ssizeGem.Count() - 1))
                            {
                                break;
                            }

                            iSplit += 2;
                        }
                    }
                    countGem++;
                }

                if (cLoopLineGem.GetExactLength() > 0)
                {
                    cLoopListGem.Add(cLoopLineGem);
                }

                if (cLoopListGem.Count() > 0)
                {
                    Transaction transGem = new Transaction(doc, "Create Flächen gemischter Nutzung Areas");
                    {
                        transGem.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListGem, elementId);

                        FailureHandlingOptions options = transGem.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new AxesFailure());
                        transGem.SetFailureHandlingOptions(options);

                    }
                    transGem.Commit();
                    this.Refresh();

                    ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                    foreach (Element el in eleColle)
                    {
                        #region Parameter
                        Parameter parameter0 = el.LookupParameter("Kommentare");
                        Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                        Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                        Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                        Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                        Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                        Parameter parameter6 = el.LookupParameter("Flurnummer");
                        Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                        Parameter parameter8 = el.LookupParameter("Land");
                        Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                        Parameter parameter10 = el.LookupParameter("Kreis");
                        Parameter parameter11 = el.LookupParameter("Gemeinde");
                        Parameter parameter12 = el.LookupParameter("Dienststelle");
                        Parameter parameter13 = el.LookupParameter("Vorname");
                        Parameter parameter14 = el.LookupParameter("Nachname");
                        #endregion parameter

                        using (Transaction t = new Transaction(doc, "parameter"))
                        {
                            t.Start("Parameterwerte hinzufügen");
                            try
                            {
                                if (parameter0.HasValue.Equals(false))
                                {
                                    parameter0.Set("FlaecheGemischterNutzung");
                                }
                            }
                            catch { }
                            t.Commit();
                            this.Refresh();
                        }
                    }
                }
            }
            #endregion FlächeGemischterNutzung
            #endregion Nutzungsarten
            #region sitesubregion exterior

            Category category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography);
            CategorySet categorySet = app.Create.NewCategorySet();
            categorySet.Insert(category);

            string spFile = ofdParam.FileName;
            DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();

            //die Transaction tCreateSpFile erstellt die für den Parameterimport nach Revit erforderliche "Shared Parameter Datei". Dabei ist eine bestehende Datei notwendig 
            //(erstellt über Revit, Gruppe Verwalten, Gemeinsam genutzte Parameter). Die Datei kann entweder leer sein (heißt: nur der durch Revit erstellte "Tabellenkopf"), oder ebenso gefüllt.
            //In diesem Fall ergänzt die Transaktion die Datei ggf. um die fehlenden Parameter
            Transaction tCreateSpFile = new Transaction(doc, "Create Shared Parameter File");
            {
                tCreateSpFile.Start();

                try
                {
                    app.SharedParametersFilename = spFile;
                }
                catch (Exception)
                {
                    MessageBox.Show("No Shared Parameter File found");
                }

                DefinitionGroup dgSp = sharedParameterFile.Groups.get_Item("Flurstuecksdaten");
                
                ExternalDefinitionCreationOptions gemeindeOpt = new ExternalDefinitionCreationOptions("Gemeinde", ParameterType.Text);
                ExternalDefinitionCreationOptions gemarkNummerOpt = new ExternalDefinitionCreationOptions("Gemarkungsnummer", ParameterType.Text);
                ExternalDefinitionCreationOptions gemarkSchlüsselOpt = new ExternalDefinitionCreationOptions("Gemarkungsschlüssel", ParameterType.Text);
                ExternalDefinitionCreationOptions flstNummerOpt = new ExternalDefinitionCreationOptions("Flurstücksnummer", ParameterType.Text);
                ExternalDefinitionCreationOptions kreisOpt = new ExternalDefinitionCreationOptions("Kreis", ParameterType.Text);
                ExternalDefinitionCreationOptions landOpt = new ExternalDefinitionCreationOptions("Land", ParameterType.Text);
                ExternalDefinitionCreationOptions zeitpktDerEntstehOpt = new ExternalDefinitionCreationOptions("Zeitpunkt der Entstehung", ParameterType.Text);
                ExternalDefinitionCreationOptions dienstStelleOpt = new ExternalDefinitionCreationOptions("Dienststelle", ParameterType.Text);
                ExternalDefinitionCreationOptions flurNummerOpt = new ExternalDefinitionCreationOptions("Flurnummer", ParameterType.Text);
                ExternalDefinitionCreationOptions flurstücksKennzOpt = new ExternalDefinitionCreationOptions("Flurstückskennzeichen", ParameterType.Text);
                ExternalDefinitionCreationOptions regierBezirkOpt = new ExternalDefinitionCreationOptions("Regierungsbezirk", ParameterType.Text);
                ExternalDefinitionCreationOptions amtFlaecheOpt = new ExternalDefinitionCreationOptions("Amtliche Fläche", ParameterType.Text);
                ExternalDefinitionCreationOptions nachnameOderFirmaOpt = new ExternalDefinitionCreationOptions("Nachname oder Firma", ParameterType.Text);
                ExternalDefinitionCreationOptions vornameOpt = new ExternalDefinitionCreationOptions("Vorname", ParameterType.Text);
                ExternalDefinitionCreationOptions ortOpt = new ExternalDefinitionCreationOptions("Ort", ParameterType.Text);
                ExternalDefinitionCreationOptions plzOpt = new ExternalDefinitionCreationOptions("PLZ", ParameterType.Text);
                ExternalDefinitionCreationOptions strasseOpt = new ExternalDefinitionCreationOptions("Strasse", ParameterType.Text);
                ExternalDefinitionCreationOptions hausnummerOpt = new ExternalDefinitionCreationOptions("Hausnummer", ParameterType.Text);

                Definition gemeindeDefinition = default(Definition);
                Definition gemarkNummerDefinition = default(Definition);
                Definition gemarkSchlüsselDefinition = default(Definition);
                Definition flstNummerDefinition = default(Definition);
                Definition kreisDefinition = default(Definition);
                Definition landDefinition = default(Definition);
                Definition zeitpktDerEntstehDefinition = default(Definition);
                Definition dienstStelleDefinition = default(Definition);
                Definition flurNummerDefinition = default(Definition);
                Definition flurstücksKennzDefinition = default(Definition);
                Definition regierBezirDefinition = default(Definition);
                Definition amtFlaecheDefinition = default(Definition);
                Definition nachnameOderFirmaDefinition = default(Definition);
                Definition vornameDefinition = default(Definition);
                Definition ortDefinition = default(Definition);
                Definition plzDefinition = default(Definition);
                Definition strasseDefinition = default(Definition);
                Definition hausnummerDefinition = default(Definition);

                if (dgSp == null )
                {
                    dgSp = sharedParameterFile.Groups.Create("Flurstuecksdaten");
                    gemeindeDefinition = dgSp.Definitions.Create(gemeindeOpt);
                    gemarkNummerDefinition = dgSp.Definitions.Create(gemarkNummerOpt);
                    gemarkSchlüsselDefinition = dgSp.Definitions.Create(gemarkSchlüsselOpt);
                    flstNummerDefinition = dgSp.Definitions.Create(flstNummerOpt);
                    kreisDefinition = dgSp.Definitions.Create(kreisOpt);
                    landDefinition = dgSp.Definitions.Create(landOpt);
                    zeitpktDerEntstehDefinition = dgSp.Definitions.Create(zeitpktDerEntstehOpt);
                    dienstStelleDefinition = dgSp.Definitions.Create(dienstStelleOpt);
                    flurNummerDefinition = dgSp.Definitions.Create(flurNummerOpt);
                    flurstücksKennzDefinition = dgSp.Definitions.Create(flurstücksKennzOpt);
                    regierBezirDefinition = dgSp.Definitions.Create(regierBezirkOpt);
                    amtFlaecheDefinition = dgSp.Definitions.Create(amtFlaecheOpt);
                    nachnameOderFirmaDefinition = dgSp.Definitions.Create(nachnameOderFirmaOpt);
                    vornameDefinition = dgSp.Definitions.Create(vornameOpt);
                    ortDefinition = dgSp.Definitions.Create(ortOpt);
                    plzDefinition = dgSp.Definitions.Create(plzOpt);
                    strasseDefinition = dgSp.Definitions.Create(strasseOpt);
                    hausnummerDefinition = dgSp.Definitions.Create(hausnummerOpt);
                }

                else if (dgSp != null)
                {
                    if (gemeindeDefinition != null)
                    {

                    }
                    else if (gemeindeDefinition == null)
                    {
                        gemeindeDefinition = dgSp.Definitions.get_Item("Gemeinde");
                    }
                    if (gemarkNummerDefinition != null)
                    {

                    }
                    else if (gemarkNummerDefinition == null)
                    {
                        gemarkNummerDefinition = dgSp.Definitions.get_Item("Gemarkungsnummer");
                    }
                    if (gemarkSchlüsselDefinition != null)
                    {

                    }
                    else if (gemarkSchlüsselDefinition == null)
                    {
                        gemarkSchlüsselDefinition = dgSp.Definitions.get_Item("Gemarkungsschlüssel");
                    }
                    if (flstNummerDefinition != null)
                    {

                    }
                    else if (flstNummerDefinition == null)
                    {
                        flstNummerDefinition = dgSp.Definitions.get_Item("Flurstücksnummer");
                    }
                    if (kreisDefinition != null)
                    {

                    }
                    else if (kreisDefinition == null)
                    {
                        kreisDefinition = dgSp.Definitions.get_Item("Kreis");
                    }
                    if (landDefinition != null)
                    {

                    }
                    else if (landDefinition == null)
                    {
                        landDefinition = dgSp.Definitions.get_Item("Land");
                    }
                    if (zeitpktDerEntstehDefinition != null)
                    {

                    }
                    else if (zeitpktDerEntstehDefinition == null)
                    {
                        zeitpktDerEntstehDefinition = dgSp.Definitions.get_Item("Zeitpunkt der Entstehung");
                    }
                    if (dienstStelleDefinition != null)
                    {

                    }
                    else if (dienstStelleDefinition == null)
                    {
                        dienstStelleDefinition = dgSp.Definitions.get_Item("Dienststelle");
                    }
                    if (flurNummerDefinition != null)
                    {

                    }
                    else if (flurNummerDefinition == null)
                    {
                        flurNummerDefinition = dgSp.Definitions.get_Item("Flurnummer");
                    }
                    if (flurstücksKennzDefinition != null)
                    {

                    }
                    else if (flurstücksKennzDefinition == null)
                    {
                        flurstücksKennzDefinition = dgSp.Definitions.get_Item("Flurstückskennzeichen");
                    }
                    if (regierBezirDefinition != null)
                    {

                    }
                    else if (regierBezirDefinition == null)
                    {
                        regierBezirDefinition = dgSp.Definitions.get_Item("Regierungsbezirk");
                    }
                    if (amtFlaecheDefinition != null)
                    {

                    }
                    else if (amtFlaecheDefinition == null)
                    {
                        amtFlaecheDefinition = dgSp.Definitions.get_Item("Amtliche Fläche");
                    }
                    if (nachnameOderFirmaDefinition != null)
                    {

                    }
                    else if (nachnameOderFirmaDefinition == null)
                    {
                        nachnameOderFirmaDefinition = dgSp.Definitions.get_Item("Nachname oder Firma");
                    }
                    if (vornameDefinition != null)
                    {

                    }
                    else if (vornameDefinition == null)
                    {
                        vornameDefinition = dgSp.Definitions.get_Item("Vorname");
                    }
                    if (ortDefinition != null)
                    {

                    }
                    else if (ortDefinition == null)
                    {
                        ortDefinition = dgSp.Definitions.get_Item("Ort");
                    }
                    if (plzDefinition != null)
                    {

                    }
                    else if (plzDefinition == null)
                    {
                        plzDefinition = dgSp.Definitions.get_Item("PLZ");
                    }
                    if (strasseDefinition != null)
                    {

                    }
                    else if (strasseDefinition == null)
                    {
                        strasseDefinition = dgSp.Definitions.get_Item("Strasse");
                    }
                    if (hausnummerDefinition != null)
                    {

                    }
                    else if (hausnummerDefinition == null)
                    {
                        hausnummerDefinition = dgSp.Definitions.get_Item("Hausnummer");
                    }
                }
            }
            tCreateSpFile.Commit();
            this.Refresh();            

            //Für jede Gruppe im SP File (hier: eine Gruppe "Flurstuecksdaten") werden die Parameter ausgelesen und an die Topographien angebracht. 
            foreach (DefinitionGroup dg in sharedParameterFile.Groups)
            {
                if (dg.Name == "Flurstuecksdaten")
                {
                    ExternalDefinition gemarkSchlüsselExtDef = dg.Definitions.get_Item("Gemarkungsschlüssel") as ExternalDefinition;          
                    ExternalDefinition gemarkNummerExtDef = dg.Definitions.get_Item("Gemarkungsnummer") as ExternalDefinition;
                    ExternalDefinition flstNummerExtDef = dg.Definitions.get_Item("Flurstücksnummer") as ExternalDefinition;
                    ExternalDefinition flstKennzExtDef = dg.Definitions.get_Item("Flurstückskennzeichen") as ExternalDefinition;
                    ExternalDefinition AmtlFlaecheExtDef = dg.Definitions.get_Item("Amtliche Fläche") as ExternalDefinition;
                    ExternalDefinition flurNummerExtDef = dg.Definitions.get_Item("Flurnummer") as ExternalDefinition;
                    ExternalDefinition zeitpunktExtDef = dg.Definitions.get_Item("Zeitpunkt der Entstehung") as ExternalDefinition;
                    ExternalDefinition landExtDef = dg.Definitions.get_Item("Land") as ExternalDefinition;
                    ExternalDefinition regBezirkExtDef = dg.Definitions.get_Item("Regierungsbezirk") as ExternalDefinition;
                    ExternalDefinition kreisExtDef = dg.Definitions.get_Item("Kreis") as ExternalDefinition;
                    ExternalDefinition gemeindeExtDef = dg.Definitions.get_Item("Gemeinde") as ExternalDefinition;
                    ExternalDefinition dienststelleExtDef = dg.Definitions.get_Item("Dienststelle") as ExternalDefinition;
                    ExternalDefinition nachnameOderFirmaExtDef = dg.Definitions.get_Item("Nachname oder Firma") as ExternalDefinition;
                    ExternalDefinition vornameExtDef = dg.Definitions.get_Item("Vorname") as ExternalDefinition;
                    ExternalDefinition ortExtDef = dg.Definitions.get_Item("Ort") as ExternalDefinition;
                    ExternalDefinition plzExtDef = dg.Definitions.get_Item("PLZ") as ExternalDefinition;
                    ExternalDefinition strasseExtDef = dg.Definitions.get_Item("Strasse") as ExternalDefinition;
                    ExternalDefinition hausnummerExtDef = dg.Definitions.get_Item("Hausnummer") as ExternalDefinition;

                    Transaction tParam = new Transaction(doc, "Insert Parameter");
                    {
                        tParam.Start();
                        InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                        doc.ParameterBindings.Insert(gemarkSchlüsselExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(gemarkNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(flstNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(flstKennzExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(AmtlFlaecheExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(flurNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(zeitpunktExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(landExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(regBezirkExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(kreisExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(gemeindeExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(dienststelleExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(nachnameOderFirmaExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(vornameExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(ortExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(plzExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(strasseExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(hausnummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);

                    }
                    tParam.Commit();
                    this.Refresh();
                    int iEigent = 0;
                    foreach (XmlNode flstnodeExt in flst_list)
                    {
                        XmlNodeList pos_nachIdExt = flstnodeExt.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);

                        List<XmlNode> leerList = new List<XmlNode>();
                        List<string> innerTextList = new List<string>();


                        #region Knoten Parameter
                        XmlNode gemarkSchl = flstnodeExt.SelectSingleNode("ns2:gemarkung/ns2:AX_Gemarkung_Schluessel/ns2:land", nsmgr);
                        XmlNode gemarkNumm = flstnodeExt.SelectSingleNode("ns2:gemarkung/ns2:AX_Gemarkung_Schluessel/ns2:gemarkungsnummer", nsmgr);
                        XmlNode flstNummZ = flstnodeExt.SelectSingleNode("ns2:flurstuecksnummer/ns2:AX_Flurstuecksnummer/ns2:zaehler", nsmgr);
                        XmlNode flstNummN = flstnodeExt.SelectSingleNode("ns2:flurstuecksnummer/ns2:AX_Flurstuecksnummer/ns2:nenner", nsmgr);
                        XmlNode flstKennz = flstnodeExt.SelectSingleNode("ns2:flurstueckskennzeichen", nsmgr);
                        XmlNode amtlFlae = flstnodeExt.SelectSingleNode("ns2:amtlicheFlaeche", nsmgr);
                        XmlNode flNr = flstnodeExt.SelectSingleNode("ns2:flurnummer", nsmgr);
                        XmlNode zDE = flstnodeExt.SelectSingleNode("ns2:zeitpunktDerEntstehung", nsmgr);
                        XmlNode land = flstnodeExt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:land", nsmgr);
                        XmlNode regBez = flstnodeExt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:regierungsbezirk", nsmgr);
                        XmlNode kreis = flstnodeExt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:kreis", nsmgr);
                        XmlNode gemeinde = flstnodeExt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:gemeinde", nsmgr);
                        XmlNode dienStL = flstnodeExt.SelectSingleNode("ns2:zustaendigeStelle/ns2:AX_Dienststelle_Schluessel/ns2:land", nsmgr);
                        XmlNode dienStS = flstnodeExt.SelectSingleNode("ns2:zustaendigeStelle/ns2:AX_Dienststelle_Schluessel/ns2:stelle", nsmgr);
                        XmlNode personNachnameOderFirma = default(XmlNode);
                        XmlNode vorname = default(XmlNode);
                        XmlNode ort = default(XmlNode);
                        XmlNode plz = default(XmlNode);
                        XmlNode strasse = default(XmlNode);
                        XmlNode hausnummer = default(XmlNode);

                        leerList.Add(gemarkSchl);
                        leerList.Add(gemarkNumm);
                        leerList.Add(flstNummZ);
                        leerList.Add(flstNummN);
                        leerList.Add(flstKennz);
                        leerList.Add(amtlFlae);
                        leerList.Add(flNr);
                        leerList.Add(zDE);
                        leerList.Add(land);
                        leerList.Add(regBez);
                        leerList.Add(kreis);
                        leerList.Add(gemeinde);
                        leerList.Add(dienStL);
                        leerList.Add(dienStS);
                        leerList.Add(personNachnameOderFirma);
                        leerList.Add(vorname);
                        leerList.Add(ort);
                        leerList.Add(plz);
                        leerList.Add(strasse);
                        leerList.Add(hausnummer);

                        for (int x = 0; x<leerList.Count;x++)
                        {
                            try
                            {
                                innerTextList.Add(leerList[x].InnerText);
                            }
                            catch
                            {
                                innerTextList.Add("-");
                            }
                        }


                        //innerTextList.Add(gemarkSchl.InnerText);
                        //innerTextList.Add(gemarkNumm.InnerText);
                        //innerTextList.Add(flstNummZ.InnerText);
                        //innerTextList.Add(flstNummN.InnerText);
                        //innerTextList.Add(flstKennz.InnerText);
                        //innerTextList.Add(amtlFlae.InnerText);
                        //innerTextList.Add(flNr.InnerText);
                        //innerTextList.Add(zDE.InnerText);
                        //innerTextList.Add(land.InnerText);
                        //innerTextList.Add(regBez.InnerText);
                        //innerTextList.Add(kreis.InnerText);
                        //innerTextList.Add(gemeinde.InnerText);
                        //innerTextList.Add(dienStL.InnerText);
                        //innerTextList.Add(dienStS.InnerText);
                        //innerTextList.Add(personNachnameOderFirma.InnerText);
                        //innerTextList.Add(vorname.InnerText);
                        //innerTextList.Add(ort.InnerText);
                        //innerTextList.Add(plz.InnerText);
                        //innerTextList.Add(strasse.InnerText);
                        //innerTextList.Add(hausnummer.InnerText);

                        //Knoten für die über xlink verknüpften Daten, wie z.B. Eigentümerdaten

                        //Flurstück auslesen: ID für Buchungsblatt
                        XmlNodeList flstIstGebuchtList = (xmlDoc.SelectNodes("//ns2:AX_Flurstueck/ns2:istGebucht", nsmgr));

                        string flstIstGebuchtHrefId = flstIstGebuchtList[iEigent].Attributes["xlink:href"].Value;

                        //Liest pro Flurstück den HRef Wert für "istGebucht" aus
                        //Console.WriteLine("flstIstGebuchtHrefId: " + flstIstGebuchtHrefId); 
                        string flstIstGebuchtId = flstIstGebuchtHrefId.Substring(flstIstGebuchtHrefId.Length - 16);


                        //Buchungsblatt auslesen: ID für Buchungsstelle
                        var buchungsStelleZuFlst = xmlDoc.SelectSingleNode("//ns2:AX_Buchungsstelle[@gml:id='" + flstIstGebuchtId + "']", nsmgr);
                        var buchungsstelleIBVHrefId2 = buchungsStelleZuFlst["istBestandteilVon"].Attributes["xlink:href"].Value;
                        //urn:adv:oid:DEBBAL0600000WY6
                        string buchungsstelleIBVHrefId2M16 = buchungsstelleIBVHrefId2.Substring(buchungsstelleIBVHrefId2.Length - 16);

                        //Buchungsblatt suchen, auf welches sich die Buchungsstelle mit ihrer HrefId bezieht
                        var buchungsblattZuBuchungsstelle = xmlDoc.SelectSingleNode("//ns2:AX_Buchungsblatt[@gml:id='" + buchungsstelleIBVHrefId2M16 + "']", nsmgr);

                        //Namensnummer suchen, die bestandteil der Buchungsstelle ist
                        var namensnummerZuBuchungsstelle = xmlDoc.SelectSingleNode("//ns2:AX_Namensnummer/ns2:istBestandteilVon[@xlink:href='" + buchungsstelleIBVHrefId2 + "']", nsmgr);
                        string personNachnameOderFirmaString = default(string);
                        string vornameString = default(string);
                        XmlNode personZuNamensnummer = default(XmlNode);
                        string ortString = default(string);
                        string plzString = default(string);
                        string strasseString = default(string);
                        string hausnummerString = default(string);

                        if (namensnummerZuBuchungsstelle == null)
                        {
                            personNachnameOderFirma = null;
                            personNachnameOderFirmaString = "-";
                            vornameString = "-";
                        }
                        else if (namensnummerZuBuchungsstelle != null)
                        {
                            //"benennt"-HrefId suchen, um Person zu finden
                            var namensnummerBenenntHrefId = namensnummerZuBuchungsstelle.ParentNode["benennt"].Attributes["xlink:href"].Value;
                            string namensnummerBenenntHrefIdM16 = namensnummerBenenntHrefId.Substring(namensnummerBenenntHrefId.Length - 16);

                            //Person suchen, die zur "benennt" -href passt
                            personZuNamensnummer = xmlDoc.SelectSingleNode("//ns2:AX_Person[@gml:id='" + namensnummerBenenntHrefIdM16 + "']", nsmgr);
                            personNachnameOderFirma = personZuNamensnummer["nachnameOderFirma"];
                            personNachnameOderFirmaString = personNachnameOderFirma.InnerText;
                            if (personZuNamensnummer.ChildNodes[6].Name == "vorname")
                            {
                                vorname = personZuNamensnummer["vorname"];
                                vornameString = vorname.InnerText;
                            }
                            else
                            {
                                //MessageBox.Show("nein");
                                vornameString = "-";
                            }
                        }                    

                        var personHatHrefId = default(string);
                        var personHatHrefIdM16 = default(string);

                        if (personZuNamensnummer == null)
                        {

                        }
                        else
                        {
                            if (personZuNamensnummer["hat"] == null)
                            {
                                personHatHrefId = "-";
                            }
                            else
                            {
                                personHatHrefId = personZuNamensnummer["hat"].Attributes["xlink:href"].Value;

                            }
                            personHatHrefId = personZuNamensnummer["hat"].Attributes["xlink:href"].Value;
                            personHatHrefIdM16 = personHatHrefId.Substring(personHatHrefId.Length - 16);
                        }

                        XmlNode anschriftZuPerson = xmlDoc.SelectSingleNode("//ns2:AX_Anschrift[@gml:id='" + personHatHrefIdM16 + "']", nsmgr);
                        if (anschriftZuPerson == null)
                        {
                        }
                        else
                        {

                            if (anschriftZuPerson["ort_Post"] == null)
                            {
                                ortString = "-";
                            }
                            else
                            {
                                ort = anschriftZuPerson["ort_Post"];
                                ortString = ort.InnerText;
                            }
                            if (anschriftZuPerson["postleitzahlPostzustellung"] == null)
                            {
                                plzString = "-";
                            }
                            else
                            {
                                plz = anschriftZuPerson["postleitzahlPostzustellung"];
                                plzString = plz.InnerText;
                            }
                            if (anschriftZuPerson["strasse"] == null)
                            {
                                strasseString = "-";
                            }
                            else
                            {
                                strasse = anschriftZuPerson["strasse"];
                                strasseString = strasse.InnerText;
                            }
                            if (anschriftZuPerson["hausnummer"] == null)
                            {
                                hausnummerString = "-";
                            }
                            else
                            {
                                hausnummer = anschriftZuPerson["hausnummer"];
                                hausnummerString = hausnummer.InnerText;
                            }
                        }
                        iEigent++;
                        
                            #endregion Knoten Parameter

                            List<string> listAreaExt = new List<String>();

                        int countExt = 0;
                        List<CurveLoop> cLoopListExt = new List<CurveLoop>();
                        CurveLoop cLoopLineExt = new CurveLoop();

                        foreach (XmlNode nodeExt in pos_nachIdExt)
                        {
                            listAreaExt.Add(nodeExt.InnerText);

                            string[] ssizeExt = listAreaExt[countExt].Split(' ');

                            XYZ startPoint;
                            XYZ endPoint;

                            double startX = Convert.ToDouble(ssizeExt[0], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeExt[1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeExt[2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeExt[3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClExt = Line.CreateBound(tStartPoint, tEndPoint);

                            cLoopLineExt.Append(lineClExt);

                            countExt++;
                        }

                        if (cLoopLineExt.GetExactLength() > 0)
                        {
                            cLoopListExt.Add(cLoopLineExt);
                        }

                        if (cLoopListExt.Count() > 0)
                        {
                            Transaction tExtFlst = new Transaction(doc, "Create Exterior");
                            {
                                FailureHandlingOptions options = tExtFlst.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new AxesFailure());
                                tExtFlst.SetFailureHandlingOptions(options);

                                tExtFlst.Start();
                                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                                SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListExt, elementIdFlst);
                            }
                            tExtFlst.Commit();
                            this.Refresh();

                            ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                            foreach (Element el in eleColle)
                            {
                                #region Parameter
                                Parameter parameter0 = el.LookupParameter("Kommentare");
                                Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                                Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                                Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                                Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                                Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                                Parameter parameter6 = el.LookupParameter("Flurnummer");
                                Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                                Parameter parameter8 = el.LookupParameter("Land");
                                Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                                Parameter parameter10 = el.LookupParameter("Kreis");
                                Parameter parameter11 = el.LookupParameter("Gemeinde");
                                Parameter parameter12 = el.LookupParameter("Dienststelle");
                                Parameter parameter13 = el.LookupParameter("Nachname oder Firma");
                                Parameter parameter14 = el.LookupParameter("Vorname");
                                Parameter parameter15 = el.LookupParameter("Ort");
                                Parameter parameter16 = el.LookupParameter("PLZ");
                                Parameter parameter17 = el.LookupParameter("Strasse");
                                Parameter parameter18 = el.LookupParameter("Hausnummer");




                                #endregion parameter

                                using (Transaction t = new Transaction(doc, "parameter"))
                                {
                                    t.Start("Parameterwerte hinzufügen");
                                    try
                                    {
                                        if (parameter0.HasValue.Equals(false))
                                        {
                                            parameter0.Set("Exterior-Flaeche");
                                            if (parameter1.HasValue.Equals(false))
                                            {
                                                for (int y = 0; y<innerTextList.Count();y++)
                                                {
                                                    parameter1.Set(innerTextList[0]);
                                                    parameter2.Set(innerTextList[1]);
                                                    if (flstNummN != null)
                                                    {
                                                        parameter3.Set(innerTextList[2] + "/" + innerTextList[3]);
                                                    }
                                                    else if (flstNummN == null)
                                                    {
                                                        parameter3.Set(innerTextList[2]);
                                                    }

                                                    parameter4.Set(innerTextList[4]);
                                                    parameter5.Set(innerTextList[5]);
                                                    parameter6.Set(innerTextList[6]);
                                                    parameter7.Set(innerTextList[7]);
                                                    parameter8.Set(innerTextList[8]);
                                                    parameter9.Set(innerTextList[9]);
                                                    parameter10.Set(innerTextList[10]);
                                                    parameter11.Set(innerTextList[10]);
                                                    parameter12.Set(innerTextList[12] + "/" + innerTextList[13]);
                                                    parameter13.Set(innerTextList[14]);
                                                    parameter14.Set(innerTextList[15]);
                                                    parameter15.Set(innerTextList[16]);
                                                    parameter16.Set(innerTextList[17]);
                                                    parameter17.Set(innerTextList[18]);
                                                    parameter18.Set(innerTextList[19]);

                                                }

                                                //parameter1.Set(gemarkSchl.InnerText);
                                                //parameter2.Set(gemarkNumm.InnerText);
                                                //if (flstNummN != null)
                                                //{
                                                //    parameter3.Set(flstNummZ.InnerText + "/" + flstNummN.InnerText);
                                                //}
                                                //else if (flstNummN == null)
                                                //{
                                                //    parameter3.Set(flstNummZ.InnerText);
                                                //}

                                                //parameter4.Set(flstKennz.InnerText);
                                                //parameter5.Set(amtlFlae.InnerText);
                                                //parameter6.Set(flNr.InnerText);
                                                //parameter7.Set(zDE.InnerText);
                                                //parameter8.Set(land.InnerText);
                                                //parameter9.Set(regBez.InnerText);
                                                //parameter10.Set(kreis.InnerText);
                                                //parameter11.Set(gemeinde.InnerText);
                                                //parameter12.Set(dienStL.InnerText + "/" + dienStS.InnerText);
                                                //parameter13.Set(personNachnameOderFirmaString);
                                                //parameter14.Set(vornameString);
                                                //parameter15.Set(ortString);
                                                //parameter16.Set(plzString);
                                                //parameter17.Set(strasseString);
                                                //parameter18.Set(hausnummerString);
                                            }
                                        }

                                        else if (parameter1.HasValue.Equals(true))
                                        {

                                        }
                                    }
                                    catch { }
                                    t.Commit();
                                    this.Refresh();
                                }
                            }
                        }
                    }
                }
            }
            #endregion sitesubregion exterior
            #region sitesubregion interior
            //analog zu den Exterior Flurstücken werden die interior Flurstücke importiert. Die SP Datei kann hierbeig weitergenutzt werden, d.h. sie muss nicht neu erstellt werden.
            //Für die Interior FLurstücke gibt es in der vorliegenden ALKIS Datei keine Parameter, deswegen wurde lediglich der Vermerk "interior" in den Kommentaren in Revit angebracht. 
            foreach (DefinitionGroup dg in sharedParameterFile.Groups)
            {
                if (dg.Name == "Flurstuecksdaten")
                {
                    ExternalDefinition gemarkSchlüsselExtDef = dg.Definitions.get_Item("Gemarkungsschlüssel") as ExternalDefinition;          
                    ExternalDefinition gemarkNummerExtDef = dg.Definitions.get_Item("Gemarkungsnummer") as ExternalDefinition;
                    ExternalDefinition flstNummerExtDef = dg.Definitions.get_Item("Flurstücksnummer") as ExternalDefinition;
                    ExternalDefinition flstKennzExtDef = dg.Definitions.get_Item("Flurstückskennzeichen") as ExternalDefinition;
                    ExternalDefinition AmtlFlaecheExtDef = dg.Definitions.get_Item("Amtliche Fläche") as ExternalDefinition;
                    ExternalDefinition flurNummerExtDef = dg.Definitions.get_Item("Flurnummer") as ExternalDefinition;
                    ExternalDefinition zeitpunktExtDef = dg.Definitions.get_Item("Zeitpunkt der Entstehung") as ExternalDefinition;
                    ExternalDefinition landExtDef = dg.Definitions.get_Item("Land") as ExternalDefinition;
                    ExternalDefinition regBezirkExtDef = dg.Definitions.get_Item("Regierungsbezirk") as ExternalDefinition;
                    ExternalDefinition kreisExtDef = dg.Definitions.get_Item("Kreis") as ExternalDefinition;
                    ExternalDefinition gemeindeExtDef = dg.Definitions.get_Item("Gemeinde") as ExternalDefinition;
                    ExternalDefinition dienststelleExtDef = dg.Definitions.get_Item("Dienststelle") as ExternalDefinition;

                    Transaction tParam = new Transaction(doc, "Insert Parameter");
                    {
                        tParam.Start();

                        InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                        doc.ParameterBindings.Insert(gemarkSchlüsselExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(gemarkNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(flstNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(flstKennzExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(AmtlFlaecheExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(flurNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(zeitpunktExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(landExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(regBezirkExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(kreisExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(gemeindeExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                        doc.ParameterBindings.Insert(dienststelleExtDef, newIB, BuiltInParameterGroup.PG_DATA);
                    }
                    tParam.Commit();
                    this.Refresh();

                    foreach (XmlNode flstnodeInt in flst_listInt)
                    {
                        XmlNodeList pos_nachIdInt = flstnodeInt.SelectNodes("gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);

                        #region Knoten Parameter
                        XmlNode gemarkSchl = flstnodeInt.SelectSingleNode("ns2:gemarkung/ns2:AX_Gemarkung_Schluessel/ns2:land", nsmgr);
                        XmlNode gemarkNumm = flstnodeInt.SelectSingleNode("ns2:gemarkung/ns2:AX_Gemarkung_Schluessel/ns2:gemarkungsnummer", nsmgr);
                        XmlNode flstNummZ = flstnodeInt.SelectSingleNode("ns2:flurstuecksnummer/ns2:AX_Flurstuecksnummer/ns2:zaehler", nsmgr);
                        XmlNode flstNummN = flstnodeInt.SelectSingleNode("ns2:flurstuecksnummer/ns2:AX_Flurstuecksnummer/ns2:nenner", nsmgr);
                        XmlNode flstKennz = flstnodeInt.SelectSingleNode("ns2:flurstueckskennzeichen", nsmgr);
                        XmlNode amtlFlae = flstnodeInt.SelectSingleNode("ns2:amtlicheFlaeche", nsmgr);
                        XmlNode flNr = flstnodeInt.SelectSingleNode("ns2:flurnummer", nsmgr);
                        XmlNode zDE = flstnodeInt.SelectSingleNode("ns2:zeitpunktDerEntstehung", nsmgr);
                        XmlNode land = flstnodeInt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:land", nsmgr);
                        XmlNode regBez = flstnodeInt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:regierungsbezirk", nsmgr);
                        XmlNode kreis = flstnodeInt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:kreis", nsmgr);
                        XmlNode gemeinde = flstnodeInt.SelectSingleNode("ns2:gemeindezugehoerigkeit/ns2:AX_Gemeindekennzeichen/ns2:gemeinde", nsmgr);
                        XmlNode dienStL = flstnodeInt.SelectSingleNode("ns2:zustaendigeStelle/ns2:AX_Dienststelle_Schluessel/ns2:land", nsmgr);
                        XmlNode dienStS = flstnodeInt.SelectSingleNode("ns2:zustaendigeStelle/ns2:AX_Dienststelle_Schluessel/ns2:stelle", nsmgr);
                        #endregion Knoten Parameter

                        List<string> listAreaInt = new List<String>();

                        int countInt = 0;

                        List<CurveLoop> cLoopListInt = new List<CurveLoop>();
                        CurveLoop cLoopLineInt = new CurveLoop();

                        foreach (XmlNode nodeInt in pos_nachIdInt)
                        {
                            listAreaInt.Add(nodeInt.InnerText);

                            string[] ssizeInt = listAreaInt[countInt].Split(' ');

                            XYZ startPoint;
                            XYZ endPoint;

                            double startX = Convert.ToDouble(ssizeInt[0], System.Globalization.CultureInfo.InvariantCulture);
                            double startXm = startX * feetToMeter;
                            double startXmRedu = startXm / R;
                            double startY = Convert.ToDouble(ssizeInt[1], System.Globalization.CultureInfo.InvariantCulture);
                            double startYm = startY * feetToMeter;
                            double startYmRedu = startYm / R;
                            double startZ = 0.000;
                            double startZm = startZ * feetToMeter;

                            double endX = Convert.ToDouble(ssizeInt[2], System.Globalization.CultureInfo.InvariantCulture);
                            double endXm = endX * feetToMeter;
                            double endXmRedu = endXm / R;
                            double endY = Convert.ToDouble(ssizeInt[3], System.Globalization.CultureInfo.InvariantCulture);
                            double endYm = endY * feetToMeter;
                            double endYmRedu = endYm / R;
                            double endZ = 0.000;
                            double endZm = endZ * feetToMeter;

                            startPoint = new XYZ(startXmRedu, startYmRedu, startZm);
                            endPoint = new XYZ(endXmRedu, endYmRedu, endZm);

                            XYZ tStartPoint = transf.OfPoint(startPoint);
                            XYZ tEndPoint = transf.OfPoint(endPoint);

                            Line lineClInt = Line.CreateBound(tStartPoint, tEndPoint);
                            cLoopLineInt.Append(lineClInt);

                            countInt++;
                        }

                        if (cLoopLineInt.GetExactLength() > 0)
                        {
                            cLoopListInt.Add(cLoopLineInt);
                        }

                        if (cLoopListInt.Count() > 0)
                        {
                            Transaction tIntFlst = new Transaction(doc, "Create Interior");
                            {
                                FailureHandlingOptions options = tIntFlst.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new AxesFailure());
                                tIntFlst.SetFailureHandlingOptions(options);

                                tIntFlst.Start();
                                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                                SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListInt, elementIdFlst);
                            }
                            tIntFlst.Commit();
                            this.Refresh();

                            ICollection<Element> eleColle = SelectAllElements(uidoc, doc);

                            foreach (Element el in eleColle)
                            {
                                #region Parameter
                                Parameter parameter0 = el.LookupParameter("Kommentare");
                                Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
                                Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
                                Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
                                Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
                                Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
                                Parameter parameter6 = el.LookupParameter("Flurnummer");
                                Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
                                Parameter parameter8 = el.LookupParameter("Land");
                                Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
                                Parameter parameter10 = el.LookupParameter("Kreis");
                                Parameter parameter11 = el.LookupParameter("Gemeinde");
                                Parameter parameter12 = el.LookupParameter("Dienststelle");
                                #endregion parameter

                                using (Transaction t = new Transaction(doc, "parameter"))
                                {
                                    t.Start("Parameterwerte hinzufügen");
                                    try
                                    {
                                        if (parameter0.HasValue.Equals(false))
                                        {
                                            parameter0.Set("Interior-Flaeche");
                                            parameter1.Set(" ");
                                        }
                                    }
                                    catch { }
                                    t.Commit();
                                    this.Refresh();
                                }
                            }
                        }
                    }
                }
            }
            #endregion sitesubregion interior
        }
    }
} 
