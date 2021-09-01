﻿using System;
using System.Windows;

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace City2RVT.GUI.DTM2BIM
{
    /// <summary>
    /// Interaktionslogik für Terrain_ImportUI.xaml
    /// </summary>
    public partial class Terrain_ImportUI : Window
    {
        /// <summary>
        /// return this value to start import in revit cmd 
        /// </summary>
        public bool startTerrainImport { get { return startImport; } }

        /// <summary>
        /// Value to be specified that the import should be started.
        /// </summary>
        private bool startImport { set; get; } = false;

        /// <summary>
        /// init DTM2BIM main window
        /// </summary>
        public Terrain_ImportUI()
        {   
            InitializeComponent();

            //send gui logging
            guiLog.setLog("Welcome to DTM2BIM");
        }

        /// <summary>
        /// kick off settings for DTM import
        /// </summary>
        private void btnStartImport_Click(object sender, RoutedEventArgs e)
        {
            //start mapping process
            startImport = true;

            Close();
        }

        /// <summary>
        /// error handling for processing selection
        /// </summary>
        private void cbProcessing_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbFaces.IsSelected)
            {
                init.config.readPoints = false;
            }
            else
            {
                init.config.readPoints = true;
            }
            
            if(cbProcessing.SelectedIndex != -1)
            {
                //set task to done
                guiLog.selectProcessing = true;
                
                //ready checker
                enableStartBtn(guiLog.rdyDTM2BIM());

                return;
            }
            return;
        }

        /// <summary>
        /// enable or disable button (error handling)
        /// </summary>
        private void enableStartBtn(bool enable)
        {
            if (enable)
            {
                this.btnStartImport.IsEnabled = true;
            }
            else
            {
                this.btnStartImport.IsEnabled = false;
            }
        }

        /// <summary>
        /// clear log on closing window
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            guiLog.clearLog();
        }
    }
}
