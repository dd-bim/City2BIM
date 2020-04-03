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

        public Hide_Layers(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            List<string> xPlanObjectList = new List<string>();

            List<string> visibleElements = new List<string>();
            foreach (var t in topoCollector)
            {
                TopographySurface topoSurf = doc.GetElement(t.UniqueId.ToString()) as TopographySurface;

                string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();

                if (bezeichnung.StartsWith("Reference plane") == false)
                {
                    if (xPlanObjectList.Contains(bezeichnung) == false)
                    {
                        xPlanObjectList.Add(bezeichnung);
                    }                    
                }

                var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                if (topoSurf.IsHidden(view) == false)
                {
                    if (visibleElements.Contains(bezeichnung) == false)
                    {
                        visibleElements.Add(bezeichnung);
                    }
                }
            }
            xPlanObjectList.Sort();

            foreach (var elem in visibleElements)
            {
                categoryListbox2.SelectedItems.Add(elem);
            }

            int ix = 0;
            foreach (string item in xPlanObjectList)
            {
                categoryListbox2.Items.Add(xPlanObjectList[ix]);
                ix++;
            }
        }

        private void categoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryListbox2.SelectedItems.Count < categoryListbox2.Items.Count)
            {
                radioButton1.IsChecked = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);

            var preHide = new List<ElementId>();

            foreach (var id in topoCollector)
            {
                preHide.Add(id.Id);
            }

            foreach (var t in topoCollector)
            {
                using (var transHideAll = new Transaction(doc, "Test"))
                {
                    transHideAll.Start();
                    var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                    if (view != null)
                    {
                        view.HideElements(preHide);
                    }
                    transHideAll.Commit();
                }
            }


            var chosen = categoryListbox2.SelectedItems;

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
                        view.UnhideElements(hideIds);
                    }
                    tran.Commit();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (radioButton1.IsChecked == true)
            {
                categoryListbox2.SelectAll();
            }
            else
            {
                categoryListbox2.UnselectAll();

            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            categoryListbox2.UnselectAll();
            radioButton1.IsChecked = false;
        }
    }
}
