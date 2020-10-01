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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wf_showProperties));
            this.btn_apply = new System.Windows.Forms.Button();
            this.btn_reset = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dgv_showProperties = new System.Windows.Forms.DataGridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dgv_zukunftBau = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.btn_close = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_showProperties)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_zukunftBau)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_apply
            // 
            this.btn_apply.Location = new System.Drawing.Point(558, 47);
            this.btn_apply.Name = "btn_apply";
            this.btn_apply.Size = new System.Drawing.Size(75, 23);
            this.btn_apply.TabIndex = 1;
            this.btn_apply.Text = "Apply";
            this.btn_apply.UseVisualStyleBackColor = true;
            this.btn_apply.Click += new System.EventHandler(this.btn_apply_Click);
            // 
            // btn_reset
            // 
            this.btn_reset.Location = new System.Drawing.Point(558, 92);
            this.btn_reset.Name = "btn_reset";
            this.btn_reset.Size = new System.Drawing.Size(75, 23);
            this.btn_reset.TabIndex = 2;
            this.btn_reset.Text = "Reset";
            this.btn_reset.UseVisualStyleBackColor = true;
            this.btn_reset.Click += new System.EventHandler(this.btn_reset_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(25, 25);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(500, 375);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dgv_showProperties);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(492, 349);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "XPlanung";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // dgv_showProperties
            // 
            this.dgv_showProperties.AllowUserToAddRows = false;
            this.dgv_showProperties.AllowUserToDeleteRows = false;
            this.dgv_showProperties.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_showProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_showProperties.Location = new System.Drawing.Point(0, -20);
            this.dgv_showProperties.Margin = new System.Windows.Forms.Padding(2);
            this.dgv_showProperties.Name = "dgv_showProperties";
            this.dgv_showProperties.RowHeadersVisible = false;
            this.dgv_showProperties.RowHeadersWidth = 82;
            this.dgv_showProperties.RowTemplate.Height = 33;
            this.dgv_showProperties.Size = new System.Drawing.Size(496, 370);
            this.dgv_showProperties.TabIndex = 1;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dgv_zukunftBau);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(492, 349);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "ZukunftBau";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgv_zukunftBau
            // 
            this.dgv_zukunftBau.AllowUserToAddRows = false;
            this.dgv_zukunftBau.AllowUserToDeleteRows = false;
            this.dgv_zukunftBau.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_zukunftBau.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_zukunftBau.Location = new System.Drawing.Point(0, -20);
            this.dgv_zukunftBau.Name = "dgv_zukunftBau";
            this.dgv_zukunftBau.RowHeadersVisible = false;
            this.dgv_zukunftBau.Size = new System.Drawing.Size(500, 370);
            this.dgv_zukunftBau.TabIndex = 0;
            this.dgv_zukunftBau.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_zukunftBau_CellContentClick);
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(492, 349);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "ALKIS";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // btn_close
            // 
            this.btn_close.Location = new System.Drawing.Point(558, 374);
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(75, 23);
            this.btn_close.TabIndex = 4;
            this.btn_close.Text = "Close";
            this.btn_close.UseVisualStyleBackColor = true;
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            // 
            // Wf_showProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(667, 421);
            this.Controls.Add(this.btn_close);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btn_reset);
            this.Controls.Add(this.btn_apply);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Wf_showProperties";
            this.Text = "Wf_showProperties";
            this.Load += new System.EventHandler(this.Wf_showProperties_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_showProperties)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_zukunftBau)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btn_apply;
        private System.Windows.Forms.Button btn_reset;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.DataGridView dgv_showProperties;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dgv_zukunftBau;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Button btn_close;
    }
}