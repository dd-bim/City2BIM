namespace City2RVT.GUI.Modify
{
    partial class IfcPropertySets
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
            this.PropertySetsListbox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // PropertySetsListbox
            // 
            this.PropertySetsListbox.FormattingEnabled = true;
            this.PropertySetsListbox.Location = new System.Drawing.Point(11, 11);
            this.PropertySetsListbox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.PropertySetsListbox.Name = "PropertySetsListbox";
            this.PropertySetsListbox.Size = new System.Drawing.Size(232, 186);
            this.PropertySetsListbox.TabIndex = 0;
            this.PropertySetsListbox.SelectedIndexChanged += new System.EventHandler(this.PropertySetsListbox_SelectedIndexChanged);
            // 
            // IfcPropertySets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(275, 254);
            this.Controls.Add(this.PropertySetsListbox);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "IfcPropertySets";
            this.Text = "IfcPropertySets";
            this.Load += new System.EventHandler(this.IfcPropertySets_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox PropertySetsListbox;
    }
}