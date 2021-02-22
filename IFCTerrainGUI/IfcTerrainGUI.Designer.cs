
namespace IFCTerrainGUI
{
    partial class IfcTerrainGUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IfcTerrainGUI));
            this.pBoxLogo = new System.Windows.Forms.PictureBox();
            this.lblImportSettings = new System.Windows.Forms.Label();
            this.lblExportSettings = new System.Windows.Forms.Label();
            this.tbControlFileSelection = new System.Windows.Forms.TabControl();
            this.tabPageDxf = new System.Windows.Forms.TabPage();
            this.lbDxfDtmLayer = new System.Windows.Forms.ListBox();
            this.lblDtmLayer = new System.Windows.Forms.Label();
            this.lblSelectDtmLayer = new System.Windows.Forms.Label();
            this.btnReadDxf = new System.Windows.Forms.Button();
            this.tabPageXml = new System.Windows.Forms.TabPage();
            this.tabPageGrafbat = new System.Windows.Forms.TabPage();
            this.tabPagePostGis = new System.Windows.Forms.TabPage();
            this.tabPageGrid = new System.Windows.Forms.TabPage();
            this.tabPageReb = new System.Windows.Forms.TabPage();
            backgroundWorkerDxf = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.pBoxLogo)).BeginInit();
            this.tbControlFileSelection.SuspendLayout();
            this.tabPageDxf.SuspendLayout();
            this.SuspendLayout();
            // 
            // pBoxLogo
            // 
            this.pBoxLogo.Image = global::IFCTerrainGUI.Properties.Resources.DD_BIM_LOGO;
            resources.ApplyResources(this.pBoxLogo, "pBoxLogo");
            this.pBoxLogo.Name = "pBoxLogo";
            this.pBoxLogo.TabStop = false;
            // 
            // lblImportSettings
            // 
            resources.ApplyResources(this.lblImportSettings, "lblImportSettings");
            this.lblImportSettings.Name = "lblImportSettings";
            // 
            // lblExportSettings
            // 
            resources.ApplyResources(this.lblExportSettings, "lblExportSettings");
            this.lblExportSettings.Name = "lblExportSettings";
            // 
            // tbControlFileSelection
            // 
            this.tbControlFileSelection.Controls.Add(this.tabPageDxf);
            this.tbControlFileSelection.Controls.Add(this.tabPageXml);
            this.tbControlFileSelection.Controls.Add(this.tabPageGrafbat);
            this.tbControlFileSelection.Controls.Add(this.tabPagePostGis);
            this.tbControlFileSelection.Controls.Add(this.tabPageGrid);
            this.tbControlFileSelection.Controls.Add(this.tabPageReb);
            resources.ApplyResources(this.tbControlFileSelection, "tbControlFileSelection");
            this.tbControlFileSelection.Multiline = true;
            this.tbControlFileSelection.Name = "tbControlFileSelection";
            this.tbControlFileSelection.SelectedIndex = 0;
            // 
            // tabPageDxf
            // 
            this.tabPageDxf.Controls.Add(this.lbDxfDtmLayer);
            this.tabPageDxf.Controls.Add(this.lblDtmLayer);
            this.tabPageDxf.Controls.Add(this.lblSelectDtmLayer);
            this.tabPageDxf.Controls.Add(this.btnReadDxf);
            resources.ApplyResources(this.tabPageDxf, "tabPageDxf");
            this.tabPageDxf.Name = "tabPageDxf";
            this.tabPageDxf.UseVisualStyleBackColor = true;
            // 
            // lbDxfDtmLayer
            // 
            this.lbDxfDtmLayer.FormattingEnabled = true;
            resources.ApplyResources(this.lbDxfDtmLayer, "lbDxfDtmLayer");
            this.lbDxfDtmLayer.Name = "lbDxfDtmLayer";
            // 
            // lblDtmLayer
            // 
            resources.ApplyResources(this.lblDtmLayer, "lblDtmLayer");
            this.lblDtmLayer.Name = "lblDtmLayer";
            // 
            // lblSelectDtmLayer
            // 
            resources.ApplyResources(this.lblSelectDtmLayer, "lblSelectDtmLayer");
            this.lblSelectDtmLayer.Name = "lblSelectDtmLayer";
            // 
            // btnReadDxf
            // 
            resources.ApplyResources(this.btnReadDxf, "btnReadDxf");
            this.btnReadDxf.Name = "btnReadDxf";
            this.btnReadDxf.UseVisualStyleBackColor = true;
            this.btnReadDxf.Click += new System.EventHandler(this.btnReadDxf_Click);
            // 
            // tabPageXml
            // 
            resources.ApplyResources(this.tabPageXml, "tabPageXml");
            this.tabPageXml.Name = "tabPageXml";
            this.tabPageXml.UseVisualStyleBackColor = true;
            // 
            // tabPageGrafbat
            // 
            resources.ApplyResources(this.tabPageGrafbat, "tabPageGrafbat");
            this.tabPageGrafbat.Name = "tabPageGrafbat";
            this.tabPageGrafbat.UseVisualStyleBackColor = true;
            // 
            // tabPagePostGis
            // 
            resources.ApplyResources(this.tabPagePostGis, "tabPagePostGis");
            this.tabPagePostGis.Name = "tabPagePostGis";
            this.tabPagePostGis.UseVisualStyleBackColor = true;
            // 
            // tabPageGrid
            // 
            resources.ApplyResources(this.tabPageGrid, "tabPageGrid");
            this.tabPageGrid.Name = "tabPageGrid";
            this.tabPageGrid.UseVisualStyleBackColor = true;
            // 
            // tabPageReb
            // 
            resources.ApplyResources(this.tabPageReb, "tabPageReb");
            this.tabPageReb.Name = "tabPageReb";
            this.tabPageReb.UseVisualStyleBackColor = true;
            // 
            // backgroundWorkerDxf
            // 
            backgroundWorkerDxf.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerDxf_DoWork);
            backgroundWorkerDxf.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerDXF_RunWorkerCompleted);
            // 
            // IfcTerrainGUI
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tbControlFileSelection);
            this.Controls.Add(this.lblExportSettings);
            this.Controls.Add(this.lblImportSettings);
            this.Controls.Add(this.pBoxLogo);
            this.Name = "IfcTerrainGUI";
            ((System.ComponentModel.ISupportInitialize)(this.pBoxLogo)).EndInit();
            this.tbControlFileSelection.ResumeLayout(false);
            this.tabPageDxf.ResumeLayout(false);
            this.tabPageDxf.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pBoxLogo;
        private System.Windows.Forms.Label lblImportSettings;
        private System.Windows.Forms.Label lblExportSettings;
        private System.Windows.Forms.TabControl tbControlFileSelection;
        private System.Windows.Forms.TabPage tabPageXml;
        private System.Windows.Forms.TabPage tabPageDxf;
        private System.Windows.Forms.TabPage tabPageReb;
        private System.Windows.Forms.TabPage tabPageGrafbat;
        private System.Windows.Forms.TabPage tabPagePostGis;
        private System.Windows.Forms.TabPage tabPageGrid;
        private System.Windows.Forms.Button btnReadDxf;
        private System.Windows.Forms.Label lblSelectDtmLayer;
        private System.Windows.Forms.Label lblDtmLayer;
        private System.Windows.Forms.ListBox lbDxfDtmLayer;
        public static System.ComponentModel.BackgroundWorker backgroundWorkerDxf;
    }
}