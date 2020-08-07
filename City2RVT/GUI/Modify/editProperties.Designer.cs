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
            this.dgv_editProperties = new System.Windows.Forms.DataGridView();
            this.applyButton = new System.Windows.Forms.Button();
            this.CheckAll = new System.Windows.Forms.Button();
            this.UncheckAll = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_editProperties)).BeginInit();
            this.SuspendLayout();
            // 
            // editPropertiesGrid
            // 
            this.dgv_editProperties.AllowUserToAddRows = false;
            this.dgv_editProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_editProperties.Location = new System.Drawing.Point(81, 63);
            this.dgv_editProperties.Margin = new System.Windows.Forms.Padding(6);
            this.dgv_editProperties.Name = "editPropertiesGrid";
            this.dgv_editProperties.RowHeadersWidth = 82;
            this.dgv_editProperties.Size = new System.Drawing.Size(1232, 693);
            this.dgv_editProperties.TabIndex = 1;
            this.dgv_editProperties.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.dgv_editProperties.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.editPropertiesGrid_CellContentClick);
            this.dgv_editProperties.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
            // 
            // applyButton
            // 
            this.applyButton.Location = new System.Drawing.Point(1325, 630);
            this.applyButton.Margin = new System.Windows.Forms.Padding(6);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(150, 44);
            this.applyButton.TabIndex = 2;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // CheckAll
            // 
            this.CheckAll.Location = new System.Drawing.Point(1325, 63);
            this.CheckAll.Margin = new System.Windows.Forms.Padding(6);
            this.CheckAll.Name = "CheckAll";
            this.CheckAll.Size = new System.Drawing.Size(150, 44);
            this.CheckAll.TabIndex = 3;
            this.CheckAll.Text = "Check all";
            this.CheckAll.UseVisualStyleBackColor = true;
            this.CheckAll.Click += new System.EventHandler(this.CheckAll_Click);
            // 
            // UncheckAll
            // 
            this.UncheckAll.Location = new System.Drawing.Point(1325, 119);
            this.UncheckAll.Margin = new System.Windows.Forms.Padding(6);
            this.UncheckAll.Name = "UncheckAll";
            this.UncheckAll.Size = new System.Drawing.Size(150, 44);
            this.UncheckAll.TabIndex = 4;
            this.UncheckAll.Text = "Uncheck all";
            this.UncheckAll.UseVisualStyleBackColor = true;
            this.UncheckAll.Click += new System.EventHandler(this.UncheckAll_Click);
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(1325, 712);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(150, 44);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // editProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1519, 821);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.UncheckAll);
            this.Controls.Add(this.CheckAll);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.dgv_editProperties);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "editProperties";
            this.Text = "Edit IFC properties";
            this.Load += new System.EventHandler(this.showRelatives_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_editProperties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridView dgv_editProperties;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button CheckAll;
        private System.Windows.Forms.Button UncheckAll;
        private System.Windows.Forms.Button closeButton;
    }
}