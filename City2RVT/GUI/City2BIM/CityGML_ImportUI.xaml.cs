using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace City2RVT.GUI.City2BIM
{
    /// <summary>
    /// Interaction logic for CityGML_ImportUI.xaml
    /// </summary>
    public partial class CityGML_ImportUI : Window
    {
        public bool StartImport { get { return startImport; } }
        private bool startImport { set; get; }

        /*
        private bool fromFile { get; set; }
        public bool FromFile { get { return fromFile; } }

        private bool fromServer { get; set;}
        public bool FromServer { get { return fromServer; } }

        private string filePath { get; set; }
        public string FilePath { get { return filePath; } }

        public CoordOder CoordOder { get { return coordOder; } }
        private CoordOder coordOder { get; set; }

        private string serverURL { get; set; }
        public string ServerURL { get { return serverURL; } }
        */
        public City2RVT.Builder.CityGMLImportSettings ImportSettings { get { return importSettings; } }
        private City2RVT.Builder.CityGMLImportSettings importSettings { get; set; }

        public CityGML_ImportUI()
        {
            InitializeComponent();
            CityGML_ImportUI_Loaded();

            Lat_Box.Text = City2RVT.GUI.Prop_GeoRefSettings.WgsCoord[0].ToString();
            Long_Box.Text = City2RVT.GUI.Prop_GeoRefSettings.WgsCoord[1].ToString();
        }

        private void CityGML_ImportUI_Loaded()
        {
            srcFile.IsChecked = true;
            siteLocation.IsChecked = true;
        }

        private void src_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb.Name.Equals("srcFile"))
            {
                gB_File.IsEnabled = true;
                gB_Server.IsEnabled = false;
            }
            else
            {
                gB_File.IsEnabled = false;
                gB_Server.IsEnabled = true;
            }
        }

        private void cancelBtn_click(object sender, RoutedEventArgs e)
        {
            this.startImport = false;
            this.Close();
        }

        private void fileBrowse_Btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML/GML files (*.xml, *.gml)|*.xml; *.gml|All files (*.*)|*.*";


            if (openFileDialog.ShowDialog() == true)
            {
                filePath_Box.Text = openFileDialog.FileName;
            }
        }

        private void responseFolder_Btn_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.ShowDialog();
            ResponsePath.Text = folderDialog.SelectedPath;

        }

        private void editURL_click(object sender, RoutedEventArgs e)
        {
            serverURL_Box.IsReadOnly = false;
        }

        private void center_checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb.Name.Equals("siteLocation"))
            {
                Lat_Box.Text = City2RVT.GUI.Prop_GeoRefSettings.WgsCoord[0].ToString();
                Long_Box.Text = City2RVT.GUI.Prop_GeoRefSettings.WgsCoord[1].ToString();
                Lat_Box.IsReadOnly = true;
                Long_Box.IsReadOnly = true;
            }
            else
            {
                Lat_Box.IsReadOnly = false;
                Long_Box.IsReadOnly = false;
            }
        }

        private void import_Btn_Click(object sender, RoutedEventArgs e)
        {
            City2RVT.Builder.CoordOrder co = City2RVT.Builder.CoordOrder.ENH;

            if (NEHOrder_Radio.IsChecked == true)
            {
                co = City2RVT.Builder.CoordOrder.NEH;
            }

            City2RVT.Builder.CitySource cs = City2RVT.Builder.CitySource.File;

            if (srcServer.IsChecked == true)
            {
                cs = City2RVT.Builder.CitySource.Server;
            }

            BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist cl;

            if (codelist_check.IsChecked == true)
            {
                ComboBoxItem item = (ComboBoxItem)code_comboBox.SelectedItem;

                if (item != null)
                {
                    string value = item.Content.ToString();

                    switch (value)
                    {
                        case "":
                            cl = BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist.none;
                            break;

                        case "AdV":
                            cl = BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist.adv;
                            break;

                        case "SIG3D":
                            cl = BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist.sig3d;
                            break;

                        default:
                            cl = BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist.none;
                            break;
                    }
                }
                else
                {
                    cl = BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist.none;
                }
            }
            else
            {
                cl = BIMGISInteropLibs.CityGML.CityGml_Codelist.Codelist.none;
            }

            City2RVT.Builder.CityGeometry cg = Builder.CityGeometry.Solid;
            
            if (faces_radio.IsChecked == true)
            {
                cg = Builder.CityGeometry.Faces;
            }

            bool saveServerResponse = false;
            if (response_Box.IsChecked == true)
            {
                saveServerResponse = true;
            }

            var importSettings = new City2RVT.Builder.CityGMLImportSettings();
            importSettings.CoordOrder = co;
            importSettings.ImportSource = cs;
            importSettings.CodeTranslate = cl;
            importSettings.FilePath = filePath_Box.Text;
            importSettings.ImportGeomType = cg;
            importSettings.serverURL = serverURL_Box.Text;
            importSettings.FolderPath = ResponsePath.Text;
            importSettings.CenterCoords = new double[] { Double.Parse(Lat_Box.Text), Double.Parse(Long_Box.Text) };
            importSettings.saveResponse = saveServerResponse;
            importSettings.Extent = Double.Parse(extent_Box.Text);

            this.startImport = true;
            this.importSettings = importSettings;

            this.Close();
        }


    }
}
