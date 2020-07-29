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
using Form = System.Windows.Forms.Form;
using MessageBox = System.Windows.Forms.MessageBox;

namespace City2RVT.GUI.Modify
{
    public partial class editIfcProperties : Form
    {
        ExternalCommandData commandData;

        public editIfcProperties(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
            propertyListBox.MouseDoubleClick += new MouseEventHandler(propertyListBox_DoubleClick);
        }


        private void propertyListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //UIApplication app = commandData.Application;
            //UIDocument uidoc = app.ActiveUIDocument;
            //Document doc = uidoc.Document;

            //FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);

            //foreach (Element topo in topoCollector)
            //{
            //    TopographySurface topoSurf = doc.GetElement(topo.UniqueId.ToString()) as TopographySurface;

            //    string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            //    if (bezeichnung == null)
            //    {
            //        bezeichnung = "-";
            //    }
            //}
        }

        void propertyListBox_DoubleClick(object sender, MouseEventArgs e)
        {
            if (propertyListBox.SelectedItem != null)
            {
                //MessageBox.Show(propertyListBox.SelectedItem.ToString());
                GUI.Prop_NAS_settings.SelectedElement = propertyListBox.SelectedItem.ToString();

                Modify.editProperties f1 = new Modify.editProperties(commandData);
                _ = f1.ShowDialog();
            }
        }

        private void editIfcProperties_Load(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            List<string> bezList = new List<string>();

            foreach (Element topo in topoCollector)
            {
                TopographySurface topoSurf = doc.GetElement(topo.UniqueId.ToString()) as TopographySurface;

                string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                if (bezeichnung == null)
                {
                    bezeichnung = "-";
                }
                if (bezList.Contains(bezeichnung) == false && bezeichnung.StartsWith("Reference plane") == false)
                {
                    bezList.Add(bezeichnung);
                }
            }

            //var paramList = GUI.Prop_NAS_settings.ParamList;
            var paramList = bezList;


            int ix = 0;
            foreach (string item in paramList)
            {
                propertyListBox.Items.Add(paramList[ix]);
                ix++;
            }

            //for (int i = 0; i < propertyListBox.Items.Count; i++)
            //{
            //    propertyListBox.SetItemChecked(i, true);
            //}
        }
    }
}
