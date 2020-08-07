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
            this.pickElement = new System.Windows.Forms.Button();
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
            // pickElement
            // 
            this.pickElement.Location = new System.Drawing.Point(13, 334);
            this.pickElement.Name = "pickElement";
            this.pickElement.Size = new System.Drawing.Size(101, 34);
            this.pickElement.TabIndex = 1;
            this.pickElement.Text = "Select in view";
            this.pickElement.UseVisualStyleBackColor = true;
            this.pickElement.Click += new System.EventHandler(this.pickElement_Click);
            // 
            // editIfcProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(342, 380);
            this.Controls.Add(this.pickElement);
            this.Controls.Add(this.propertyListBox);
            this.Name = "editIfcProperties";
            this.Text = "editIfcProperties";
            this.Load += new System.EventHandler(this.editIfcProperties_Load);
            this.VisibleChanged += new System.EventHandler(this.editIfcProperties_VisibleChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox propertyListBox;
        private System.Windows.Forms.Button pickElement;
    }
}