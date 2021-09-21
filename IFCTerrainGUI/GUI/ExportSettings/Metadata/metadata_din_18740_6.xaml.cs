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
    /// Interaktionslogik für metadata_din_18740_6.xaml
    /// </summary>
    public partial class metadata_din_18740_6 : Window
    {
        public metadata_din_18740_6()
        {
            InitializeComponent();
        }

        /// <summary>
        /// set input to json settings <para/>
        /// close window and enable main window
        /// </summary>
        private void btnApplyMetadata187406_Click(object sender, RoutedEventArgs e)
        {
            //set json settings (DIN 18740 - 6)
            //#1 - model type 
            init.config18740.modelType = this.selectModelType.SelectionBoxItem.ToString();

            //#2 - data structure
            init.config18740.dataStructure = this.selectDataStructure.SelectionBoxItem.ToString();

            //#3 topicality
            if(this.entryDate.SelectedDate == null)
            {
                init.config18740.topicality = DateTime.Now.ToShortDateString();
            }
            else
            {
                init.config18740.topicality = this.entryDate.SelectedDate.Value.ToShortDateString();
            }

            //#4 position reference system
            init.config18740.positionReferenceSystem = this.selectPosRef.SelectionBoxItem.ToString() + "; " + this.tbInputPosRef.Text.ToString();

            //#5 altitude reference system
            if (itemAltitudeUser.IsSelected)
            {
                //only output user input
                init.config18740.altitudeReferenceSystem = this.tbInputAltitude.Text.ToString();
            }
            else
            {
                init.config18740.altitudeReferenceSystem = this.selectAltitudeRef.SelectionBoxItem.ToString() + "; " + this.tbInputAltitude.Text.ToString();
            }

            //#6 projection
            if (itemProjectUser.IsSelected)
            {
                //only output user input
                init.config18740.projection = this.tbInputProjection.Text.ToString();
            }
            else
            {
                init.config18740.projection = this.selectProjection.SelectionBoxItem.ToString() + "; " + this.tbInputProjection.Text.ToString();
            }

            //#7 deviation
            init.config18740.deviationPosition = this.tbDeviationPosition.Text.ToString();
            init.config18740.deviationAltitude = this.tbDeviationAltitude.Text.ToString();

            //gui logging (user information)
            guiLog.setLog(BIMGISInteropLibs.Logging.LogType.info, "Metadata DIN 18740-6 adopted!");

            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;

            //close current window
            Close();
        }

        /// <summary>
        /// error handler - select projction
        /// </summary>
        private void selectProjection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if user input was selected
            if (itemProjectUser.IsSelected)
            {
                //enable textbox
                tbInputProjection.IsEnabled = true;
            }
            //otherwise
            else
            {
                //disable textbox
                tbInputProjection.IsEnabled = false;
            }
        }

        /// <summary>
        ///  error handler - select altitude
        /// </summary>
        private void selectAltitudeRef_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if user input was selected
            if (itemAltitudeUser.IsSelected)
            {
                //enable textbox
                tbInputAltitude.IsEnabled = true;
            }
            //otherwise
            else
            {
                //disable textbox
                tbInputAltitude.IsEnabled = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;

            //TODO error handler, if input failed
        }
    }
}
