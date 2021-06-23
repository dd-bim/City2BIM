using System.Windows;

//shortcut to set json settings
using init = GuiHandler.InitClass;

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
        /// set if delauny triangulation should be processed
        /// </summary>
        public bool usePointsFaces { get { return useFaces; } }

        /// <summary>
        /// setter for user selection
        /// </summary>
        private bool useFaces { set; get; } = false;
        
        /// <summary>
        /// init dtm2bim main window
        /// </summary>
        public Terrain_ImportUI()
        {   
            InitializeComponent();
        }

        /// <summary>
        /// kick off settings for DTM import
        /// </summary>
        private void btnStartImport_Click(object sender, RoutedEventArgs e)
        {
            //start mapping process
            startImport = true;

            //set min dist to default value (TODO - generic)
            init.config.minDist = 1;

            //set is3D to default value (TODO - generic)
            init.config.is3D = true;

            Close();
        }

        /// <summary>
        /// error handling for processing selection
        /// </summary>
        private void cbProcessing_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbFaces.IsSelected)
            {
                useFaces = true;
                //enable filter
                gbSpatialFilter.IsEnabled = false;
            }
            else
            {
                useFaces = false;

                //disable
                gbSpatialFilter.IsEnabled = true;
            }
            
            if(cbProcessing.SelectedIndex != -1)
            {
                //set task to done
                GuiHandler.GuiSupport.selectProcessing = true;
                
                //ready checker
                enableStartBtn(GuiHandler.GuiSupport.rdyDTM2BIM());

                return;
            }
            return;
        }

        /// <summary>
        /// enable or disable button (error handling)
        /// </summary>
        private void enableStartBtn(bool enable)
        {
            if (enable)
            {
                this.btnStartImport.IsEnabled = true;
            }
            else
            {
                this.btnStartImport.IsEnabled = false;
            }
        }
    }
}
