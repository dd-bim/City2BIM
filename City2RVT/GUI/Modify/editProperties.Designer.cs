namespace City2RVT.GUI.Modify
{
    partial class editProperties
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
            this.editPropertiesGrid = new System.Windows.Forms.DataGridView();
            this.applyButton = new System.Windows.Forms.Button();
            this.CheckAll = new System.Windows.Forms.Button();
            this.UncheckAll = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.editPropertiesGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // editPropertiesGrid
            // 
            this.editPropertiesGrid.AllowUserToAddRows = false;
            this.editPropertiesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.editPropertiesGrid.Location = new System.Drawing.Point(40, 33);
            this.editPropertiesGrid.Name = "editPropertiesGrid";
            this.editPropertiesGrid.Size = new System.Drawing.Size(324, 331);
            this.editPropertiesGrid.TabIndex = 1;
            this.editPropertiesGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.editPropertiesGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
            // 
            // applyButton
            // 
            this.applyButton.Location = new System.Drawing.Point(370, 370);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 2;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // CheckAll
            // 
            this.CheckAll.Location = new System.Drawing.Point(370, 33);
            this.CheckAll.Name = "CheckAll";
            this.CheckAll.Size = new System.Drawing.Size(75, 23);
            this.CheckAll.TabIndex = 3;
            this.CheckAll.Text = "Check all";
            this.CheckAll.UseVisualStyleBackColor = true;
            this.CheckAll.Click += new System.EventHandler(this.CheckAll_Click);
            // 
            // UncheckAll
            // 
            this.UncheckAll.Location = new System.Drawing.Point(370, 62);
            this.UncheckAll.Name = "UncheckAll";
            this.UncheckAll.Size = new System.Drawing.Size(75, 23);
            this.UncheckAll.TabIndex = 4;
            this.UncheckAll.Text = "Uncheck all";
            this.UncheckAll.UseVisualStyleBackColor = true;
            this.UncheckAll.Click += new System.EventHandler(this.UncheckAll_Click);
            // 
            // editProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(516, 427);
            this.Controls.Add(this.UncheckAll);
            this.Controls.Add(this.CheckAll);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.editPropertiesGrid);
            this.Name = "editProperties";
            this.Text = "Edit IFC properties";
            this.Load += new System.EventHandler(this.showRelatives_Load);
            ((System.ComponentModel.ISupportInitialize)(this.editPropertiesGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridView editPropertiesGrid;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button CheckAll;
        private System.Windows.Forms.Button UncheckAll;
    }
}