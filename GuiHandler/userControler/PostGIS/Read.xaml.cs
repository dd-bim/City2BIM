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

namespace GuiHandler.userControler.PostGIS
{
    /// <summary>
    /// Interaktionslogik für Read.xaml
    /// </summary>
    public partial class Read : UserControl
    {
        public Read()
        {
            InitializeComponent();
        }

        /// <summary>
        /// check if it is possible to query to database
        /// </summary>
        private void btnCheckConnection_Click(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            BIMGISInteropLibs.PostGIS.ReaderTerrain.connectDB(config, out Npgsql.NpgsqlConnection conn);
            
            if (conn != null)
            {
                try
                {
                    conn.Open();
                    tbConRes.Text = "Valid";
                    tbConRes.Foreground = Brushes.DarkGreen;
                    GuiSupport.setLog(BIMGISInteropLibs.Logging.LogType.info, "DB connection is valid!");
                    conn.Close();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
           
            tbConRes.Text = "Invalid";
            tbConRes.Foreground = Brushes.OrangeRed;
            GuiSupport.setLog(BIMGISInteropLibs.Logging.LogType.warning, "DB connection failed!");

        }

        /// <summary>
        ///  show 'current' query string
        /// </summary>
        private void btnShowQuery_Click(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            string query = "SELECT " + "ST_AsEWKT(" + config.tin_column + ") " +
                "as wkt FROM " + config.schema + "." + config.tin_table +
                " WHERE " + config.tinid_column + " = " + "'" + config.tin_id + "'";

            if (config.breakline.GetValueOrDefault())
            {
                query = 
                "SELECT " + config.breakline_table + "." + config.breakline_column
                + " FROM " + config.schema + "." + config.breakline_table
                + " JOIN " + config.schema + "." + config.tin_table
                + " ON (" + config.breakline_table + "." + config.breakline_tin_id
                + " = " + config.tin_table + "." + config.tinid_column
                + ") WHERE " + config.tin_table + "." + config.tinid_column
                + " = " + "'" + config.tin_id + "'";
            }
            
            tbQueryTest.Text = query;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;
            if (config.fileType.Equals(BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.PostGIS))
            {
                config.filePath = "Placeholder POSTGIS";
            }
        }
    }
}
