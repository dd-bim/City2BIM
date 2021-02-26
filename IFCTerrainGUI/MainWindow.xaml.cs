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

using IFCTerrainGUI.GUI.MainWindowLogic; //used to outsource auxiliary functions

//integrate logic from BIMGISInteropsLibs 
using BIMGISInteropLibs.IfcTerrain; //used for JsonSettings, ...

using System.IO; //used for file handling (e.g. open directory)


namespace IFCTerrainGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// initialize main gui
        /// </summary>
        public MainWindow()
        {
            //all components of the GUI are created (never remove)
            InitializeComponent();
        }

        /// <summary>
        /// create an instance for Json Settings (getter + setter)
        /// mainly used to create interaction between command / GUI and readers & writers
        /// </summary>
        public static JsonSettings jSettings { get; set; } = new JsonSettings();

        private void tbDocumentation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //direct Link to GITHUB - Repro so it should be accessable for "all"
            string docuPath = "https://github.com/dd-bim/IfcTerrain/blob/master/README.md";
            //opens link
            System.Diagnostics.Process.Start(docuPath);
        }
    }
}
