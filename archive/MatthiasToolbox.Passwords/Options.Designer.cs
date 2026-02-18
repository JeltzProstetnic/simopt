namespace MatthiasToolbox.Passwords
{
    partial class Options
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Options));
            this.checkBoxPurge = new System.Windows.Forms.CheckBox();
            this.checkBox2Tray = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoXT = new System.Windows.Forms.CheckBox();
            this.textBoxAutoXTtime = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxIV = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonGenerate = new System.Windows.Forms.Button();
            this.radioButtonCPU = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.radioButtonMAC = new System.Windows.Forms.RadioButton();
            this.radioButtonHD = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBoxCPU = new System.Windows.Forms.TextBox();
            this.comboBoxHD = new System.Windows.Forms.ComboBox();
            this.comboBoxMAC = new System.Windows.Forms.ComboBox();
            this.radioButtonFile = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxPurge
            // 
            this.checkBoxPurge.AutoSize = true;
            this.checkBoxPurge.ForeColor = System.Drawing.Color.Black;
            this.checkBoxPurge.Location = new System.Drawing.Point(20, 28);
            this.checkBoxPurge.Name = "checkBoxPurge";
            this.checkBoxPurge.Size = new System.Drawing.Size(228, 17);
            this.checkBoxPurge.TabIndex = 0;
            this.checkBoxPurge.Text = "Ask for password, when restoring from tray.";
            this.checkBoxPurge.UseVisualStyleBackColor = true;
            // 
            // checkBox2Tray
            // 
            this.checkBox2Tray.AutoSize = true;
            this.checkBox2Tray.ForeColor = System.Drawing.Color.Black;
            this.checkBox2Tray.Location = new System.Drawing.Point(20, 51);
            this.checkBox2Tray.Name = "checkBox2Tray";
            this.checkBox2Tray.Size = new System.Drawing.Size(269, 17);
            this.checkBox2Tray.TabIndex = 1;
            this.checkBox2Tray.Text = "Close to tray, don\'t use maximize / minimize buttons.";
            this.checkBox2Tray.UseVisualStyleBackColor = true;
            // 
            // checkBoxAutoXT
            // 
            this.checkBoxAutoXT.AutoSize = true;
            this.checkBoxAutoXT.Enabled = false;
            this.checkBoxAutoXT.ForeColor = System.Drawing.Color.Black;
            this.checkBoxAutoXT.Location = new System.Drawing.Point(20, 74);
            this.checkBoxAutoXT.Name = "checkBoxAutoXT";
            this.checkBoxAutoXT.Size = new System.Drawing.Size(76, 17);
            this.checkBoxAutoXT.TabIndex = 2;
            this.checkBoxAutoXT.Text = "Close after";
            this.checkBoxAutoXT.UseVisualStyleBackColor = true;
            this.checkBoxAutoXT.CheckedChanged += new System.EventHandler(this.checkBoxAutoXT_CheckedChanged);
            // 
            // textBoxAutoXTtime
            // 
            this.textBoxAutoXTtime.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBoxAutoXTtime.Enabled = false;
            this.textBoxAutoXTtime.Location = new System.Drawing.Point(113, 71);
            this.textBoxAutoXTtime.Name = "textBoxAutoXTtime";
            this.textBoxAutoXTtime.Size = new System.Drawing.Size(26, 20);
            this.textBoxAutoXTtime.TabIndex = 3;
            this.textBoxAutoXTtime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Enabled = false;
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(145, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "minutes idle time.";
            // 
            // textBoxIV
            // 
            this.textBoxIV.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBoxIV.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxIV.Location = new System.Drawing.Point(14, 19);
            this.textBoxIV.Name = "textBoxIV";
            this.textBoxIV.ReadOnly = true;
            this.textBoxIV.Size = new System.Drawing.Size(234, 21);
            this.textBoxIV.TabIndex = 6;
            // 
            // buttonSave
            // 
            this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSave.Location = new System.Drawing.Point(228, 382);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 8;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonGenerate
            // 
            this.buttonGenerate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonGenerate.FlatAppearance.BorderColor = System.Drawing.Color.RoyalBlue;
            this.buttonGenerate.FlatAppearance.BorderSize = 0;
            this.buttonGenerate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.CornflowerBlue;
            this.buttonGenerate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonGenerate.ForeColor = System.Drawing.Color.RoyalBlue;
            this.buttonGenerate.Image = ((System.Drawing.Image)(resources.GetObject("buttonGenerate.Image")));
            this.buttonGenerate.Location = new System.Drawing.Point(254, 13);
            this.buttonGenerate.Name = "buttonGenerate";
            this.buttonGenerate.Size = new System.Drawing.Size(31, 31);
            this.buttonGenerate.TabIndex = 15;
            this.buttonGenerate.UseVisualStyleBackColor = true;
            this.buttonGenerate.Click += new System.EventHandler(this.buttonGenerate_Click);
            // 
            // radioButtonCPU
            // 
            this.radioButtonCPU.AutoSize = true;
            this.radioButtonCPU.ForeColor = System.Drawing.Color.Black;
            this.radioButtonCPU.Location = new System.Drawing.Point(14, 66);
            this.radioButtonCPU.Name = "radioButtonCPU";
            this.radioButtonCPU.Size = new System.Drawing.Size(61, 17);
            this.radioButtonCPU.TabIndex = 19;
            this.radioButtonCPU.Text = "CPU ID";
            this.radioButtonCPU.UseVisualStyleBackColor = true;
            this.radioButtonCPU.CheckedChanged += new System.EventHandler(this.radioButtonCPU_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.checkBoxPurge);
            this.groupBox1.Controls.Add(this.checkBox2Tray);
            this.groupBox1.Controls.Add(this.checkBoxAutoXT);
            this.groupBox1.Controls.Add(this.textBoxAutoXTtime);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(6, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(297, 130);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "User Interface";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Enabled = false;
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(145, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "minutes idle time.";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(113, 94);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(26, 20);
            this.textBox1.TabIndex = 6;
            this.textBox1.Text = "0";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Enabled = false;
            this.checkBox1.ForeColor = System.Drawing.Color.Black;
            this.checkBox1.Location = new System.Drawing.Point(20, 97);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(96, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Go to tray after";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // radioButtonMAC
            // 
            this.radioButtonMAC.AutoSize = true;
            this.radioButtonMAC.ForeColor = System.Drawing.Color.Black;
            this.radioButtonMAC.Location = new System.Drawing.Point(14, 89);
            this.radioButtonMAC.Name = "radioButtonMAC";
            this.radioButtonMAC.Size = new System.Drawing.Size(89, 17);
            this.radioButtonMAC.TabIndex = 21;
            this.radioButtonMAC.Text = "MAC Address";
            this.radioButtonMAC.UseVisualStyleBackColor = true;
            this.radioButtonMAC.CheckedChanged += new System.EventHandler(this.radioButtonMAC_CheckedChanged);
            // 
            // radioButtonHD
            // 
            this.radioButtonHD.AutoSize = true;
            this.radioButtonHD.ForeColor = System.Drawing.Color.Black;
            this.radioButtonHD.Location = new System.Drawing.Point(14, 112);
            this.radioButtonHD.Name = "radioButtonHD";
            this.radioButtonHD.Size = new System.Drawing.Size(60, 17);
            this.radioButtonHD.TabIndex = 22;
            this.radioButtonHD.Text = "Disc ID";
            this.radioButtonHD.UseVisualStyleBackColor = true;
            this.radioButtonHD.CheckedChanged += new System.EventHandler(this.radioButtonHD_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBoxCPU);
            this.groupBox2.Controls.Add(this.comboBoxHD);
            this.groupBox2.Controls.Add(this.comboBoxMAC);
            this.groupBox2.Controls.Add(this.radioButtonFile);
            this.groupBox2.Controls.Add(this.textBoxIV);
            this.groupBox2.Controls.Add(this.radioButtonHD);
            this.groupBox2.Controls.Add(this.buttonGenerate);
            this.groupBox2.Controls.Add(this.radioButtonMAC);
            this.groupBox2.Controls.Add(this.radioButtonCPU);
            this.groupBox2.ForeColor = System.Drawing.Color.White;
            this.groupBox2.Location = new System.Drawing.Point(6, 148);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(297, 140);
            this.groupBox2.TabIndex = 23;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Initialization Vector";
            // 
            // textBoxCPU
            // 
            this.textBoxCPU.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBoxCPU.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCPU.Location = new System.Drawing.Point(109, 64);
            this.textBoxCPU.Name = "textBoxCPU";
            this.textBoxCPU.ReadOnly = true;
            this.textBoxCPU.Size = new System.Drawing.Size(176, 21);
            this.textBoxCPU.TabIndex = 26;
            // 
            // comboBoxHD
            // 
            this.comboBoxHD.BackColor = System.Drawing.Color.LightSteelBlue;
            this.comboBoxHD.FormattingEnabled = true;
            this.comboBoxHD.Location = new System.Drawing.Point(109, 112);
            this.comboBoxHD.Name = "comboBoxHD";
            this.comboBoxHD.Size = new System.Drawing.Size(176, 21);
            this.comboBoxHD.TabIndex = 25;
            this.comboBoxHD.SelectedIndexChanged += new System.EventHandler(this.comboBoxHD_SelectedIndexChanged);
            // 
            // comboBoxMAC
            // 
            this.comboBoxMAC.BackColor = System.Drawing.Color.LightSteelBlue;
            this.comboBoxMAC.FormattingEnabled = true;
            this.comboBoxMAC.Location = new System.Drawing.Point(109, 88);
            this.comboBoxMAC.Name = "comboBoxMAC";
            this.comboBoxMAC.Size = new System.Drawing.Size(176, 21);
            this.comboBoxMAC.TabIndex = 24;
            this.comboBoxMAC.SelectedIndexChanged += new System.EventHandler(this.comboBoxMAC_SelectedIndexChanged);
            // 
            // radioButtonFile
            // 
            this.radioButtonFile.AutoSize = true;
            this.radioButtonFile.Checked = true;
            this.radioButtonFile.ForeColor = System.Drawing.Color.Black;
            this.radioButtonFile.Location = new System.Drawing.Point(14, 45);
            this.radioButtonFile.Name = "radioButtonFile";
            this.radioButtonFile.Size = new System.Drawing.Size(190, 17);
            this.radioButtonFile.TabIndex = 23;
            this.radioButtonFile.TabStop = true;
            this.radioButtonFile.Text = "Do not combine with Hardware IDs";
            this.radioButtonFile.UseVisualStyleBackColor = true;
            this.radioButtonFile.CheckedChanged += new System.EventHandler(this.radioButtonFile_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.comboBox1);
            this.groupBox3.Controls.Add(this.checkBox2);
            this.groupBox3.ForeColor = System.Drawing.Color.White;
            this.groupBox3.Location = new System.Drawing.Point(10, 305);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(293, 71);
            this.groupBox3.TabIndex = 24;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Keylogger Protection";
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Enabled = false;
            this.checkBox2.ForeColor = System.Drawing.Color.Black;
            this.checkBox2.Location = new System.Drawing.Point(16, 34);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(15, 14);
            this.checkBox2.TabIndex = 3;
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // comboBox1
            // 
            this.comboBox1.BackColor = System.Drawing.Color.LightSteelBlue;
            this.comboBox1.Enabled = false;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(37, 31);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(244, 21);
            this.comboBox1.TabIndex = 26;
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.CornflowerBlue;
            this.ClientSize = new System.Drawing.Size(315, 417);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Options";
            this.Text = "BlueLogic Secret Words - Options";
            this.Load += new System.EventHandler(this.Options_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxPurge;
        private System.Windows.Forms.CheckBox checkBox2Tray;
        private System.Windows.Forms.CheckBox checkBoxAutoXT;
        private System.Windows.Forms.TextBox textBoxAutoXTtime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxIV;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonGenerate;
        private System.Windows.Forms.RadioButton radioButtonCPU;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButtonMAC;
        private System.Windows.Forms.RadioButton radioButtonHD;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButtonFile;
        private System.Windows.Forms.ComboBox comboBoxHD;
        private System.Windows.Forms.ComboBox comboBoxMAC;
        private System.Windows.Forms.TextBox textBoxCPU;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.CheckBox checkBox2;
    }
}