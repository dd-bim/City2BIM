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

using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide error handling
using System.Text.RegularExpressions; //include to be able to restrict textbox entries
using Microsoft.Win32; //used for file handling

//shortcut to set json settings
using init = GuiHandler.InitClass;

namespace IFCTerrainGUI.GUI.PostGIS
{
    /// <summary>
    /// Interaction logic for ucReadPostGis.xaml
    /// </summary>
    public partial class ucReadPostGis : UserControl
    {
        public ucReadPostGis()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute processing via postgis input fields
        /// </summary>
        private void btnProcessPostGis_Click(object sender, RoutedEventArgs e)
        {

            #region logging [TODO]
            //add logging
            #endregion logging [TODO]

            #region set json settings
            //set "fileType" to PostGIS
            init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.PostGIS;

            //set host
            init.config.host = this.tbHost.Text;

            //set port
            init.config.port = Convert.ToInt32(this.tbPort.Text);

            //set username
            init.config.user = this.tbUser.Text;

            //set password
            init.config.password = this.tbPwd.Password;

            //set database
            init.config.database = this.tbDatabase.Text;

            //set schema
            init.config.schema = this.tbSchema.Text;

            //set tin geom (table)
            init.config.tin_table = this.tbTinTable.Text;

            //set tin geom (column)
            init.config.tin_column = this.tbTinColumn.Text;

            //set tin id (colum)
            init.config.tinid_column = this.tbTinIdColumn.Text;

            //set tin id (value)
            init.config.tin_id = Convert.ToInt32(this.tbTinIdValue.Text);

            //set is TIN
            init.config.isTin = true;

            #region breaklines
            //check if breaklines should be processed
            if(rbProcessBlTrue.IsChecked == true)
            {
                //breakline will be processed
                init.config.breakline = true;

                //set bl table
                init.config.breakline_table = this.tbBlTable.Text;

                //set bl column
                init.config.breakline_column = this.tbBlGeomColumn.Text;

                //set bl tin id (column)
                init.config.breakline_tin_id = this.tbBlTinIdColumn.Text;

                //gui feedback
                ((MainWindow)Application.Current.MainWindow).tbFileSpecific.Text = "Breaklines will be processed";
            }
            else
            {
                //breakline will not processed
                init.config.breakline = false;

                //gui feedback
                ((MainWindow)Application.Current.MainWindow).tbFileSpecific.Text = "Breaklines will NOT be processed";
            }
            #endregion breaklines
            #endregion set json settings

            #region gui feedback
            //return info to database
            ((MainWindow)Application.Current.MainWindow).tbFileName.Text = init.config.database;

            //return info to file type
            ((MainWindow)Application.Current.MainWindow).tbFileType.Text = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.PostGIS.ToString() + " request";

            //return info to breaklines
            ((MainWindow)Application.Current.MainWindow).ipFileSpecific.Text = "Breaklines";

            //gui logging (user information)
            MainWindowBib.setGuiLog("PostGIS settings applyed.");
            #endregion gui feedback

            #region error handling
            //set task (file opening) to true
            GuiHandler.GuiSupport.taskfileOpening = true;

            //check if all task are allready done
            MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState());
            #endregion error handling
        }

        /// <summary>
        /// Check that all required tb fields are not empty (via booleans)
        /// </summary>
        private bool readyState()
        {
            //if all tbs checker set to true
            if (this.inputHost && this.inputPort 
                && this.inputUsername && this.inputPassword 
                && this.inputDatabase && this.inputSchema
                && this.inputGeomTable && this.inputGeomColumn
                && this.inputTinId && this.inputTinValue)
            {
                if(this.rbProcessBlFalse.IsChecked == true)
                {
                    //enable process postgis button
                    this.btnProcessPostGis.IsEnabled = true;
                }
                else if ((this.rbProcessBlTrue.IsChecked == true) 
                    && this.inputBlTable
                    && this.inputBlColumn
                    && this.inputBlTinId)
                {
                    //enable process postgis button
                    this.btnProcessPostGis.IsEnabled = true;
                }
                else
                {
                    this.btnProcessPostGis.IsEnabled = false;
                }
                //return
                return true;
            }
            else
            {
                //disable process postgis button
                this.btnProcessPostGis.IsEnabled = false;
                return false;
            }
        }
        #region input fields checker
        #region database & tin geom
        /// <summary>
        /// check if host is set
        /// </summary>
        private bool inputHost { get; set; }

        /// <summary>
        /// check if port is set
        /// </summary>
        private bool inputPort { get; set; }

        /// <summary>
        /// check if username is set
        /// </summary>
        private bool inputUsername { get; set; }

        /// <summary>
        /// check if username is set
        /// </summary>
        private bool inputPassword { get; set; }

        /// <summary>
        /// check if username is set
        /// </summary>
        private bool inputDatabase { get; set; }

        /// <summary>
        /// check if username is set
        /// </summary>
        private bool inputSchema { get; set; }

        /// <summary>
        /// check if geom table is set
        /// </summary>
        private bool inputGeomTable { get; set; }

        /// <summary>
        /// check if geom column is set
        /// </summary>
        private bool inputGeomColumn { get; set; }

        /// <summary>
        /// check if tin id column is set
        /// </summary>
        private bool inputTinId { get; set; }

        /// <summary>
        /// check if tin value is set
        /// </summary>
        private bool inputTinValue { get; set; }


        #endregion database & tin geom
        #region breaklines
        /// <summary>
        /// check if breakline table is set
        /// </summary>
        private bool inputBlTable { get; set; }

        /// <summary>
        /// check if breakline column is set
        /// </summary>
        private bool inputBlColumn { get; set; }

        /// <summary>
        /// check if breakline tin id is set
        /// </summary>
        private bool inputBlTinId { get; set; }

        #endregion breaklines
        #endregion input fields checker

        /// <summary>
        /// check the textbox input if it corresponds to the regex
        /// </summary>
        private void tbPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //regex only numbers (no comma or dot)
            Regex regex = new Regex("^[0-9]*$");

            //if not valid no input follows
            e.Handled = !regex.IsMatch(e.Text);
        }

        /// <summary>
        /// is executed as soon as a text field is changed
        /// </summary>
        private void tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.tbHost.Text))
            {
                //set to true
                this.inputHost = true;
            }
            else
            {
                //set to false --> text box is empty (so no processing should be possible)
                this.inputHost = false;
            }

            if (!string.IsNullOrEmpty(this.tbPort.Text))
            {
                this.inputPort = true;
            }
            else
            {
                this.inputPort = false;
            }

            if (!string.IsNullOrEmpty(this.tbUser.Text))
            {
                this.inputUsername = true;
            }
            else
            {
                this.inputUsername = false;
            }

            if (!string.IsNullOrEmpty(this.tbPwd.Password))
            {
                this.inputPassword = true;
            }
            else
            {
                this.inputPassword = false;
            }

            if (!string.IsNullOrEmpty(this.tbDatabase.Text))
            {
                this.inputDatabase = true;
            }
            else
            {
                this.inputDatabase = false;
            }

            if (!string.IsNullOrEmpty(this.tbSchema.Text))
            {
                this.inputSchema = true;
            }
            else
            {
                this.inputSchema = false;
            }

            if (!string.IsNullOrEmpty(this.tbTinTable.Text))
            {
                this.inputGeomTable = true;
            }
            else
            {
                this.inputGeomTable = false;
            }

            if (!string.IsNullOrEmpty(this.tbTinColumn.Text))
            {
                this.inputGeomColumn = true;
            }
            else
            {
                this.inputGeomColumn = false;
            }

            if (!string.IsNullOrEmpty(this.tbTinIdColumn.Text))
            {
                this.inputTinId = true;
            }
            else
            {
                this.inputTinId = false;
            }

            if (!string.IsNullOrEmpty(this.tbTinIdValue.Text))
            {
                this.inputTinValue = true;
            }
            else
            {
                this.inputTinValue = false;
            }

            //breakline checker (below)
            if (!string.IsNullOrEmpty(this.tbBlTable.Text))
            {
                this.inputBlTable = true;
            }
            else
            {
                this.inputBlTable = false;
            }


            if (!string.IsNullOrEmpty(this.tbBlGeomColumn.Text))
            {
                this.inputBlColumn = true;
            }
            else
            {
                this.inputBlColumn = false;
            }

            if (!string.IsNullOrEmpty(this.tbBlTinIdColumn.Text))
            {
                this.inputBlTinId = true;
            }
            else
            {
                this.inputBlTinId = false;
            }

            //check if all fields are not empty
            readyState();
            return;
        }

        private void rbProcessBlTrue_Checked(object sender, RoutedEventArgs e)
        {
            //try
            readyState();

            //enable textboxes for input
            this.tbBlTable.IsEnabled = true;
            this.tbBlGeomColumn.IsEnabled = true;
            this.tbBlTinIdColumn.IsEnabled = true;
        }

        private void rbProcessBlFalse_Checked(object sender, RoutedEventArgs e)
        {
            //try
            readyState();

            //disable textboxes for input
            this.tbBlTable.IsEnabled = false;
            this.tbBlGeomColumn.IsEnabled = false;
            this.tbBlTinIdColumn.IsEnabled = false;
        }

        /// <summary>
        /// Load example fields (only tin)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPostGisExampleTin_Click(object sender, RoutedEventArgs e)
        {
            this.tbHost.Text = "terrain.dd-bim.org";
            this.tbPort.Text = "5432";
            this.tbUser.Text = "marcus";
            this.tbPwd.Password = "123456";
            this.tbDatabase.Text = "tt";
            this.tbSchema.Text = "terraintwin";
            this.tbTinTable.Text = "tin";
            this.tbTinColumn.Text = "geometry";
            this.tbTinIdColumn.Text = "tin_id";
            this.tbTinIdValue.Text = "3";
            this.rbProcessBlFalse.IsChecked = true;
        }


        /// <summary>
        /// Load example fields (tin with break lines)
        /// </summary>
        private void btnPostGisExampelTinBl_Click(object sender, RoutedEventArgs e)
        {
            this.btnPostGisExampleTin_Click(sender,e);
            this.rbProcessBlTrue.IsChecked = true;

            this.tbBlGeomColumn.Text = "breaklines";
            this.tbBlTable.Text = "geometry";
            this.tbBlTinIdColumn.Text = "tin_id";
        }

    }
}
