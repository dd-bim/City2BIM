using System.Windows;

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
        /// 
        /// </summary>
        public bool useDelaunyTriangulation { get { return useDelauny; } }

        /// <summary>
        /// 
        /// </summary>
        private bool useDelauny { set; get; } = false;
        
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

        private void cbProcessing_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbDelauny.IsSelected)
            {
                useDelauny = true;
            }
            else
            {
                useDelauny = false;
            }

            if(cbProcessing.SelectedIndex != -1)
            {
                //set task to done
                GuiHandler.GuiSupport.selectProcessing = true;
                
                //ready checker
                enableStartBtn(GuiHandler.GuiSupport.rdyDTM2BIM());
            }
        }

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
