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

//using Xbim.Ifc;
//https://forums.autodesk.com/t5/revit-api-forum/revit-api-s-integration-with-xbim-geometry-microsoft-extensions/td-p/9270358

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan2IFC.xaml
    /// </summary>
    public partial class Wpf_XPlan2IFC : Window
    {
        ExternalCommandData commandData;
        double feetToMeter = 1.0 / 0.3048;

        public Wpf_XPlan2IFC(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        //string locationFolder;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            DialogResult folder = dlg.ShowDialog();
            string locationFolder = dlg.SelectedPath;
            System.Windows.Forms.MessageBox.Show(locationFolder.ToString());
            ifc_Location.Text = locationFolder;
            //Reader.FileDialog ifcLocation = new Reader.FileDialog();
            //ifc_Location.Text = ifcLocation.ImportPath(Reader.FileDialog.Data.XPlanGML);
        }

        private void ifc_Location_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}
