using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

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
using View = Autodesk.Revit.DB.View;

namespace City2RVT.GUI.Modify
{
    public partial class editIfcProperties : Form
    {
        Form someForm;
        ExternalCommandData commandData;

        public editIfcProperties(ExternalCommandData cData, Form parentForm)
        {
            commandData = cData;
            InitializeComponent();
            someForm = parentForm;
            propertyListBox.MouseDoubleClick += new MouseEventHandler(propertyListBox_DoubleClick);
        }

        private void propertyListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        void propertyListBox_DoubleClick(object sender, MouseEventArgs e)
        {
            if (propertyListBox.SelectedItem != null)
            {
                Prop_NAS_settings.SelectedElement = propertyListBox.SelectedItem.ToString();

                EditProperties f1 = new EditProperties(commandData);
                f1.Text = propertyListBox.SelectedItem.ToString();
                _ = f1.ShowDialog();
            }
        }

        private void editIfcProperties_Load(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            var selected = Prop_NAS_settings.SelectedPset;

            if (selected == "BauantragGrundstück")
            {
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
                    if (!bezList.Contains(bezeichnung) && !bezeichnung.StartsWith("Reference plane"))
                    {
                        bezList.Add(bezeichnung);
                    }
                }

                var paramList = bezList;


                int ix = 0;
                foreach (string item in paramList)
                {
                    propertyListBox.Items.Add(paramList[ix]);
                    ix++;
                }
            }

            else if (selected == "BauantragGebäude")
            {
                propertyListBox.Items.Add("Gebäude");
            }

            else if (selected == "BauantragGeschoss")
            {
                propertyListBox.Items.Add("Geschoss");
            }



        }

        private void pickElement_Click(object sender, EventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Autodesk.Revit.DB.View view = doc.ActiveView;
            Element pickedElement = Prop_Revit.PickedElement;

            this.Hide();
            someForm.Hide();

            Selection choices = uidoc.Selection;
            Reference hasPickOne = choices.PickObject(ObjectType.Element, "Please select the element to change ifc properties. ");

            pickedElement = doc.GetElement(hasPickOne);

            Prop_Revit.PickedElement = pickedElement;

            Modify.editElement f1 = new Modify.editElement(commandData);
            //f1.Text = clickedName;
            _ = f1.ShowDialog();


            this.Show();
        }

        private void editIfcProperties_VisibleChanged(object sender, EventArgs e)
        {
            //if (this.Visible == false)
            //{
            //    MessageBox.Show("ausgeblendet");
            //}
        }
    }
}
