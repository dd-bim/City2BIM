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

using System.IO; //file path
using Microsoft.Win32; //used for file handling


namespace GuiHandler.userControler.CityGML
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

        private void btnReadCityGML_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();

            //set filtering so that the following selection is possible 
            ofd.Filter = "CityGML *.gml|*.gml";

            if (ofd.ShowDialog().GetValueOrDefault())
            {
                var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

                //set file path and get file name
                config.filePath = ofd.FileName;
                config.fileName = Path.GetFileName(ofd.FileName);
            }
        }
    }
}
