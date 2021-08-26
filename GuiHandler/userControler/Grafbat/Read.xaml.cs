using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32; //used for file handling

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace GuiHandler.userControler.Grafbat
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
        
        private void btnReadGrafbat_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();

            //set filter to grafbat (out files)
            ofd.Filter = "Grafbat files *.out|*.out";

            //is performed when a file is selected
            if (ofd.ShowDialog() == true)
            {
                //set the save path of the file to be converted
                init.config.filePath = ofd.FileName;

                //set JSON settings of file name
                init.config.fileName = System.IO.Path.GetFileName(ofd.FileName);

                //set the save path of the file to be converted
                init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.Grafbat;

                //enable process button [TODO]: check if need to be relocated 
                btnProcessGraftbat.IsEnabled = true;

                //gui logging (user information)
                guiLog.setLog("File selected! --> Please make settings and confirm.");
                
                //display short information about imported file to user
                guiLog.fileImported();

                return;
            }
        }

        /// <summary>
        /// will be executed as soon as the process grafbat button is clicked
        /// mainly used to apply gui info and json setting
        /// </summary>
        private void btnProcessGraftbat_Click(object sender, RoutedEventArgs e)
        {
            bool localCheck = true;

            #region set config
            if (!rbGraftbatReadFaces.IsChecked.GetValueOrDefault())
            {
                //set true otherwise would be false (by default)
                init.config.readPoints = true;

                //check if required
                if (chkGrafbatFilteringPointtypes.IsChecked.GetValueOrDefault()
                    && !string.IsNullOrEmpty(tbPointTypes.Text))
                {
                    //set filtering to config
                    init.config.layer = tbPointTypes.Text;
                    guiLog.setLog("Pointtype(s): '" + tbPointTypes.Text + "' will be used for processing");
                }
                else
                {
                    guiLog.setLog("Pointtype has not been applyed. All point types will be user for conversion!");
                }
            }
            else
            {
                //
                if (!rbGrafbatAllHorizons.IsChecked.GetValueOrDefault()
                    && !string.IsNullOrEmpty(tbGraftbatFilteringHorizon.Text))
                {
                    //TODO do via textbox
                    if (!int.TryParse(tbGraftbatFilteringHorizon.Text, out int hor))
                    {
                        guiLog.setLog("Horizon can't be parsed to integer value. Please check input!");
                        init.config.onlyHorizon = false;
                        localCheck = false;
                    }
                    else
                    {
                        //set to horizon
                        init.config.onlyHorizon = true;

                        //set horizon
                        init.config.horizon = hor;
                        guiLog.setLog("Horizon '" + hor + "' has been set");
                    }
                }
            }

            if (rbGraftbatBreaklinesNo.IsChecked.GetValueOrDefault())
            {
                guiLog.setLog("Breaklines will not processed!");
            }
            else
            {
                init.config.breakline = true;

                if (!string.IsNullOrEmpty(tbGraftbatBreaklinesInput.Text))
                {
                    init.config.breakline_layer = tbGraftbatBreaklinesInput.Text;
                    guiLog.setLog("Breaklines of type: '" + tbGraftbatBreaklinesInput.Text + "' will be processed!");
                }
                else
                {
                    guiLog.setLog("Please input breakline types!");
                    localCheck = false;
                }
            }

            if (localCheck)
            {
                #region error handling
                //set task (file opening) to true
                guiLog.taskfileOpening = true;

                //[IfcTerrain] check if all task are allready done
                guiLog.readyState();

                //[DTM2BIM] check if all task are allready done
                guiLog.rdyDTM2BIM();

                //gui logging (user information)
                guiLog.setLog("Grafbat settings applyed.");

                //check if all task are allready done
                guiLog.readyState();
                #endregion error handling
            }
            else
            {
                guiLog.setLog("Error in settings. Please check!");
            }





            #endregion set config

            #region gui info panel
            // storage location
            //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, init.config.filePath);

            //file tpye
            //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileType, init.config.fileType.ToString());
            #endregion gui info panel




        }

        private void rbGraftbatReadFaces_Unchecked(object sender, RoutedEventArgs e)
        {
            gbHorizon.IsEnabled = false;
            gbPointtypes.IsEnabled = true;
        }
        
        private void rbGrafbatHorizonSelect_Checked(object sender, RoutedEventArgs e)
        {
            tbGraftbatFilteringHorizon.IsEnabled = true;
        }

        private void rbGrafbatHorizonSelect_Unchecked(object sender, RoutedEventArgs e)
        {
            tbGraftbatFilteringHorizon.IsEnabled = false;
        }

        private void rbGraftbatBreaklinesYes_Checked(object sender, RoutedEventArgs e)
        {
            tbGraftbatBreaklinesInput.IsEnabled = true;
        }

        private void rbGraftbatBreaklinesYes_Unchecked(object sender, RoutedEventArgs e)
        {
            tbGraftbatBreaklinesInput.IsEnabled = false;
        }

        private void chkGrafbatFilteringPointtypes_Checked(object sender, RoutedEventArgs e)
        {
            tbPointTypes.IsEnabled = true;
        }

        private void chkGrafbatFilteringPointtypes_Unchecked(object sender, RoutedEventArgs e)
        {
            tbPointTypes.IsEnabled = false;
        }

        private void rbGraftbatReadPoints_Checked(object sender, RoutedEventArgs e)
        {
            gbHorizon.IsEnabled = false;
            gbPointtypes.IsEnabled = true;
        }

        private void rbGraftbatReadPoints_Unchecked(object sender, RoutedEventArgs e)
        {
            gbHorizon.IsEnabled = true;
            gbPointtypes.IsEnabled = false;
        }
    }
}