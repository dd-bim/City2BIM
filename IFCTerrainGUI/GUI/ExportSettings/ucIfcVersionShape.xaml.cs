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

using System.Text.RegularExpressions;

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

            //MainWindow.jSettings.outFileType =
        }
        
        /// <summary>
        /// is executed as soon as an IfcVersion is selected
        /// </summary>
        private void cbIfcVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region error handling
            //Apply constraints for IFC2x3
            if ((this.ifc2x3.IsSelected) | (this.ifc2x3Tin.IsSelected))
            {
                //disable Ifc Geo Element (not supported in IFC2x3)
                this.chkIfcGeoEl.IsEnabled = false;
                this.chkIfcGeoEl.IsChecked = false;

                //disable TFS Shape Repres (not supported in IFC2x3)
                this.ifcTFS.IsEnabled = false;
                this.ifcTFS.IsSelected = false;
            }
            else if ((this.ifc4.IsSelected) | (this.ifc4Tin.IsSelected))
            {
                //enable Ifc Geo Element + TFS (supported in IFC4)
                this.chkIfcGeoEl.IsEnabled = true;
                this.ifcTFS.IsEnabled = true;
            }
            #endregion error handling

            #region json settings
            //IfcVersion: 2x3 (using MESH)
            if (this.ifc2x3.IsSelected)
            {
                MainWindow.jSettings.outIFCType = BIMGISInteropLibs.IFC.IfcVersion.IFC2x3;
            }
            //IfcVersion: 4 (using MESH)
            else if (this.ifc4.IsSelected)
            {
                MainWindow.jSettings.outIFCType = BIMGISInteropLibs.IFC.IfcVersion.IFC4;
            }
            //IfcVersion 2x3 (using TIN) --> will replace 2x3
            else if (this.ifc2x3Tin.IsSelected)
            {
                MainWindow.jSettings.outIFCType = BIMGISInteropLibs.IFC.IfcVersion.IFC2x3Tin;
            }
            //IfcVersion 4 (using TIN
            else if (this.ifc4Tin.IsSelected)
            {
                MainWindow.jSettings.outIFCType = BIMGISInteropLibs.IFC.IfcVersion.IFC4Tin;
            }
            //
            else if (this.ifc4dot3.IsSelected)
            {
                //PLACEHOLDER
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
                MainWindow.jSettings.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.GCS;
            }
            //Shell Based Surface Model (SBSM)
            else if (this.ifcSBSM.IsSelected)
            {
                MainWindow.jSettings.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.SBSM;
            }
            //Triangulated Face Set (TFS)
            else if (this.ifcTFS.IsSelected)
            {
                MainWindow.jSettings.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.TFS;
            }
            //Triangulated Irregular Network (TIN) [currently not supported]
            else if (this.ifcTIN.IsSelected)
            {
                MainWindow.jSettings.surfaceType = BIMGISInteropLibs.IFC.SurfaceType.TIN;
            }
        }

        private void chkIfcGeoEl_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.jSettings.geoElement = true;
        }

        private void chkIfcGeoEl_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.jSettings.geoElement = false;
        }
    }
}
