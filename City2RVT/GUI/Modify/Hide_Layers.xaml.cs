using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;


namespace City2RVT.GUI.Modify
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

            // Set an icon using code
            Uri iconUri = new Uri("pack://application:,,,/City2RVT;component/img/HideLayerIcon_32px_96dpi.ico", UriKind.RelativeOrAbsolute);
            this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);

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
                else if (bezeichnung.StartsWith("Reference plane") == true)
                {
                    if (xPlanObjectList.Contains(bezeichnung.Substring(bezeichnung.LastIndexOf(':') + 2)) == false)
                    {
                        xPlanObjectList.Add(bezeichnung.Substring(bezeichnung.LastIndexOf(':') + 2));
                    }
                }

                var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                if (topoSurf.IsHidden(view) == false)
                {
                    if (visibleElements.Contains(bezeichnung) == false)
                    {
                        if (bezeichnung.StartsWith("Reference plan"))
                        {
                            visibleElements.Add(bezeichnung.Substring(bezeichnung.LastIndexOf(':') + 2));
                        }
                        else
                        {
                            visibleElements.Add(bezeichnung);
                        }
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

        private void Button_Click_ShowSelectedLayer(object sender, RoutedEventArgs e)
        {
            // this button hides all elements of type OST_Topography by default 
            // and unhides alle elements that are selected by the user to be visible

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);

            var topoIds = new List<ElementId>();

            foreach (var id in topoCollector)
            {
                topoIds.Add(id.Id);
            }

            foreach (var t in topoCollector)
            {
                using (var transHideAll = new Transaction(doc, "Test"))
                {
                    transHideAll.Start();
                    var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                    if (view != null)
                    {
                        view.HideElements(topoIds);
                    }
                    transHideAll.Commit();
                }
            }


            var selectedLayer = categoryListbox2.SelectedItems;
            foreach (var sl in selectedLayer)
            {
                var hiddenTopoIds = new List<ElementId>();

                var collectorRefPlanes = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == "Reference plane: " + sl.ToString()).Cast<Element>().ToList();

                foreach (var id in collectorRefPlanes)
                {
                    hiddenTopoIds.Add(id.Id);
                }

                var collectorTopographies = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == sl.ToString()).Cast<Element>().ToList();                

                foreach (var id in collectorTopographies)
                {
                    hiddenTopoIds.Add(id.Id);
                }

                using (var tran = new Transaction(doc, "Test"))
                {
                    tran.Start();
                    var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                    if (view != null)
                    {
                        view.UnhideElements(hiddenTopoIds);
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

        private void Button_Click_Unselect_All(object sender, RoutedEventArgs e)
        {
            categoryListbox2.UnselectAll();
            radioButton1.IsChecked = false;
        }
    }
}
