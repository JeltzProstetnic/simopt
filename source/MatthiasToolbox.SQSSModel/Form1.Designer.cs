namespace MatthiasToolbox.SQSSModel
{
    partial class Form1
    {
#pragma warning disable 0649
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components;
#pragma warning restore 0649

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
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBoxLog = new System.Windows.Forms.CheckBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdLoad = new System.Windows.Forms.Button();
            this.cmdInitSimulation = new System.Windows.Forms.Button();
            this.cmdStart = new System.Windows.Forms.Button();
            this.cmdContinue = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.Location = new System.Drawing.Point(12, 12);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(1101, 539);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(1038, 557);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Start";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(957, 557);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Stop";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkBoxLog
            // 
            this.checkBoxLog.AutoSize = true;
            this.checkBoxLog.Location = new System.Drawing.Point(12, 563);
            this.checkBoxLog.Name = "checkBoxLog";
            this.checkBoxLog.Size = new System.Drawing.Size(95, 17);
            this.checkBoxLog.TabIndex = 3;
            this.checkBoxLog.Text = "Event Logging";
            this.checkBoxLog.UseVisualStyleBackColor = true;
            // 
            // cmdSave
            // 
            this.cmdSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdSave.Location = new System.Drawing.Point(364, 559);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.TabIndex = 4;
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            this.cmdSave.Click += new System.EventHandler(this.cmdSave_Click);
            // 
            // cmdLoad
            // 
            this.cmdLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdLoad.Location = new System.Drawing.Point(446, 559);
            this.cmdLoad.Name = "cmdLoad";
            this.cmdLoad.Size = new System.Drawing.Size(75, 23);
            this.cmdLoad.TabIndex = 5;
            this.cmdLoad.Text = "Load";
            this.cmdLoad.UseVisualStyleBackColor = true;
            this.cmdLoad.Click += new System.EventHandler(this.cmdLoad_Click);
            // 
            // cmdInitSimulation
            // 
            this.cmdInitSimulation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdInitSimulation.Location = new System.Drawing.Point(284, 559);
            this.cmdInitSimulation.Name = "cmdInitSimulation";
            this.cmdInitSimulation.Size = new System.Drawing.Size(75, 23);
            this.cmdInitSimulation.TabIndex = 6;
            this.cmdInitSimulation.Text = "Init Simulation";
            this.cmdInitSimulation.UseVisualStyleBackColor = true;
            this.cmdInitSimulation.Click += new System.EventHandler(this.cmdInitSimulation_Click);
            // 
            // cmdStart
            // 
            this.cmdStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdStart.Location = new System.Drawing.Point(526, 557);
            this.cmdStart.Name = "cmdStart";
            this.cmdStart.Size = new System.Drawing.Size(75, 23);
            this.cmdStart.TabIndex = 7;
            this.cmdStart.Text = "Start";
            this.cmdStart.UseVisualStyleBackColor = true;
            this.cmdStart.Click += new System.EventHandler(this.cmdStart_Click);
            // 
            // cmdContinue
            // 
            this.cmdContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdContinue.Location = new System.Drawing.Point(608, 559);
            this.cmdContinue.Name = "cmdContinue";
            this.cmdContinue.Size = new System.Drawing.Size(75, 23);
            this.cmdContinue.TabIndex = 8;
            this.cmdContinue.Text = "Continue";
            this.cmdContinue.UseVisualStyleBackColor = true;
            this.cmdContinue.Click += new System.EventHandler(this.cmdContinue_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1122, 589);
            this.Controls.Add(this.cmdContinue);
            this.Controls.Add(this.cmdStart);
            this.Controls.Add(this.cmdInitSimulation);
            this.Controls.Add(this.cmdLoad);
            this.Controls.Add(this.cmdSave);
            this.Controls.Add(this.checkBoxLog);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox1);
            this.Name = "Form1";
            this.Text = "SQSS Demo";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBoxLog;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdLoad;
        private System.Windows.Forms.Button cmdInitSimulation;
        private System.Windows.Forms.Button cmdStart;
        private System.Windows.Forms.Button cmdContinue;
    }
}

