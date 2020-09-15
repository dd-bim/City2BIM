namespace City2RVT.GUI.Properties
{
    partial class Wf_showEntities
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
            this.dgv_showEntites = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_showEntites)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv_showEntites
            // 
            this.dgv_showEntites.AllowUserToAddRows = false;
            this.dgv_showEntites.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_showEntites.Location = new System.Drawing.Point(90, 23);
            this.dgv_showEntites.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.dgv_showEntites.Name = "dgv_showEntites";
            this.dgv_showEntites.RowHeadersWidth = 82;
            this.dgv_showEntites.Size = new System.Drawing.Size(704, 556);
            this.dgv_showEntites.TabIndex = 0;
            this.dgv_showEntites.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_showEntites_CellContentClick);
            this.dgv_showEntites.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_showEntites_CellDoubleClick);
            // 
            // Wf_showEntities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(938, 712);
            this.Controls.Add(this.dgv_showEntites);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "Wf_showEntities";
            this.Text = "Wf_showEntities";
            this.Load += new System.EventHandler(this.Wf_showEntities_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_showEntites)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_showEntites;
    }
}