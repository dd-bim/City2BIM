namespace City2RVT.GUI.Modify
{
    partial class editIfcProperties
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
            this.propertyListBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // propertyListBox
            // 
            this.propertyListBox.FormattingEnabled = true;
            this.propertyListBox.Location = new System.Drawing.Point(12, 12);
            this.propertyListBox.Name = "propertyListBox";
            this.propertyListBox.Size = new System.Drawing.Size(308, 316);
            this.propertyListBox.TabIndex = 0;
            this.propertyListBox.SelectedIndexChanged += new System.EventHandler(this.propertyListBox_SelectedIndexChanged);
            // 
            // editIfcProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(342, 380);
            this.Controls.Add(this.propertyListBox);
            this.Name = "editIfcProperties";
            this.Text = "editIfcProperties";
            this.Load += new System.EventHandler(this.editIfcProperties_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox propertyListBox;
    }
}