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

using Microsoft.Win32; //namespace for file handling (FileDialog)

namespace IFCTerrainGUI.GUI
{
    /// <summary>
    /// Interaktionslogik für ucTin.xaml
    /// </summary>
    public partial class ucTin : UserControl
    {
        public ucTin()
        {
            InitializeComponent();
        }

        
        private void btnReadXml_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "LandXML *.xml|*.xml|CityGML *.gml|*.gml";
            if (ofd.ShowDialog() == true)
            {
                switch (ofd.FilterIndex)
                {
                    case 1:
                        MessageBox.Show("file read " + ofd.FileName);
                        break;
                }

            }

        }
    }
}
