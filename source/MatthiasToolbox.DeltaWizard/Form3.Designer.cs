namespace MatthiasToolbox.DeltaWizard
{
    partial class Form3
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form3));
            this.panel3 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBox7 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.SuspendLayout();
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.button2);
            this.panel3.Controls.Add(this.button1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 313);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(639, 62);
            this.panel3.TabIndex = 5;
            // 
            // button2
            // 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Retry;
            this.button2.Location = new System.Drawing.Point(453, 15);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(84, 33);
            this.button2.TabIndex = 1;
            this.button2.Text = "<< Zurück";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(543, 15);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 33);
            this.button1.TabIndex = 0;
            this.button1.Text = "Weiter >>";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel5);
            this.panel2.Controls.Add(this.textBox10);
            this.panel2.Controls.Add(this.textBox9);
            this.panel2.Controls.Add(this.textBox8);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.checkBox7);
            this.panel2.Controls.Add(this.checkBox5);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 105);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(639, 270);
            this.panel2.TabIndex = 4;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.label8);
            this.panel5.Controls.Add(this.textBox2);
            this.panel5.Controls.Add(this.label2);
            this.panel5.Controls.Add(this.comboBox1);
            this.panel5.Controls.Add(this.label7);
            this.panel5.Controls.Add(this.textBox6);
            this.panel5.Controls.Add(this.label3);
            this.panel5.Controls.Add(this.textBox1);
            this.panel5.Location = new System.Drawing.Point(13, 14);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(614, 161);
            this.panel5.TabIndex = 45;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(14, 127);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(97, 13);
            this.label8.TabIndex = 50;
            this.label8.Text = "Maximalgröße (MB)";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(141, 124);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(308, 20);
            this.textBox2.TabIndex = 49;
            this.textBox2.Text = "10";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 13);
            this.label2.TabIndex = 48;
            this.label2.Text = "Digest Funktion";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(141, 13);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(308, 21);
            this.comboBox1.TabIndex = 47;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 90);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 13);
            this.label7.TabIndex = 46;
            this.label7.Text = "Mindestgröße (byte)";
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(141, 87);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(308, 20);
            this.textBox6.TabIndex = 45;
            this.textBox6.Text = "1";
            this.textBox6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox6.TextChanged += new System.EventHandler(this.textBox6_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 53);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 13);
            this.label3.TabIndex = 43;
            this.label3.Text = "Blockgröße (byte)";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(141, 50);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(308, 20);
            this.textBox1.TabIndex = 41;
            this.textBox1.Text = "2048";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged_1);
            // 
            // textBox10
            // 
            this.textBox10.Enabled = false;
            this.textBox10.Location = new System.Drawing.Point(494, 181);
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new System.Drawing.Size(43, 20);
            this.textBox10.TabIndex = 36;
            this.textBox10.Text = "100";
            this.textBox10.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox10.Visible = false;
            this.textBox10.TextChanged += new System.EventHandler(this.textBox10_TextChanged);
            // 
            // textBox9
            // 
            this.textBox9.Enabled = false;
            this.textBox9.Location = new System.Drawing.Point(380, 182);
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new System.Drawing.Size(37, 20);
            this.textBox9.TabIndex = 35;
            this.textBox9.Text = "10000";
            this.textBox9.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox9.Visible = false;
            this.textBox9.TextChanged += new System.EventHandler(this.textBox9_TextChanged);
            // 
            // textBox8
            // 
            this.textBox8.Enabled = false;
            this.textBox8.Location = new System.Drawing.Point(301, 182);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(37, 20);
            this.textBox8.TabIndex = 34;
            this.textBox8.Text = "100";
            this.textBox8.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox8.Visible = false;
            this.textBox8.TextChanged += new System.EventHandler(this.textBox8_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Enabled = false;
            this.label6.Location = new System.Drawing.Point(426, 184);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 33;
            this.label6.Text = "Schrittweite";
            this.label6.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Enabled = false;
            this.label5.Location = new System.Drawing.Point(354, 184);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(20, 13);
            this.label5.TabIndex = 32;
            this.label5.Text = "bis";
            this.label5.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Enabled = false;
            this.label4.Location = new System.Drawing.Point(270, 184);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(25, 13);
            this.label4.TabIndex = 31;
            this.label4.Text = "von";
            this.label4.Visible = false;
            // 
            // checkBox7
            // 
            this.checkBox7.AutoSize = true;
            this.checkBox7.Enabled = false;
            this.checkBox7.Location = new System.Drawing.Point(146, 183);
            this.checkBox7.Name = "checkBox7";
            this.checkBox7.Size = new System.Drawing.Size(118, 17);
            this.checkBox7.TabIndex = 30;
            this.checkBox7.Text = "Blockgrößen testen";
            this.checkBox7.UseVisualStyleBackColor = true;
            this.checkBox7.Visible = false;
            this.checkBox7.CheckedChanged += new System.EventHandler(this.checkBox7_CheckedChanged);
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(27, 183);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(109, 17);
            this.checkBox5.TabIndex = 29;
            this.checkBox5.Text = "Statistik mitführen";
            this.checkBox5.UseVisualStyleBackColor = true;
            this.checkBox5.Visible = false;
            this.checkBox5.CheckedChanged += new System.EventHandler(this.checkBox5_CheckedChanged);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(373, 64);
            this.label1.TabIndex = 14;
            this.label1.Text = "Einstellungen:";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.pictureBox3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(639, 105);
            this.panel1.TabIndex = 3;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox3.InitialImage")));
            this.pictureBox3.Location = new System.Drawing.Point(563, 15);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(64, 64);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox3.TabIndex = 22;
            this.pictureBox3.TabStop = false;
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 375);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form3";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "SDelta 1.0";
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.TextBox textBox10;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.TextBox textBox8;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox7;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox2;
    }
}