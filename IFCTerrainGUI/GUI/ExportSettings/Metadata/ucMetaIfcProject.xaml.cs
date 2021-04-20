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

using IFCTerrainGUI.GUI.MainWindowLogic; //include for error handling (ready checker)

//logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace IFCTerrainGUI.GUI.ExportSettings.Metadata
{
    /// <summary>
    /// Interaction logic for ucMetaIfcProject.xaml
    /// </summary>
    public partial class ucMetaIfcProject : UserControl
    {
        public ucMetaIfcProject()
        {
            InitializeComponent();
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

            //site name
            MainWindow.jSettings.siteName = tbSiteName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.siteName))
            {
                MainWindow.jSettings.siteName = "Site with Terrain";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Site name set to: " + MainWindow.jSettings.projectName));

            //project name
            MainWindow.jSettings.projectName = tbProjectName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.projectName))
            {
                MainWindow.jSettings.projectName = "Project Titel [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Project name set to: " + MainWindow.jSettings.projectName));

            //organisation name
            MainWindow.jSettings.editorsOrganisationName = tbOrganisationName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.editorsOrganisationName))
            {
                MainWindow.jSettings.editorsOrganisationName = "Organisation [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Organisation name set to: " + MainWindow.jSettings.editorsOrganisationName));

            //given name
            MainWindow.jSettings.editorsGivenName = tbGivenName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.editorsGivenName))
            {
                MainWindow.jSettings.editorsGivenName = "Given name [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Given name set to: " + MainWindow.jSettings.editorsGivenName));
            
            //family name
            MainWindow.jSettings.editorsFamilyName = tbFamilyName.Text.ToString();
            if (string.IsNullOrEmpty(MainWindow.jSettings.editorsFamilyName))
            {
                MainWindow.jSettings.editorsFamilyName = "Family name [Placeholder]";
                
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Family name set to: " + MainWindow.jSettings.editorsFamilyName));

            //set task (file opening) to true
            MainWindowBib.selectMetadata = true;

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] Metadata - IfcProject set."));
            MainWindowBib.setGuiLog("Metadata - IfcProject set.");

            //check if all task are allready done
            MainWindowBib.readyState();
            return;
        }
    }
}
