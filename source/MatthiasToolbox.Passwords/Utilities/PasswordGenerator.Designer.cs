namespace MatthiasToolbox.Passwords
{
    partial class PasswordGenerator
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PasswordGenerator));
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBoxResult = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            this.SuspendLayout();
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.BackColor = System.Drawing.Color.LightSteelBlue;
            this.numericUpDown1.Location = new System.Drawing.Point(67, 160);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(39, 20);
            this.numericUpDown1.TabIndex = 0;
            this.numericUpDown1.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(11, 14);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(99, 17);
            this.checkBox1.TabIndex = 1;
            this.checkBox1.Text = "use uppercase:";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(11, 37);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(97, 17);
            this.checkBox2.TabIndex = 2;
            this.checkBox2.Text = "use lowercase:";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Checked = true;
            this.checkBox3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox3.Location = new System.Drawing.Point(11, 60);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(89, 17);
            this.checkBox3.TabIndex = 3;
            this.checkBox3.Text = "use numbers:";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Checked = true;
            this.checkBox4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox4.Location = new System.Drawing.Point(11, 83);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(111, 17);
            this.checkBox4.TabIndex = 4;
            this.checkBox4.Text = "use special chars:";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(15, 162);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 14);
            this.label1.TabIndex = 5;
            this.label1.Text = "Length";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBox1.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(119, 81);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(290, 20);
            this.textBox1.TabIndex = 6;
            this.textBox1.Text = "°!\"§$%&/()=?`´ß}][{³²^@*+\'#~-_.:,;µ<>|\\";
            // 
            // textBoxResult
            // 
            this.textBoxResult.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBoxResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResult.Location = new System.Drawing.Point(11, 117);
            this.textBoxResult.Name = "textBoxResult";
            this.textBoxResult.Size = new System.Drawing.Size(317, 26);
            this.textBoxResult.TabIndex = 7;
            this.textBoxResult.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(334, 154);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 28);
            this.button1.TabIndex = 8;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(334, 117);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 26);
            this.button2.TabIndex = 9;
            this.button2.Text = "Generate";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(135, 162);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 14);
            this.label2.TabIndex = 11;
            this.label2.Text = "Minimal Strength";
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.BackColor = System.Drawing.Color.LightSteelBlue;
            this.numericUpDown2.Location = new System.Drawing.Point(242, 158);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.numericUpDown2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(39, 20);
            this.numericUpDown2.TabIndex = 10;
            this.numericUpDown2.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // textBox3
            // 
            this.textBox3.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBox3.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox3.Location = new System.Drawing.Point(119, 57);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(290, 20);
            this.textBox3.TabIndex = 12;
            this.textBox3.Text = "1234567890";
            // 
            // textBox4
            // 
            this.textBox4.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBox4.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox4.Location = new System.Drawing.Point(119, 34);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(290, 20);
            this.textBox4.TabIndex = 13;
            this.textBox4.Text = "abcdefghijklmnopqrstuvwxyz";
            // 
            // textBox5
            // 
            this.textBox5.BackColor = System.Drawing.Color.LightSteelBlue;
            this.textBox5.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox5.Location = new System.Drawing.Point(119, 12);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(290, 20);
            this.textBox5.TabIndex = 14;
            this.textBox5.Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            // 
            // PasswordGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.CornflowerBlue;
            this.ClientSize = new System.Drawing.Size(421, 192);
            this.Controls.Add(this.textBox5);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numericUpDown2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.textBoxResult);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBox4);
            this.Controls.Add(this.checkBox3);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.numericUpDown1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PasswordGenerator";
            this.Opacity = 0.9;
            this.Text = "BlueLogic Secret Words - Password Generator";
            this.Load += new System.EventHandler(this.PasswordGenerator_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.TextBox textBoxResult;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.TextBox textBox5;
    }
}