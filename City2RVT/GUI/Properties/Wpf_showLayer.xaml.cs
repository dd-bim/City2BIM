using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace City2RVT.GUI.Properties
{
    /// <summary>
    /// Interaktionslogik für Wpf_showLayer.xaml
    /// </summary>
    public partial class Wpf_showLayer : Window
    {
        ExternalCommandData commandData;
        public Wpf_showLayer(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
            showLayer();
        }

        public void showLayer()
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            //var selected = Prop_NAS_settings.SelectedPset;

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
                lb_layer.Items.Add(paramList[ix]);
                ix++;
            }
        }

        private void lb_layer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void mdc_layer(object sender, MouseButtonEventArgs e)
        {
            Prop_NAS_settings.SelectedSingleLayer = lb_layer.SelectedItem.ToString();

            Wf_showEntities f1 = new Wf_showEntities(commandData);
            //f1.Text = propertyListBox.SelectedItem.ToString();
            _ = f1.ShowDialog();
        }
    }
}
