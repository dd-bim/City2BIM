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
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.Interfaces;
using IfcBuildingStorey = Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
using Xbim.Ifc2x3.SharedBldgElements;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan2IFC.xaml
    /// </summary>
    public partial class Wpf_XPlan2IFC : Window
    {
        ExternalCommandData commandData;
        GUI_helper guiLogic = new GUI_helper();
        private static double feetToMeter = 1.0 / 0.3048;


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

            ifc_name_textbox.Text = doc.Title;
            var docPathname = Path.GetDirectoryName(doc.PathName);
            ifc_Location.Text = docPathname;

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            List<string> ifcExportList = new List<string>();

            foreach (var t in topoCollector)
            {
                TopographySurface topoSurf = doc.GetElement(t.UniqueId.ToString()) as TopographySurface;
                string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();

                if (bezeichnung.StartsWith("Reference plane") == false)
                {
                    if (ifcExportList.Contains(bezeichnung) == false)
                    {
                        ifcExportList.Add(bezeichnung);
                    }
                }
            }

            int ix = 0;
            foreach (string item in ifcExportList)
            {
                _ = ifcExportListbox.Items.Add(ifcExportList[ix]);
                ix++;
            }
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

            var selectedLayers = ifcExportListbox.SelectedItems;
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

                var ifcRooms = model.Instances.OfType<Xbim.Ifc4.ProductExtension.IfcSpace>();
                //ifcBuilder.CreateEnumeration(model,)
                foreach (var room in ifcRooms)
                {
                    ifcBuilder.CreateUsage(model, room);
                    ifcBuilder.CreateRoomArea(model, room);
                }

                foreach (Element topo in topoCollector)
                { 
                    // Get parameters, point coordinates and geometry of topography surfaces
                    TopographySurface topoSurf = doc.GetElement(topo.UniqueId.ToString()) as TopographySurface;
                    IList<XYZ> topoPoints = topoSurf.GetPoints();
                    ParameterSet topoParams = topoSurf.Parameters;

                    string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                    if (bezeichnung == null)
                    {
                        bezeichnung = "-";
                    }

                    // Diese IF-Anweisung bewirkt, dass die Referenzflächen, also die Boundingboxen, die "gedachte Flächen", d.h. keine existierenden Flächen sondern 
                    // nur für die Darstellung in Revit notwendig sind, nicht nach IFC exportiert werden.
                    if (bezeichnung.StartsWith("Reference plane") == false && bezeichnung.StartsWith("DTM: ") == false && selectedLayers.Contains(bezeichnung))
                    {
                        IfcXBim.CreateSite(model, topoPoints, topoParams, bezeichnung, topoSurf, pbp, doc);
                        IfcXBim.createSpace(model, topoSurf, commandData, pbp, bezeichnung);
                    }
                }

                var ifcBuildings = model.Instances.OfType<Xbim.Ifc4.ProductExtension.IfcBuilding>();
                foreach (Xbim.Ifc4.ProductExtension.IfcBuilding b in ifcBuildings)
                {
                    IfcXBim.CreateBuildingSpace(model, buildingElements, commandData, pbp);
                }

                foreach (Xbim.Ifc4.ProductExtension.IfcBuilding b in ifcBuildings)
                {
                    IfcXBim.CreateFloorSpace(model, buildingElements, commandData, pbp);
                }

                var buldingStory = model.Instances.OfType<IfcBuildingStorey>();
                List<double> storeyElevationList = new List<double>();
                foreach (var bs in buldingStory)
                {
                    double relZ = bs.Elevation.Value;
                    storeyElevationList.Add(relZ);
                }

                List<double> roofHeightList = new List<double>();
                foreach (var r in roofCollector)
                {
                    BoundingBoxXYZ boundingBox = r.get_BoundingBox(view);
                    if (boundingBox != null)
                    {
                        roofHeightList.Add(Convert.ToDouble(boundingBox.Max.Z / feetToMeter));
                    }
                }
                double roofHeight = roofHeightList.Max();

                storeyElevationList.Add(roofHeight);
                var firstEle = storeyElevationList.FirstOrDefault();
                storeyElevationList.RemoveAt(0);
                storeyElevationList.Add(firstEle);

                int i = 0;
                foreach (var bs in buldingStory)
                {
                    if (storeyElevationList[i] > 0)
                    {
                        IfcXBim.createStoreySpace(model, bs, commandData, storeyElevationList[i], wallCollector);
                        i++;
                    }
                }

                ifcBuilder.createProjectInformation(model, ifcProject, doc);

                string save = Path.Combine(ifc_Location.Text, ifc_name_textbox.Text + ".ifc");
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
            //DialogResult folder = dlg.ShowDialog();
            dlg.ShowDialog();
            string locationFolder = dlg.SelectedPath;
            ifc_Location.Text = locationFolder;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            guiLogic.setRadiobutton(rb_selectAll, ifcExportListbox);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ifcExportListbox.UnselectAll();
            rb_selectAll.IsChecked = false;
        }

        private void ifcExportListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            guiLogic.uncheckRbWhenSelected(rb_selectAll, ifcExportListbox);
        }

        private void close_dialoge_button(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
