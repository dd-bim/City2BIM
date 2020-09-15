namespace City2RVT.GUI.Modify
{
    partial class editElement
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPsets = new System.Windows.Forms.TabPage();
            this.btn_apply = new System.Windows.Forms.Button();
            this.dgv_tabPsets = new System.Windows.Forms.DataGridView();
            this.tabOriginal = new System.Windows.Forms.TabPage();
            this.dgv_original = new System.Windows.Forms.DataGridView();
            this.tabControl1.SuspendLayout();
            this.tabPsets.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_tabPsets)).BeginInit();
            this.tabOriginal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_original)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPsets);
            this.tabControl1.Controls.Add(this.tabOriginal);
            this.tabControl1.Location = new System.Drawing.Point(28, 61);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1188, 665);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPsets
            // 
            this.tabPsets.Controls.Add(this.btn_apply);
            this.tabPsets.Controls.Add(this.dgv_tabPsets);
            this.tabPsets.Location = new System.Drawing.Point(8, 39);
            this.tabPsets.Name = "tabPsets";
            this.tabPsets.Padding = new System.Windows.Forms.Padding(3);
            this.tabPsets.Size = new System.Drawing.Size(1172, 618);
            this.tabPsets.TabIndex = 0;
            this.tabPsets.Text = "IFC Property Sets";
            this.tabPsets.UseVisualStyleBackColor = true;
            this.tabPsets.Click += new System.EventHandler(this.tabPsets_Click);
            // 
            // btn_apply
            // 
            this.btn_apply.Location = new System.Drawing.Point(933, 67);
            this.btn_apply.Name = "btn_apply";
            this.btn_apply.Size = new System.Drawing.Size(208, 66);
            this.btn_apply.TabIndex = 1;
            this.btn_apply.Text = "Apply";
            this.btn_apply.UseVisualStyleBackColor = true;
            this.btn_apply.Click += new System.EventHandler(this.btn_applyPsets_Click);
            // 
            // dgv_tabPsets
            // 
            this.dgv_tabPsets.AllowUserToAddRows = false;
            this.dgv_tabPsets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_tabPsets.Location = new System.Drawing.Point(5, 20);
            this.dgv_tabPsets.Name = "dgv_tabPsets";
            this.dgv_tabPsets.RowHeadersWidth = 82;
            this.dgv_tabPsets.RowTemplate.Height = 33;
            this.dgv_tabPsets.Size = new System.Drawing.Size(862, 460);
            this.dgv_tabPsets.TabIndex = 0;
            this.dgv_tabPsets.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_tabPsets_CellContentClick);
            // 
            // tabOriginal
            // 
            this.tabOriginal.Controls.Add(this.dgv_original);
            this.tabOriginal.Location = new System.Drawing.Point(8, 39);
            this.tabOriginal.Name = "tabOriginal";
            this.tabOriginal.Padding = new System.Windows.Forms.Padding(3);
            this.tabOriginal.Size = new System.Drawing.Size(1172, 618);
            this.tabOriginal.TabIndex = 1;
            this.tabOriginal.Text = "Original";
            this.tabOriginal.UseVisualStyleBackColor = true;
            // 
            // dgv_original
            // 
            this.dgv_original.AllowUserToAddRows = false;
            this.dgv_original.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_original.Location = new System.Drawing.Point(71, 80);
            this.dgv_original.Name = "dgv_original";
            this.dgv_original.RowHeadersWidth = 82;
            this.dgv_original.RowTemplate.Height = 33;
            this.dgv_original.Size = new System.Drawing.Size(875, 483);
            this.dgv_original.TabIndex = 0;
            this.dgv_original.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_original_CellContentClick);
            // 
            // editElement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1254, 738);
            this.Controls.Add(this.tabControl1);
            this.Name = "editElement";
            this.Text = "editElement";
            this.Load += new System.EventHandler(this.editElement_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPsets.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_tabPsets)).EndInit();
            this.tabOriginal.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_original)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPsets;
        private System.Windows.Forms.TabPage tabOriginal;
        private System.Windows.Forms.DataGridView dgv_tabPsets;
        private System.Windows.Forms.DataGridView dgv_original;
        private System.Windows.Forms.Button btn_apply;
    }
}