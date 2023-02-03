using BIMGISInteropLibs;
using BIMGISInteropLibs.CityGML;
using System;
using System.Globalization;
using System.Windows;

namespace CityBIM.GUI
{
    /// <summary>
    /// Interaction logic for CityGML_settings.xaml
    /// </summary>
    public partial class Wpf_CityGML_settings : Window
    {
        private readonly string[] codelistTypes = new string[] { "AdV (Arbeitsgemeinschaft der Vermessungsverwaltungen der Länder der BRD)", 
                                                        "SIG3D (Special Interest Group 3D)" };
        public Wpf_CityGML_settings()
        {
            InitializeComponent();

            tb_lat.Text = Prop_GeoRefSettings.WgsCoord[0].ToString();
            tb_lon.Text = Prop_GeoRefSettings.WgsCoord[1].ToString();
            tb_extent.Text = Prop_CityGML_settings.Extent.ToString();
            tb_file.Text = Prop_CityGML_settings.FileUrl;
            tb_server.Text = Prop_CityGML_settings.ServerUrl;

            if (Prop_CityGML_settings.IsServerRequest)
                rb_server.IsChecked = true;
            else
                rb_file.IsChecked = true;

            if (Prop_CityGML_settings.IsGeodeticSystem)
                rb_YXZ.IsChecked = true;
            else
                rb_XYZ.IsChecked = true;

            foreach (var item in codelistTypes)
            {
                cb_Codelist.Items.Add(item);
            }

        }

        private void Bt_browse_Click(object sender, RoutedEventArgs e)
        {
            Reader.FileDialog imp = new Reader.FileDialog();
            tb_file.Text = imp.ImportPath(Reader.FileDialog.Data.CityGML);

        }

        private void Bt_editURL_Click(object sender, RoutedEventArgs e)
        {
            tb_server.IsEnabled = true;
        }

        private void Bt_apply_Click(object sender, RoutedEventArgs e)
        {
            //read server / file url
           Prop_CityGML_settings.FileUrl = tb_file.Text;
           Prop_CityGML_settings.ServerUrl = tb_server.Text;

            //set bool whether server or not
            if ((bool)rb_server.IsChecked)
            {
               Prop_CityGML_settings.IsServerRequest = true;
               Prop_CityGML_settings.IsGeodeticSystem = true;
            }
            else
               Prop_CityGML_settings.IsServerRequest = false;

            //set center coordinates for request
            if ((bool)rb_site.IsChecked)
            {
                Prop_CityGML_settings.ServerCoord[0] = 1.0;
                var ab = Prop_GeoRefSettings.WgsCoord[1];

                Prop_CityGML_settings.ServerCoord[0] = Prop_GeoRefSettings.WgsCoord[1];
               Prop_CityGML_settings.ServerCoord[1] = Prop_GeoRefSettings.WgsCoord[0];
            }
            else
            {
                bool vLat = double.TryParse(tb_lat.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat);
                bool vLon = double.TryParse(tb_lon.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon);

                if (vLat && vLon)
                {
                   Prop_CityGML_settings.ServerCoord[1] = lat;
                   Prop_CityGML_settings.ServerCoord[0] = lon;
                }
            }

            //set extent for request
            bool ext = double.TryParse(tb_extent.Text, out var extSize);
            if (ext)
                Prop_CityGML_settings.Extent = extSize;

            //set coordinates order for file import
            if ((bool)rb_YXZ.IsChecked)
               Prop_CityGML_settings.IsGeodeticSystem = true;
            else
               Prop_CityGML_settings.IsGeodeticSystem = false;

            if (check_applyCode.IsChecked == false)
               Prop_CityGML_settings.CodelistName = CityGml_Codelist.Codelist.none;
            else
            {
                var item = cb_Codelist.SelectedItem;

                if (item.ToString().Equals("AdV (Arbeitsgemeinschaft der Vermessungsverwaltungen der Länder der BRD)"))
                   Prop_CityGML_settings.CodelistName = CityGml_Codelist.Codelist.adv;
                else if (item.ToString().Equals("SIG3D (Special Interest Group 3D)"))
                   Prop_CityGML_settings.CodelistName = CityGml_Codelist.Codelist.sig3d;
                else
                   Prop_CityGML_settings.CodelistName = CityGml_Codelist.Codelist.none;
            }

            if (check_saveResponse.IsChecked == true)
               Prop_CityGML_settings.SaveServerResponse = true;
            else
               Prop_CityGML_settings.SaveServerResponse = false;

            this.Close();
        }

        private void Rb_custom_Checked(object sender, RoutedEventArgs e)
        {
            tb_lat.IsEnabled = true;
            tb_lon.IsEnabled = true;
        }

        private void Bt_saveResponse_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                //Log.Information("Start of changing directory. Dialogue opened.");

                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                fbd.Description = "Select folder for CityGML file";

                fbd.ShowNewFolderButton = true;

                var result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                  Prop_CityGML_settings.PathResponse = fbd.SelectedPath;
                }
            }
        }
    }
}
