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
    /// Interaktionslogik für Hide_Layers.xaml
    /// </summary>
    public partial class Hide_Layers : Window
    {
        ExternalCommandData commandData;
        double feetToMeter = 1.0 / 0.3048;
        string xPlanGmlPath;

        public Hide_Layers(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();

            
            if (Prop_XPLAN_settings.FileUrl == "")
                TaskDialog.Show("No file path set!", "Please enter a file path in the settings window first!");
                
                //String selectedFormat = String.Empty;
                //DialogResult result = DialogResult.OK;
                //this.DialogResult = (result != DialogResult.Cancel ? DialogResult.OK : DialogResult.None);
            else
            {
                //TaskDialog.Show("Tag","Gutn");
                xPlanGmlPath = City2RVT.GUI.XPlan2BIM.Prop_XPLAN_settings.FileUrl;
                //TaskDialog.Show("Tag", importetPath);

                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlDocument xmlDoc = new XmlDocument();

                using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
                {
                    xmlDoc.Load(reader);
                    xmlDoc.Load(xPlanGmlPath);
                }

                #region namespaces
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
                nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
                nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
                nsmgr.AddNamespace("xplan", "http://www.xplanung.de/xplangml/5/2");
                #endregion namespaces

                List<string> xPlanObjectList = new List<string>();
                XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

                foreach (XmlNode x in allXPlanObjects)
                {
                    if (x.FirstChild.SelectNodes(".//gml:exterior", nsmgr) != null)
                    {
                        if (xPlanObjectList.Contains(x.FirstChild.Name.Substring((x.FirstChild.Name).LastIndexOf(':') + 1)) == false)
                        {
                            xPlanObjectList.Add(x.FirstChild.Name.Substring((x.FirstChild.Name).LastIndexOf(':') + 1));
                        }
                    }
                }

                xPlanObjectList.Sort();

                int ix = 0;
                foreach (string item in xPlanObjectList)
                {
                    categoryListBox.Items.Add(xPlanObjectList[ix]);
                    ix++;
                }
            }
        }

        private void categoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            var chosen = categoryListBox.SelectedItems;

            foreach (var c in chosen)
            {
                var collector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == "Reference plane: " + c.ToString()).Cast<Element>().ToList();

                var collector2 = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == c.ToString()).Cast<Element>().ToList();


                var hideIds = new List<ElementId>();

                foreach (var id in collector)
                {
                    hideIds.Add(id.Id);
                }

                foreach (var id in collector2)
                {
                    hideIds.Add(id.Id);
                }

                using (var tran = new Transaction(doc, "Test"))
                {
                    tran.Start();
                    var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                    if (view != null)
                    {
                        view.HideElementsTemporary(hideIds);
                    }
                    tran.Commit();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
