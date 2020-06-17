using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using System.IO;
using City2RVT.Calc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.UtilityResource;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan2IFC.xaml
    /// </summary>
    public partial class Wpf_XPlan2IFC : Window
    {
        ExternalCommandData commandData;

        public Wpf_XPlan2IFC(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();

            // Set an icon using code
            Uri iconUri = new Uri("pack://application:,,,/City2RVT;component/img/IFC_32px_96dpi.ico", UriKind.RelativeOrAbsolute);
            this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        }

        private void Button_Click_IfcExport(object sender, RoutedEventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            var transfClass = new Transformation();
            Transform transf = transfClass.transform(doc);
            XYZ pbp = transfClass.getProjectBasePoint(doc);
            double angle = transfClass.getAngle(doc);

            Builder.IfcBuilder ifcBuilder = new Builder.IfcBuilder();
            string original = ifcBuilder.getRevitDefaultExportPath(ifc_Location.Text, doc, commandData);

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            FilteredElementCollector wallCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls);
            FilteredElementCollector roofCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs);

            FilteredElementCollector buildingElements = wallCollector.UnionWith(roofCollector);


            var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;

            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "land2ifc",
                ApplicationFullName = "IFC Export LandBIM",
                ApplicationIdentifier = "LandBIM_IFC",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Meier",
                EditorsGivenName = "Sören",
                EditorsOrganisationName = "HTW Dresden"
            };

            using (IfcStore model = IfcStore.Open(original,editor))
            {
                IfcProject ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();

                ifcBuilder.editRevitExport(model, doc);

                foreach (Element t in topoCollector)
                { 
                    TopographySurface topoSurf = doc.GetElement(t.UniqueId.ToString()) as TopographySurface;
                    IList<XYZ> topoPoints = topoSurf.GetPoints();
                    ParameterSet topoParams = topoSurf.Parameters;

                    string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                    if (bezeichnung == null)
                    {
                        bezeichnung = "-";
                    }

                    // Diese IF-Anweisung bewirkt, dass die Referenzflächen, also die Boundingboxen, die "gedachte Flächen", d.h. keine existierenden Flächen sondern 
                    // nur für die Darstellung in Revit notwendig sind, nicht nach IFC exportiert werden.
                    if (bezeichnung.StartsWith("Reference plane") == false && bezeichnung.StartsWith("DTM: ") == false)
                    {
                        IfcXBim.CreateSite(model, topoPoints, topoParams, bezeichnung, topoSurf, pbp, doc);

                        IfcXBim.createSpace(model, topoSurf, commandData, pbp, bezeichnung);
                    }
                }

                var ifcBuildings = model.Instances.OfType<Xbim.Ifc4.ProductExtension.IfcBuilding>();

                foreach (Xbim.Ifc4.ProductExtension.IfcBuilding b in ifcBuildings)
                {
                    IfcXBim.createBuildingSpace(model, buildingElements, commandData, pbp);
                }


                ifcBuilder.createProjectInformation(model, ifcProject, doc);

                string save;
                if (string.IsNullOrWhiteSpace(ifc_name_textbox.Text))
                {                    
                    save = @"D:\Daten\revit2ifc.ifc";
                }
                else
                {
                    save = Path.Combine(ifc_Location.Text, ifc_name_textbox.Text + ".ifc");
                }

                model.SaveAs(@save);
            }  
        }

        private void ifc_Location_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void ifc_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_SetIfcLocation(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            DialogResult folder = dlg.ShowDialog();
            string locationFolder = dlg.SelectedPath;
            ifc_Location.Text = locationFolder;
        }
    }
}
