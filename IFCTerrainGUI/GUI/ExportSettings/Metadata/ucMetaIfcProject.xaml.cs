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

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

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
            init.config.siteName = tbSiteName.Text.ToString();
            if (string.IsNullOrEmpty(init.config.siteName))
            {
                init.config.siteName = "Terrain";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Site name set to: " + init.config.projectName));

            //project name
            init.config.projectName = tbProjectName.Text.ToString();
            if (string.IsNullOrEmpty(init.config.projectName))
            {
                init.config.projectName = "Project Titel [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Project name set to: " + init.config.projectName));

            //organisation name
            init.config.editorsOrganisationName = tbOrganisationName.Text.ToString();
            if (string.IsNullOrEmpty(init.config.editorsOrganisationName))
            {
                init.config.editorsOrganisationName = "Organisation [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Organisation name set to: " + init.config.editorsOrganisationName));

            //given name
            init.config.editorsGivenName = tbGivenName.Text.ToString();
            if (string.IsNullOrEmpty(init.config.editorsGivenName))
            {
                init.config.editorsGivenName = "Given name [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Given name set to: " + init.config.editorsGivenName));

            //family name
            init.config.editorsFamilyName = tbFamilyName.Text.ToString();
            if (string.IsNullOrEmpty(init.config.editorsFamilyName))
            {
                init.config.editorsFamilyName = "Family name [Placeholder]";
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Metadata: Family name set to: " + init.config.editorsFamilyName));

            //set task (file opening) to true
            GuiHandler.GuiSupport.selectMetadata = true;

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] Metadata - IfcProject set."));
            guiLog.setLog("Metadata - IfcProject set.");

            //check if all task are allready done
            MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState());
            return;
        }
    }
}
