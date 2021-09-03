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
using System.ComponentModel; //interface --> property changed
using System.Collections.ObjectModel; //observable collection

using System.Text.RegularExpressions; //include to be able to restrict textbox entries
using Microsoft.Win32; //used for file handling

//embed for file logging
using BIMGISInteropLibs.Logging;                                    //acess to logger
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;    //to set log messages

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace GuiHandler.userControler.Grid
{
    /// <summary>
    /// Interaktionslogik für Read.xaml
    /// </summary>
    public partial class Read : UserControl
    {
        public ObservableCollection<checker> checker { get; set; }

        public Read()
        {
            InitializeComponent();

            checker = new ObservableCollection<checker>();

            ucGrid.DataContext = checker;

            checker.Add(new Grid.checker());                     
        }

        /// <summary>
        /// opens file handling - elevation grid
        /// </summary>
        private void btnReadGrid_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();
            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "Textfile *.txt|*.txt|XYZ *.xyz|*.xyz";

            //opens the dialog window (if a file is selected, everything inside the loop is executed)
            if (ofd.ShowDialog() == true)
            {
                #region JSON settings
                //set JSON settings of file format 
                //(Referencing to the BIMGISInteropsLibs, for which fileTypes an enumeration is used).
                init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.Grid;

                //set JSON settings of file path
                init.config.filePath = ofd.FileName;

                //set JSON settings of file name
                init.config.fileName = System.IO.Path.GetFileName(ofd.FileName);
                #endregion JSON settings

                #region logging
                LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] File (" + ofd.FileName + ") selected!"));

                guiLog.setLog("File selected! --> Please make settings and apply.");
                #endregion logging

                chkGridBB.IsEnabled = true;

                checkApply();
            }
            
        }

        /// <summary>
        /// will be executed as soon the user klicked the process button
        /// </summary>
        private void btnProcessGrid_Click(object sender, RoutedEventArgs e)
        {
            #region set json settings
            if (chkGridBB.IsChecked.GetValueOrDefault())
            {
                //bounding box will be used
                init.config.bBox = true;

                init.config.bbNorth = double.Parse(tbP1X.Text);
                init.config.bbWest = double.Parse(tbP1Y.Text);
                init.config.bbSouth = double.Parse(tbP2X.Text);
                init.config.bbEast = double.Parse(tbP2Y.Text);
            }
            else
            {
                //bounding box will not be used
                init.config.bBox = false;
            }

            if (rbGeodeticCRS.IsChecked.GetValueOrDefault())
            {
                init.config.mathematicCRS = false;
            }

            if (rbMathematicCRS.IsChecked.GetValueOrDefault())
            {
                init.config.mathematicCRS = true;
            }

            init.config.readPoints = true;
            #endregion set json settings

            //error handling
            //set task (file opening) to true
            guiLog.taskfileOpening = true;

            //[IfcTerrain] check if all task are allready done
            guiLog.readyState();

            //[DTM2BIM] check if all task are allready done
            guiLog.rdyDTM2BIM();

            //display short information about imported file to user
            guiLog.fileReaded();

            //gui logging (user information)
            guiLog.setLog("Grid settings applyed.");
        }

        /// <summary>
        /// check the textbox input if it corresponds to the regex
        /// </summary>
        private void tbBB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //regex only numbers (no comma or dot)
            Regex regex = new Regex("^[0-9]*$");

            //if not valid no input follows
            e.Handled = !regex.IsMatch(e.Text);

            //
            checkBondingBoxValues();
        }

        /// <summary>
        /// 
        /// </summary>
        private void checkApply()
        {
            checkBondingBoxValues();

            var chk = checker.LastOrDefault() as checker;

            if (chk.boundingBox
                && chk.bbValues)
            {
                chk.enableApply = true;
            }
            else if (!chk.boundingBox)
            {
                chk.enableApply = true;
            }
            else
            {
                chk.enableApply = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void checkBondingBoxValues()
        {
            var chk = checker.LastOrDefault() as checker;

            if (!string.IsNullOrEmpty(tbP1X.Text) 
                && !string.IsNullOrEmpty(tbP1Y.Text)
                && !string.IsNullOrEmpty(tbP2X.Text)
                && !string.IsNullOrEmpty(tbP2Y.Text))
            {
                chk.bbValues = true;
                return;
            }
            chk.bbValues = false;
        }

        private void chkGridBB_Check(object sender, RoutedEventArgs e)
        {
            checkApply();
        }

        private void tbBB_TextChanged(object sender, TextChangedEventArgs e)
        {
            checkApply();
        }
    }

    public class checker : INotifyPropertyChanged
    {
        /// <summary>
        /// do not rename (otherwise whole 'store' interface is not valid)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// method to check if property has 'really' changed
        /// </summary>
        /// <param name="info"></param>
        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private bool _boundingBox { get; set; }

        public bool boundingBox
        {
            get { return _boundingBox; }
            set
            {
                _boundingBox = value;
                NotifyPropertyChanged(nameof(boundingBox));
            }
        }

        private bool _enableApply { get; set; }

        public bool enableApply
        {
            get { return _enableApply; }
            set
            {
                _enableApply = value;
                NotifyPropertyChanged(nameof(enableApply));
            }
        }

        private bool _bbValues { get; set; }

        public bool bbValues
        {
            get { return _bbValues; }
            set
            {
                _bbValues = value;
                NotifyPropertyChanged(nameof(bbValues));
            }
        }
    }
}


