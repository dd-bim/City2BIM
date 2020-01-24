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
using System.Windows.Shapes;
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

        public Wpf_XPlan()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

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

            #region Transformation und UTM-Reduktion

            double R = 1;

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
            //MessageBox.Show(xPlanGmlPath);
            xmlDoc.Load(xPlanGmlPath);

            #region namespaces
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xplan", "http://www.xplanung.de/xplangml/5/2");

            #endregion namespaces

            XmlNodeList strasse = xmlDoc.SelectNodes("//gml:featureMember/xplan:BP_StrassenVerkehrsFlaeche/xplan:position/gml:Polygon/gml:interior/gml:Ring/gml:curveMember/gml:LineString/gml:posList", nsmgr);
            List<string> list = new List<String>();

            int i = 0;
            foreach (XmlNode node in strasse)
            {
                list.Add(node.InnerText);
                string[] koordWerte = list[i].Split(' ');

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

                xList.Add(xStartMeterRedu);
                yList.Add(yStartMeterRedu);
                xList.Add(xEndMeterRedu);
                yList.Add(yEndMeterRedu);

                //MessageBox.Show(list[i].ToString());
                i++;
            }

            XYZ[] pointsFlst = new XYZ[4];
            pointsFlst[0] = transf.OfPoint(new XYZ(xList.Min(), yList.Min(), 0.0));
            pointsFlst[1] = transf.OfPoint(new XYZ(xList.Max(), yList.Min(), 0.0));
            pointsFlst[2] = transf.OfPoint(new XYZ(xList.Max(), yList.Max(), 0.0));
            pointsFlst[3] = transf.OfPoint(new XYZ(xList.Min(), yList.Max(), 0.0));

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
            //this.Refresh();


        }
    }
}
