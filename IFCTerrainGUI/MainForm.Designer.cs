
namespace IFCTerrainGUI
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pBoxLogo = new System.Windows.Forms.PictureBox();
            this.lblImportSettings = new System.Windows.Forms.Label();
            this.lblExportSettings = new System.Windows.Forms.Label();
            this.tbControlFileSelection = new System.Windows.Forms.TabControl();
            this.tabPageDxf = new System.Windows.Forms.TabPage();
            this.gpDxfBreakLines = new System.Windows.Forms.GroupBox();
            this.lbDxfBreaklineLayer = new System.Windows.Forms.ListBox();
            this.lblDxfBreakLines = new System.Windows.Forms.Label();
            this.rbDxfBreakLinesProcessNo = new System.Windows.Forms.RadioButton();
            this.rbDxfBreakLinesProcessYes = new System.Windows.Forms.RadioButton();
            this.lblDxfProcessBreaklines = new System.Windows.Forms.Label();
            this.gpBoxDxfDtm = new System.Windows.Forms.GroupBox();
            this.lbDxfDtmLayer = new System.Windows.Forms.ListBox();
            this.lblDtmLayer = new System.Windows.Forms.Label();
            this.rbDxfFaces = new System.Windows.Forms.RadioButton();
            this.rbDxfPointsLines = new System.Windows.Forms.RadioButton();
            this.btnReadDxf = new System.Windows.Forms.Button();
            this.tabPageXml = new System.Windows.Forms.TabPage();
            this.tabPageGrafbat = new System.Windows.Forms.TabPage();
            this.tabPagePostGis = new System.Windows.Forms.TabPage();
            this.tabPageGrid = new System.Windows.Forms.TabPage();
            this.tabPageReb = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.pBoxLogo)).BeginInit();
            this.tbControlFileSelection.SuspendLayout();
            this.tabPageDxf.SuspendLayout();
            this.gpDxfBreakLines.SuspendLayout();
            this.gpBoxDxfDtm.SuspendLayout();
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
            this.tabPageDxf.Controls.Add(this.gpDxfBreakLines);
            this.tabPageDxf.Controls.Add(this.gpBoxDxfDtm);
            this.tabPageDxf.Controls.Add(this.btnReadDxf);
            resources.ApplyResources(this.tabPageDxf, "tabPageDxf");
            this.tabPageDxf.Name = "tabPageDxf";
            this.tabPageDxf.UseVisualStyleBackColor = true;
            // 
            // gpDxfBreakLines
            // 
            this.gpDxfBreakLines.Controls.Add(this.lbDxfBreaklineLayer);
            this.gpDxfBreakLines.Controls.Add(this.lblDxfBreakLines);
            this.gpDxfBreakLines.Controls.Add(this.rbDxfBreakLinesProcessNo);
            this.gpDxfBreakLines.Controls.Add(this.rbDxfBreakLinesProcessYes);
            this.gpDxfBreakLines.Controls.Add(this.lblDxfProcessBreaklines);
            resources.ApplyResources(this.gpDxfBreakLines, "gpDxfBreakLines");
            this.gpDxfBreakLines.Name = "gpDxfBreakLines";
            this.gpDxfBreakLines.TabStop = false;
            // 
            // lbDxfBreaklineLayer
            // 
            resources.ApplyResources(this.lbDxfBreaklineLayer, "lbDxfBreaklineLayer");
            this.lbDxfBreaklineLayer.FormattingEnabled = true;
            this.lbDxfBreaklineLayer.Name = "lbDxfBreaklineLayer";
            this.lbDxfBreaklineLayer.Sorted = true;
            // 
            // lblDxfBreakLines
            // 
            resources.ApplyResources(this.lblDxfBreakLines, "lblDxfBreakLines");
            this.lblDxfBreakLines.Name = "lblDxfBreakLines";
            // 
            // rbDxfBreakLinesProcessNo
            // 
            resources.ApplyResources(this.rbDxfBreakLinesProcessNo, "rbDxfBreakLinesProcessNo");
            this.rbDxfBreakLinesProcessNo.Name = "rbDxfBreakLinesProcessNo";
            this.rbDxfBreakLinesProcessNo.TabStop = true;
            this.rbDxfBreakLinesProcessNo.UseVisualStyleBackColor = true;
            // 
            // rbDxfBreakLinesProcessYes
            // 
            resources.ApplyResources(this.rbDxfBreakLinesProcessYes, "rbDxfBreakLinesProcessYes");
            this.rbDxfBreakLinesProcessYes.Name = "rbDxfBreakLinesProcessYes";
            this.rbDxfBreakLinesProcessYes.TabStop = true;
            this.rbDxfBreakLinesProcessYes.UseVisualStyleBackColor = true;
            // 
            // lblDxfProcessBreaklines
            // 
            resources.ApplyResources(this.lblDxfProcessBreaklines, "lblDxfProcessBreaklines");
            this.lblDxfProcessBreaklines.Name = "lblDxfProcessBreaklines";
            // 
            // gpBoxDxfDtm
            // 
            this.gpBoxDxfDtm.Controls.Add(this.lbDxfDtmLayer);
            this.gpBoxDxfDtm.Controls.Add(this.lblDtmLayer);
            this.gpBoxDxfDtm.Controls.Add(this.rbDxfFaces);
            this.gpBoxDxfDtm.Controls.Add(this.rbDxfPointsLines);
            resources.ApplyResources(this.gpBoxDxfDtm, "gpBoxDxfDtm");
            this.gpBoxDxfDtm.Name = "gpBoxDxfDtm";
            this.gpBoxDxfDtm.TabStop = false;
            // 
            // lbDxfDtmLayer
            // 
            this.lbDxfDtmLayer.FormattingEnabled = true;
            resources.ApplyResources(this.lbDxfDtmLayer, "lbDxfDtmLayer");
            this.lbDxfDtmLayer.Name = "lbDxfDtmLayer";
            this.lbDxfDtmLayer.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbDxfDtmLayer.Sorted = true;
            // 
            // lblDtmLayer
            // 
            resources.ApplyResources(this.lblDtmLayer, "lblDtmLayer");
            this.lblDtmLayer.Name = "lblDtmLayer";
            // 
            // rbDxfFaces
            // 
            resources.ApplyResources(this.rbDxfFaces, "rbDxfFaces");
            this.rbDxfFaces.Name = "rbDxfFaces";
            this.rbDxfFaces.TabStop = true;
            this.rbDxfFaces.UseVisualStyleBackColor = true;
            // 
            // rbDxfPointsLines
            // 
            resources.ApplyResources(this.rbDxfPointsLines, "rbDxfPointsLines");
            this.rbDxfPointsLines.Name = "rbDxfPointsLines";
            this.rbDxfPointsLines.TabStop = true;
            this.rbDxfPointsLines.UseVisualStyleBackColor = true;
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
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tbControlFileSelection);
            this.Controls.Add(this.lblExportSettings);
            this.Controls.Add(this.lblImportSettings);
            this.Controls.Add(this.pBoxLogo);
            this.Name = "MainForm";
            ((System.ComponentModel.ISupportInitialize)(this.pBoxLogo)).EndInit();
            this.tbControlFileSelection.ResumeLayout(false);
            this.tabPageDxf.ResumeLayout(false);
            this.gpDxfBreakLines.ResumeLayout(false);
            this.gpDxfBreakLines.PerformLayout();
            this.gpBoxDxfDtm.ResumeLayout(false);
            this.gpBoxDxfDtm.PerformLayout();
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
        public System.Windows.Forms.ListBox lbDxfDtmLayer;
        private System.Windows.Forms.RadioButton rbDxfFaces;
        private System.Windows.Forms.RadioButton rbDxfPointsLines;
        private System.Windows.Forms.GroupBox gpDxfBreakLines;
        private System.Windows.Forms.Label lblDxfProcessBreaklines;
        private System.Windows.Forms.RadioButton rbDxfBreakLinesProcessNo;
        private System.Windows.Forms.RadioButton rbDxfBreakLinesProcessYes;
        private System.Windows.Forms.Label lblDxfBreakLines;
        private System.Windows.Forms.ListBox lbDxfBreaklineLayer;
        public System.Windows.Forms.GroupBox gpBoxDxfDtm;
        internal System.Windows.Forms.Label lblDtmLayer;
    }
}