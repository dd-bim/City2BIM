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

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaktionslogik für ucMetadata.xaml
    /// </summary>
    public partial class ucMetadata : UserControl
    {
        public ucMetadata()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Enable text boxes
        /// </summary>
        private void chkMetadata_Checked(object sender, RoutedEventArgs e)
        {
            grdProjectSettings.IsEnabled = true;
        }

        /// <summary>
        /// Lock text boxes
        /// </summary>
        private void chkMetadata_Unchecked(object sender, RoutedEventArgs e)
        {
            grdProjectSettings.IsEnabled = false;
        }

        /// <summary>
        /// set metadata in json settings
        /// </summary>
        private void btnMetadataApply_Click(object sender, RoutedEventArgs e)
        {
            /*
             * Procedure: 
             * #1 first take the TextBox contents and set it to the JSON setting.
             * #2 check if they are still empty (is also the case if TextBox was left empty)
             * #3 if #2 is the case: set default values (placeholder)
             */

            //project name
            MainWindow.jSettings.projectName = tbProjectName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.projectName)) 
            {
                MainWindow.jSettings.projectName = "Project Titel [Placeholder]";
            }

            //organisation name
            MainWindow.jSettings.editorsOrganisationName = tbOrganisationName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.editorsOrganisationName))
            {
                MainWindow.jSettings.editorsOrganisationName = "Organisation [Placeholder]";
            }

            //given name
            MainWindow.jSettings.editorsGivenName = tbGivenName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.editorsGivenName))
            {
                MainWindow.jSettings.editorsGivenName = "Given name [Placeholder]";
            }

            //family name
            MainWindow.jSettings.editorsFamilyName = tbFamilyName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.editorsFamilyName))
            {
                MainWindow.jSettings.editorsFamilyName = "Family name [Placeholder]";
            }
            return;
        }        
    }
}
