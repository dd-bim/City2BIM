namespace City2RVT.GUI.Properties
{
    partial class Wf_showProperties
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
            this.dgv_showProperties = new System.Windows.Forms.DataGridView();
            this.btn_apply = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_showProperties)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv_showProperties
            // 
            this.dgv_showProperties.AllowUserToAddRows = false;
            this.dgv_showProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_showProperties.Location = new System.Drawing.Point(25, 24);
            this.dgv_showProperties.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dgv_showProperties.Name = "dgv_showProperties";
            this.dgv_showProperties.RowHeadersWidth = 82;
            this.dgv_showProperties.RowTemplate.Height = 33;
            this.dgv_showProperties.Size = new System.Drawing.Size(531, 359);
            this.dgv_showProperties.TabIndex = 0;
            this.dgv_showProperties.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_showProperties_CellContentClick);
            // 
            // btn_apply
            // 
            this.btn_apply.Location = new System.Drawing.Point(615, 52);
            this.btn_apply.Name = "btn_apply";
            this.btn_apply.Size = new System.Drawing.Size(75, 23);
            this.btn_apply.TabIndex = 1;
            this.btn_apply.Text = "Apply";
            this.btn_apply.UseVisualStyleBackColor = true;
            this.btn_apply.Click += new System.EventHandler(this.btn_apply_Click);
            // 
            // Wf_showProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(744, 453);
            this.Controls.Add(this.btn_apply);
            this.Controls.Add(this.dgv_showProperties);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Wf_showProperties";
            this.Text = "Wf_showProperties";
            this.Load += new System.EventHandler(this.Wf_showProperties_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_showProperties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_showProperties;
        private System.Windows.Forms.Button btn_apply;
    }
}