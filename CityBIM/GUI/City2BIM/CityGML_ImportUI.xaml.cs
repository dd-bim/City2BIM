using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace CityBIM.GUI.City2BIM
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
        public CityBIM.Builder.CityGMLImportSettings ImportSettings { get { return importSettings; } }
        private CityBIM.Builder.CityGMLImportSettings importSettings { get; set; }

        public CityGML_ImportUI()
        {
            InitializeComponent();
            CityGML_ImportUI_Loaded();
        }

        private void CityGML_ImportUI_Loaded()
        {
            
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

        private void import_Btn_Click(object sender, RoutedEventArgs e)
        {
            CityBIM.Builder.CoordOrder co = CityBIM.Builder.CoordOrder.ENH;

            CityBIM.Builder.CitySource cs = CityBIM.Builder.CitySource.File;

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

            CityBIM.Builder.CityGeometry cg = Builder.CityGeometry.Solid;
            
            if (faces_radio.IsChecked == true)
            {
                cg = Builder.CityGeometry.Faces;
            }

            bool saveServerResponse = false;
            
            var importSettings = new CityBIM.Builder.CityGMLImportSettings();
            importSettings.CoordOrder = co;
            importSettings.ImportSource = cs;
            importSettings.CodeTranslate = cl;
            importSettings.FilePath = filePath_Box.Text;
            importSettings.ImportGeomType = cg;
            
            
            importSettings.saveResponse = saveServerResponse;

            this.startImport = true;
            this.importSettings = importSettings;

            this.Close();
        }


    }
}
