using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32; //File Dialog

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

using BIMGISInteropLibs.IfcTerrain;
namespace GuiHandler.userControler.GeoJSON
{
    /// <summary>
    /// Interaktionslogik für Read.xaml
    /// </summary>
    public partial class Read : UserControl
    {
        public Read()
        {
            InitializeComponent();

            //bind conversion types
            cbGeomType.ItemsSource = new string[]
            {
                GeometryType.MultiPoint.ToString(),
                GeometryType.MultiPolygon.ToString(),
                GeometryType.GeometryCollection.ToString()
            };

            cbGeomTypeBreakline.ItemsSource = new string[]
            {
                GeometryType.MultiLineString.ToString(),
                GeometryType.MultiPolygon.ToString(),
                GeometryType.FeatureCollection.ToString()
            };
        }
        /// <summary>
        /// Event to handle file dialog and set file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadGeoJson_Click(object sender, RoutedEventArgs e)
        {
            //create new file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            //set filter
            ofd.Filter = "GeoJSON files *.GeoJSON|*.geojson|Json *.json|*.json";

            //task to run when file has been selected
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                //set file path
                init.config.filePath = ofd.FileName;

                //set file name
                init.config.fileName = Path.GetFileNameWithoutExtension(ofd.FileName);

                //set file type
                init.config.fileType = IfcTerrainFileType.GeoJSON;

                cbGeomType.IsEnabled = true;
            }
        }

        private void btnProcessGeoJson_Click(object sender, RoutedEventArgs e)
        {
            //set file type to geojson
            init.config.fileType = IfcTerrainFileType.GeoJSON;
            
            if(init.config.geometryType == GeometryType.MultiPoint)
            {
                init.config.readPoints = true;
            }
            else{
                init.config.readPoints = false;
            }

            //breakline processing
            if (chkBreakline.IsChecked.GetValueOrDefault())
            {
                init.config.breakline = true;
            }
            else
            {
                init.config.breakline = false;
            }

            //
            guiLog.taskfileOpening = true;
            
            //
            guiLog.rdyDTM2BIM();
            
            //
            guiLog.setLog("GeoJSON settings applyed.");

            //
            guiLog.fileReaded();
            
            //
            guiLog.readyState();
            
        }

        private void cbGeomType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbGeomType.SelectedIndex != -1)
            {
                init.config.geometryType = (GeometryType)Enum.Parse(typeof(GeometryType), cbGeomType.SelectedItem.ToString());
                //
                guiLog.setLog("Geometry type set to: " + init.config.geometryType);

                btnProcessGeoJson.IsEnabled = true;
            }
        }

        private void cbGeomTypeBreakline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cbGeomTypeBreakline.SelectedIndex != -1)
            {
                init.config.breaklineGeometryType = (GeometryType)Enum.Parse(typeof(GeometryType), cbGeomTypeBreakline.SelectedItem.ToString());
                guiLog.setLog("Breakline geometry type set to: " + init.config.breaklineGeometryType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void btnOpenBreaklineFile_Click(object sender, RoutedEventArgs e)
        {
            //create new file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            //set filter
            ofd.Filter = "GeoJSON files *.GeoJSON|*.geojson|Json *.json|*.json";

            //task to run when file has been selected
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                //set file path
                init.config.breaklineFile = ofd.FileName;

                cbGeomTypeBreakline.IsEnabled = true;
            }
        }
    }
}
