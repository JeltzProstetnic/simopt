namespace SignalSplitter
{
    partial class MainForm
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
            this.wavechart1 = new Accord.Controls.Wavechart();
            this.wavechart2 = new Accord.Controls.Wavechart();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // wavechart1
            // 
            this.wavechart1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.wavechart1.Location = new System.Drawing.Point(12, 12);
            this.wavechart1.Name = "wavechart1";
            this.wavechart1.Size = new System.Drawing.Size(558, 169);
            this.wavechart1.TabIndex = 0;
            this.wavechart1.Text = "wavechart1";
            // 
            // wavechart2
            // 
            this.wavechart2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.wavechart2.Location = new System.Drawing.Point(12, 187);
            this.wavechart2.Name = "wavechart2";
            this.wavechart2.Size = new System.Drawing.Size(477, 63);
            this.wavechart2.TabIndex = 1;
            this.wavechart2.Text = "wavechart2";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(495, 187);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 63);
            this.button1.TabIndex = 2;
            this.button1.Text = "Separate";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 262);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.wavechart2);
            this.Controls.Add(this.wavechart1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Accord.Controls.Wavechart wavechart1;
        private Accord.Controls.Wavechart wavechart2;
        private System.Windows.Forms.Button button1;
    }
}

