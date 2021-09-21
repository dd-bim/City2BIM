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
using System.Windows.Shapes;

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace IFCTerrainGUI.GUI.ExportSettings.Metadata
{
    /// <summary>
    /// Interaktionslogik für metadata_din_spec_91391_2.xaml
    /// </summary>
    public partial class metadata_din_spec_91391_2 : Window
    {
        public metadata_din_spec_91391_2()
        {
            InitializeComponent();

            //create guid
            Guid guid = Guid.NewGuid();
            //#1 set guid
            init.config91391.id = guid;
            tbUUID.Text = guid.ToString();
        }

        private void btnApplyMetadata913912_Click(object sender, RoutedEventArgs e)
        {
            //#2 set type
            init.config91391.type = "DTM";

            //#3 set description
            init.config91391.description = this.tbDescription.Text;

            //#4 set creation date (set always the current day)
            init.config91391.created = DateTime.Now.ToShortDateString();

            //#5 set creator
            init.config91391.creator = this.tbCreator.Text;

            //#6 set revision
            init.config91391.revision = this.tbRevision.Text;

            //#6 set version
            init.config91391.version = this.tbVersion.Text;

            //#7 ser project id
            init.config91391.projectId = this.tbProjectId.Text;

            //#8 meta scheme
            init.config91391.metadataSchema = this.tbMetaShema.Text;

            //gui logging (user information)
            guiLog.setLog(BIMGISInteropLibs.Logging.LogType.info, "Metadata DIN 91391-2 adopted!");

            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;

            

            //close current window
            Close();
        }


        /// <summary>
        /// function 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
            
            //TODO error handler
        }
    }
}
