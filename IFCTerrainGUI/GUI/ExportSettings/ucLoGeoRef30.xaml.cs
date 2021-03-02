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

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaktionslogik für ucLoGeoRef30.xaml
    /// </summary>
    public partial class ucLoGeoRef30 : UserControl
    {
        public ucLoGeoRef30()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Changing the selection off (is initalized by default)
        /// </summary>
        private void rbLoGeoRef30Default_Checked(object sender, RoutedEventArgs e)
        {
            //in this case set to default (background: the project center will be used)
            MainWindow.jSettings.customOrigin = false;
        }

        private void rbLoGeoRef30User_Checked(object sender, RoutedEventArgs e)
        {
            
            MainWindow.jSettings.customOrigin = true;

            
        }

        private void rbLoGeoRef30Default_Unchecked(object sender, RoutedEventArgs e)
        {
            spLoGeoRef30Values.IsEnabled = true;
            btnLoGeoRef30Apply.IsEnabled = true;
        }

        private void rbLoGeoRef30User_Unchecked(object sender, RoutedEventArgs e)
        {
            spLoGeoRef30Values.IsEnabled = false;
            btnLoGeoRef30Apply.IsEnabled = false;
        }

        /// <summary>
        /// Eingabe Überprüfung, sodass nur Zahlen eingegeben werden können!
        /// </summary>
        private void tbLoGeoRef30_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //regular expression
            Regex regex = new Regex("[^0-9][,.]+");

            //check if input corresponds to a regular expression
            e.Handled = regex.IsMatch(e.Text);
        }


        /// <summary>
        /// 
        /// </summary>
        private void btnLoGeoRef30Apply_Click(object sender, RoutedEventArgs e)
        {
            //
            MainWindow.jSettings.xOrigin = Convert.ToDouble(tbLoGeoRef30ValueX.Text);
            MainWindow.jSettings.yOrigin = Convert.ToDouble(tbLoGeoRef30ValueY.Text);
            MainWindow.jSettings.zOrigin = Convert.ToDouble(tbLoGeoRef30ValueZ.Text);
        }
    }
}
