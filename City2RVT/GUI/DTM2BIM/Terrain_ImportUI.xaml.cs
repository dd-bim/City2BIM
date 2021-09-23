using System;
using System.Windows;
using System.Windows.Data; //interface for converter 

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

//shortcut to set log type
using BIMGISInteropLibs.Logging;

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
        /// init DTM2BIM main window
        /// </summary>
        public Terrain_ImportUI()
        {   
            InitializeComponent();

            //send gui logging
            guiLog.setLog(LogType.info, "Welcome to DTM2BIM");
        }

        /// <summary>
        /// kick off settings for DTM import
        /// </summary>
        private void btnStartImport_Click(object sender, RoutedEventArgs e)
        {
            //start mapping process
            startImport = true;

            Close();
        }

        /// <summary>
        /// clear log on closing window
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            guiLog.clearLog();
        }
    }


    /// <summary>
    /// class to convert integer values of tab index to file type enumeration
    /// </summary>
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  System.Globalization.CultureInfo culture)
        {
            return (BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType)value;
        }
    }
}
