using System.Windows;

namespace City2BIM.RevitCommands.City2BIM
{
    /// <summary>
    /// Interaction logic for CityGML_settings.xaml
    /// </summary>
    public partial class CityGML_settings : Window
    {
        public CityGML_settings()
        {
            InitializeComponent();

            tb_lat.Text = GeoRefSettings.WgsCoord[0].ToString();
            tb_lon.Text = GeoRefSettings.WgsCoord[1].ToString();
            tb_extent.Text = City2BIM_prop.Extent.ToString();
            tb_file.Text = City2BIM_prop.FileUrl;
            tb_server.Text = City2BIM_prop.ServerUrl;
        }

        private void bt_browse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog imp = new FileDialog();
            tb_file.Text = imp.ImportPath(FileDialog.Data.CityGML);

        }

        private void bt_editURL_Click(object sender, RoutedEventArgs e)
        {
            tb_server.IsEnabled = true;
        }

        private void bt_apply_Click(object sender, RoutedEventArgs e)
        {
            //read server / file url
            City2BIM_prop.FileUrl = tb_file.Text;
            City2BIM_prop.ServerUrl = tb_server.Text;

            //set bool whether server or not
            if ((bool)rb_server.IsChecked)
            {
                City2BIM_prop.IsServerRequest = true;
                City2BIM_prop.IsGeodeticSystem = true;
            }
            else
                City2BIM_prop.IsServerRequest = false;

            //set center coordinates for request
            if ((bool)rb_site.IsChecked)
            {
                City2BIM_prop.ServerCoord[0] = GeoRefSettings.WgsCoord[1];
                City2BIM_prop.ServerCoord[1] = GeoRefSettings.WgsCoord[0];
            }
            else
            {
                bool vLat = double.TryParse(tb_lat.Text, out var lat);
                bool vLon = double.TryParse(tb_lon.Text, out var lon);

                if (vLat && vLon)
                {
                    City2BIM_prop.ServerCoord[1] = lat;
                    City2BIM_prop.ServerCoord[0] = lon;
                }
            }

            //set extent for request
            bool ext = double.TryParse(tb_extent.Text, out var extSize);
            if (ext)
                 City2BIM_prop.Extent = extSize;

            //set coordinates order for file import
            if ((bool)rb_YXZ.IsChecked)
                City2BIM_prop.IsGeodeticSystem = true;
            else
                City2BIM_prop.IsGeodeticSystem = false;


            this.Close();
        }

        private void rb_custom_Checked(object sender, RoutedEventArgs e)
        {
            tb_lat.IsEnabled = true;
            tb_lon.IsEnabled = true;
        }
    }
}
