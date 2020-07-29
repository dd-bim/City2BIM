namespace City2RVT.GUI.Modify
{
    partial class showProperties
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
            this.showPropertiesGrid = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.showPropertiesGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // showPropertiesGrid
            // 
            this.showPropertiesGrid.AllowUserToAddRows = false;
            this.showPropertiesGrid.AllowUserToDeleteRows = false;
            this.showPropertiesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.showPropertiesGrid.Location = new System.Drawing.Point(12, 12);
            this.showPropertiesGrid.Name = "showPropertiesGrid";
            this.showPropertiesGrid.Size = new System.Drawing.Size(451, 325);
            this.showPropertiesGrid.TabIndex = 0;
            this.showPropertiesGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.showPropertiesGrid_CellContentClick);
            // 
            // showProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 365);
            this.Controls.Add(this.showPropertiesGrid);
            this.Name = "showProperties";
            this.Text = "showProperties";
            this.Load += new System.EventHandler(this.showProperties_Load);
            ((System.ComponentModel.ISupportInitialize)(this.showPropertiesGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView showPropertiesGrid;
    }
}