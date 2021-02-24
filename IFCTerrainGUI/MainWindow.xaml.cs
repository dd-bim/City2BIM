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



//integrate logic from BIMGISInteropsLibs 
using BIMGISInteropLibs.IfcTerrain; //used for JsonSettings, ...

namespace IFCTerrainGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// initialize main gui
        /// </summary>
        public MainWindow()
        {
            //all components of the GUI are created (never remove)
            InitializeComponent();
        }

        /// <summary>
        /// create an instance for Json Settings (getter + setter)
        /// mainly used to create interaction between command / GUI and readers & writers
        /// </summary>
        public static JsonSettings jSettings { get; set; } = new JsonSettings();

        /// <summary>
        /// Function to assign text to a textbox;
        /// Note: this scrolls to the end of the text box so that the end of the file path is displayed
        /// </summary>
        /// <param name="tbName">Text box identifier</param>
        /// <param name="tbText">Text which should be used</param>
        public static void setTextBoxText(TextBox tbName, string tbText)
        {
            tbName.Text = tbText;
            tbName.CaretIndex = tbText.Length;
            var rect = tbName.GetRectFromCharacterIndex(tbName.CaretIndex);
            tbName.ScrollToHorizontalOffset(rect.Right);
        }


        /// <summary>
        /// only dev test button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            setTextBoxText(tbFilePathInput, jSettings.filePath);
        }

        /// <summary>
        /// take over the properties, from the read XML file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcessXml_Click(object sender, RoutedEventArgs e)
        {
            setTextBoxText(tbFilePathInput, jSettings.filePath);
            setTextBoxText(tbFileTypeInput, jSettings.fileType.ToString());
            setTextBoxText(tbDtmLayerInput, "will not be read out at " + jSettings.fileType.ToString());
            setTextBoxText(tbGridSizeInput, "will not be read out at " + jSettings.fileType.ToString());

        }
    }
}
