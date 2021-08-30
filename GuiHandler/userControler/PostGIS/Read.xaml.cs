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

//using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide error handling
using System.Text.RegularExpressions; //include to be able to restrict textbox entries

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace GuiHandler.userControler.PostGIS
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

        /// <summary>
        /// Execute processing via postgis input fields
        /// </summary>
        private void btnProcessPostGis_Click(object sender, RoutedEventArgs e)
        {
            #region set json settings
            //set "fileType" to PostGIS
            init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.PostGIS;
            init.config.fileName = tbDatabase.Text;

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
            if (chkPrepQuery.IsChecked.GetValueOrDefault())
            {
                //set tin geom (table)
                init.config.tin_table = this.tbTinTable.Text;

                //set tin geom (column)
                init.config.tin_column = this.tbTinColumn.Text;

                //set tin id (colum)
                init.config.tinid_column = this.tbTinIdColumn.Text;

                //set tin id (value)
                init.config.tin_id = this.tbTinIdValue.Text;
                
                #region breaklines
                //check if breaklines should be processed
                if (chkPrepStatementBreakline.IsChecked.GetValueOrDefault())
                {
                    //breakline will be processed
                    init.config.breakline = true;

                    //set bl table
                    init.config.breakline_table = tbBlTable.Text;

                    //set bl column
                    init.config.breakline_column = this.tbBlGeomColumn.Text;

                    //set bl tin id (column)
                    init.config.breakline_tin_id = this.tbBlTinIdColumn.Text;
                }
                else
                {
                    //breakline will not processed
                    init.config.breakline = false;
                }
                #endregion breaklines
            }
            else
            {
                if (chkCustomBreakline.IsChecked.GetValueOrDefault())
                {
                    init.config.breakline = true;
                    init.config.queryString = tbQueryString.Text;
                    init.config.breaklineQueryString = tbBreaklineQueryString.Text;
                }
                else
                {
                    init.config.breakline = false;
                    init.config.queryString = tbQueryString.Text;
                }
            }
            #endregion set json settings

            #region gui feedback
            //display short information about imported file to user
            guiLog.fileReaded();

            //gui logging (user information)
            guiLog.setLog("PostGIS settings applyed.");
            #endregion gui feedback

            #region error handling
            
            //set task (file opening) to true
            GuiSupport.taskfileOpening = true;

            //[IfcTerrain] check if all task are allready done
            GuiSupport.readyState();

            //[DTM2BIM] check if all task are allready done
            GuiSupport.rdyDTM2BIM();

            //check if all task are allready done
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
               )
            {
                if (chkPrepQuery.IsChecked.GetValueOrDefault())
                {
                    if (inputGeomTable
                        && inputGeomColumn
                        && inputTinId
                        && inputTinValue)
                    {
                        if (!chkPrepStatementBreakline.IsChecked.GetValueOrDefault())
                        {
                            btnProcessPostGis.IsEnabled = true;
                        }
                        else if (chkPrepStatementBreakline.IsChecked.GetValueOrDefault()
                            && inputBlTable
                            && inputBlColumn
                            && inputBlTinId)
                        {
                            btnProcessPostGis.IsEnabled = true;
                        }
                        else
                        {
                            btnProcessPostGis.IsEnabled = false;
                            return false;
                        }
                    }
                    else
                    {
                        btnProcessPostGis.IsEnabled = false;
                        return false;
                    }
                }
                else if (chkCustomQuery.IsChecked.GetValueOrDefault())
                {
                    if (!chkCustomBreakline.IsChecked.GetValueOrDefault()
                        && inputUserQuery)
                    {
                        btnProcessPostGis.IsEnabled = true;
                    }
                    else if(chkCustomBreakline.IsChecked.GetValueOrDefault()
                        && inputUserQuery
                        && inputUserBlQuery)
                    {
                        btnProcessPostGis.IsEnabled = true;
                    }
                    else
                    {
                        btnProcessPostGis.IsEnabled = false;
                    }
                }
                else
                {
                    btnProcessPostGis.IsEnabled = false;
                    return false;
                }
            }
            else
            {
                btnProcessPostGis.IsEnabled = false;
            }
            return false;
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
        private bool inputUserQuery { get; set; }
        private bool inputUserBlQuery { get; set; }
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

            if (!string.IsNullOrEmpty(tbBlTinIdColumn.Text))
            {
                inputBlTinId = true;
            }
            else
            {
                this.inputBlTinId = false;
            }

            if (!string.IsNullOrEmpty(tbQueryString.Text))
            { inputUserQuery = true; } 
            else { inputUserQuery = false; }

            if (!string.IsNullOrEmpty(tbBreaklineQueryString.Text))
            { inputUserBlQuery = true; } 
            else { inputUserBlQuery = false; }

            //check if all fields are not empty
            readyState();
            return;
        }

        private void chkPrepStatementBreakline_Checked(object sender, RoutedEventArgs e)
        {
            readyState();
        }

        private void chkPrepStatementBreakline_Unchecked(object sender, RoutedEventArgs e)
        {
            readyState();
        }

        private void chkCustomQuery_Checked(object sender, RoutedEventArgs e)
        {
            chkPrepQuery.IsChecked = false;
            readyState();
        }

        private void chkPrepQuery_Checked(object sender, RoutedEventArgs e)
        {
            chkCustomQuery.IsChecked = false;
            readyState();
        }

        private void chkPrepQuery_Unchecked(object sender, RoutedEventArgs e)
        {
            readyState();
        }
    }
}
