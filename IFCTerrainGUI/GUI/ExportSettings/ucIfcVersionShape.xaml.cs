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

using System.Text.RegularExpressions; //include to be able to restrict textbox entries
using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide error handling

//logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaction logic for ucIfcVersionShape.xaml
    /// </summary>
    public partial class ucIfcVersionShape : UserControl
    {
        public ucIfcVersionShape()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// is executed as soon as an IfcVersion is selected
        /// </summary>
        private void cbIfcVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region error handling
            //Apply constraints for IFC2x3
            if (this.ifc2x3.IsSelected)
            {
                //disable Ifc Geo Element (not supported in IFC2x3)
                this.chkIfcGeoEl.IsEnabled = false;
                this.chkIfcGeoEl.IsChecked = false;

                //disable TFS Shape Repres (not supported in IFC2x3)
                this.ifcTFS.IsEnabled = false;
                this.ifcTFS.IsSelected = false;

                //disable LoGeoRef50 tab
                ((MainWindow)Application.Current.MainWindow).tabLoGeoRef50.IsEnabled = false;

                //swtich to another tab (error handling) if LoGeoRef50 is enabled
                ((MainWindow)Application.Current.MainWindow).tabLoGeoRef30.IsSelected = true;

                //set gui log
                guiLog.setLog("LoGeoRef50 disabled.");
            }
            else if (this.ifc4.IsSelected)
            {
                //enable Ifc Geo Element + TFS (supported in IFC4)
                this.chkIfcGeoEl.IsEnabled = true;
                this.ifcTFS.IsEnabled = true;

                //enable LoGeoRef50 tab
                ((MainWindow)Application.Current.MainWindow).tabLoGeoRef50.IsEnabled = true;

                //set gui log
                guiLog.setLog("LoGeoRef50 enabled.");
            }

            //check if an item was selected
            if (cbIfcVersion.SelectedIndex != -1)
            {
                //set task to true
                GuiHandler.GuiSupport.selectIfcVersion = true;

                //logging
                LogWriter.Add(LogType.debug, "[GUI] IFC schema set.");
                guiLog.setLog("IFC schema version set.");

                //check if all required tasks are performed
                MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState());
            }

            #endregion error handling

            #region json settings
            //IfcVersion: 2x3 (using MESH)
            if (this.ifc2x3.IsSelected)
            {
                init.config.outIFCType = BIMGISInteropLibs.IFC.IfcVersion.IFC2x3;
            }
            //IfcVersion: 4 (using MESH)
            else if (this.ifc4.IsSelected)
            {
                init.config.outIFCType = BIMGISInteropLibs.IFC.IfcVersion.IFC4;
            }
            //IfcVersion 4.3 (Placeholder)
            else if (this.ifc4dot3.IsSelected)
            {
                //PLACEHOLDER (TODO if supported via xbim)
            }
            #endregion json settings
        }

        /// <summary>
        /// include regular expressions so that only numbers can be entered
        /// Note: blanks can also be entered (these must still be removed)
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            //regular expression
            Regex regex = new Regex("[^0-9]+");

            //check if input corresponds to a regular expression
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// set the SurfaceType in JSON settings
        /// </summary>
        private void cbShapeRepres_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //check which Shape Representation was selected
            //GeometricCurveSet GCS
            if (this.ifcGCS.IsSelected)
            {
                init.config.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.GCS;
            }
            //Shell Based Surface Model (SBSM)
            else if (this.ifcSBSM.IsSelected)
            {
                init.config.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.SBSM;
            }
            //Triangulated Face Set (TFS)
            else if (this.ifcTFS.IsSelected)
            {
                init.config.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.TFS;
            }
            //Triangulated Irregular Network (TIN) [currently not supported]
            else if (this.ifcTIN.IsSelected)
            {
                init.config.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.TIN;
            }

            //check if an item was selected
            if (cbShapeRepres.SelectedIndex != -1)
            {
                //set task to true
                GuiHandler.GuiSupport.selectIfcShape = true;

                //logging
                LogWriter.Add(LogType.debug, "[GUI] IFC shape representation set.");
                guiLog.setLog("IFC shape representation set.");

                //check if all required tasks are performed
                MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState());
            }
        }

        /// <summary>
        /// execute ifcGeoElement was selected (apply JSON settings)
        /// </summary>
        private void chkIfcGeoEl_Checked(object sender, RoutedEventArgs e)
        {
            //bool to tro --> export ifc geo element
            init.config.geoElement = true;
        }

        /// <summary>
        /// execute ifcGeoElement was uncheked (apply JSON settings)
        /// </summary>
        private void chkIfcGeoEl_Unchecked(object sender, RoutedEventArgs e)
        {
            //bool to false --> no ifc geo element
            init.config.geoElement = false;
        }
    }
}
