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

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace IFCTerrainGUI.GUI.ExportSettings.Metadata
{
    /// <summary>
    /// Interaktionslogik für metadata_din_spec_91391_2.xaml
    /// </summary>
    public partial class metadata_din_spec_91391_2 : Window
    {
        public metadata_din_spec_91391_2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// enable main window
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }

        /// <summary>
        /// close application and gui log
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();

            // gui logging
            guiLog.setLog(BIMGISInteropLibs.Logging.LogType.info, "Metadata 'DIN SPEC 91391-2' set.");
        }
    }
}
