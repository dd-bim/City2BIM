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

//shortcut to set json settings
using init = GuiHandler.InitClass;

using rT = BIMGISInteropLibs.RvtTerrain.ConnectionInterface;

namespace City2RVT.GUI.DTM2BIM
{
    /// <summary>
    /// Interaktionslogik für Terrain_ImportUI.xaml
    /// </summary>
    public partial class Terrain_ImportUI : Window
    {
        /// <summary>
        /// return this value to start import in revit cmd 
        /// </summary>
        public bool startTerrainImport { get { return startImport; } }

        /// <summary>
        /// Value to be specified that the import should be started.
        /// </summary>
        private bool startImport { set; get; } = false;


        /// <summary>
        /// init dtm2bim main window
        /// </summary>
        public Terrain_ImportUI()
        {   
            InitializeComponent();
        }

        private void btnStartImport_Click(object sender, RoutedEventArgs e)
        {
            //start mapping process
            startImport = true;
            Close();
        }
    }
}
