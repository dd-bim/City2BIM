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
using System.Windows.Shapes;

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaktionslogik für LoGeoRef50_CRS_Metadata.xaml
    /// </summary>
    public partial class LoGeoRef50_CRS_Metadata : Window
    {
        public LoGeoRef50_CRS_Metadata()
        {
            InitializeComponent();
        }

        /// <summary>
        /// button to close window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
