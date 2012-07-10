namespace KeyManager
{
    partial class LicenceGenerator
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
            this.appNameTextBox = new System.Windows.Forms.TextBox();
            this.fromDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.validFromCheckBox = new System.Windows.Forms.CheckBox();
            this.validToCheckBox = new System.Windows.Forms.CheckBox();
            this.toDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.appVersiontextBox = new System.Windows.Forms.TextBox();
            this.sequenceNumberTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.encryptionComboBox = new System.Windows.Forms.ComboBox();
            this.licenceNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.generateButton = new System.Windows.Forms.Button();
            this.loadButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.licenceNumericUpDown)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // appNameTextBox
            // 
            this.appNameTextBox.Location = new System.Drawing.Point(204, 6);
            this.appNameTextBox.Name = "appNameTextBox";
            this.appNameTextBox.Size = new System.Drawing.Size(103, 20);
            this.appNameTextBox.TabIndex = 0;
            this.appNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // fromDateTimePicker
            // 
            this.fromDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.fromDateTimePicker.Location = new System.Drawing.Point(204, 84);
            this.fromDateTimePicker.Name = "fromDateTimePicker";
            this.fromDateTimePicker.Size = new System.Drawing.Size(103, 20);
            this.fromDateTimePicker.TabIndex = 4;
            // 
            // validFromCheckBox
            // 
            this.validFromCheckBox.AutoSize = true;
            this.validFromCheckBox.Location = new System.Drawing.Point(106, 87);
            this.validFromCheckBox.Name = "validFromCheckBox";
            this.validFromCheckBox.Size = new System.Drawing.Size(92, 17);
            this.validFromCheckBox.TabIndex = 3;
            this.validFromCheckBox.Text = "Key valid from";
            this.validFromCheckBox.UseVisualStyleBackColor = true;
            this.validFromCheckBox.CheckedChanged += new System.EventHandler(this.validFromCheckBox_CheckedChanged);
            // 
            // validToCheckBox
            // 
            this.validToCheckBox.AutoSize = true;
            this.validToCheckBox.Location = new System.Drawing.Point(106, 115);
            this.validToCheckBox.Name = "validToCheckBox";
            this.validToCheckBox.Size = new System.Drawing.Size(81, 17);
            this.validToCheckBox.TabIndex = 5;
            this.validToCheckBox.Text = "Key valid to";
            this.validToCheckBox.UseVisualStyleBackColor = true;
            this.validToCheckBox.CheckedChanged += new System.EventHandler(this.validToCheckBox_CheckedChanged);
            // 
            // toDateTimePicker
            // 
            this.toDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.toDateTimePicker.Location = new System.Drawing.Point(204, 112);
            this.toDateTimePicker.Name = "toDateTimePicker";
            this.toDateTimePicker.Size = new System.Drawing.Size(103, 20);
            this.toDateTimePicker.TabIndex = 6;
            // 
            // appVersiontextBox
            // 
            this.appVersiontextBox.Location = new System.Drawing.Point(204, 32);
            this.appVersiontextBox.Name = "appVersiontextBox";
            this.appVersiontextBox.Size = new System.Drawing.Size(103, 20);
            this.appVersiontextBox.TabIndex = 1;
            this.appVersiontextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // sequenceNumberTextBox
            // 
            this.sequenceNumberTextBox.Location = new System.Drawing.Point(204, 58);
            this.sequenceNumberTextBox.Name = "sequenceNumberTextBox";
            this.sequenceNumberTextBox.Size = new System.Drawing.Size(103, 20);
            this.sequenceNumberTextBox.TabIndex = 2;
            this.sequenceNumberTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "App name";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "App version";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(131, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Starting sequence number";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(204, 138);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(103, 20);
            this.passwordTextBox.TabIndex = 7;
            this.passwordTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 141);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Password";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 167);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(57, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Encryption";
            // 
            // encryptionComboBox
            // 
            this.encryptionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.encryptionComboBox.FormattingEnabled = true;
            this.encryptionComboBox.Items.AddRange(new object[] {
            "DES"});
            this.encryptionComboBox.Location = new System.Drawing.Point(186, 164);
            this.encryptionComboBox.Name = "encryptionComboBox";
            this.encryptionComboBox.Size = new System.Drawing.Size(121, 21);
            this.encryptionComboBox.TabIndex = 8;
            this.encryptionComboBox.SelectedIndexChanged += new System.EventHandler(this.encryptionComboBox_SelectedIndexChanged);
            // 
            // licenceNumericUpDown
            // 
            this.licenceNumericUpDown.Location = new System.Drawing.Point(187, 191);
            this.licenceNumericUpDown.Name = "licenceNumericUpDown";
            this.licenceNumericUpDown.Size = new System.Drawing.Size(120, 20);
            this.licenceNumericUpDown.TabIndex = 9;
            this.licenceNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 193);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(102, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "Number of Licences";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar,
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 273);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(321, 22);
            this.statusStrip.TabIndex = 20;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripProgressBar
            // 
            this.toolStripProgressBar.Name = "toolStripProgressBar";
            this.toolStripProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.AutoSize = false;
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(200, 17);
            this.toolStripStatusLabel.Text = "Ready";
            this.toolStripStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolStripStatusLabel.Click += new System.EventHandler(this.toolStripStatusLabel_Click);
            // 
            // generateButton
            // 
            this.generateButton.Location = new System.Drawing.Point(189, 234);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(120, 23);
            this.generateButton.TabIndex = 11;
            this.generateButton.Text = "Generate Licences";
            this.generateButton.UseVisualStyleBackColor = true;
            this.generateButton.Click += new System.EventHandler(this.generateButton_Click);
            // 
            // loadButton
            // 
            this.loadButton.Location = new System.Drawing.Point(62, 234);
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(121, 23);
            this.loadButton.TabIndex = 10;
            this.loadButton.Text = "Load Licences";
            this.loadButton.UseVisualStyleBackColor = true;
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // LicenceGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(321, 295);
            this.Controls.Add(this.generateButton);
            this.Controls.Add(this.loadButton);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.licenceNumericUpDown);
            this.Controls.Add(this.encryptionComboBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.sequenceNumberTextBox);
            this.Controls.Add(this.appVersiontextBox);
            this.Controls.Add(this.toDateTimePicker);
            this.Controls.Add(this.validToCheckBox);
            this.Controls.Add(this.validFromCheckBox);
            this.Controls.Add(this.fromDateTimePicker);
            this.Controls.Add(this.appNameTextBox);
            this.Name = "LicenceGenerator";
            this.Text = "Xyglo Licence Generator";
            ((System.ComponentModel.ISupportInitialize)(this.licenceNumericUpDown)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox appNameTextBox;
        private System.Windows.Forms.DateTimePicker fromDateTimePicker;
        private System.Windows.Forms.CheckBox validFromCheckBox;
        private System.Windows.Forms.CheckBox validToCheckBox;
        private System.Windows.Forms.DateTimePicker toDateTimePicker;
        private System.Windows.Forms.TextBox appVersiontextBox;
        private System.Windows.Forms.TextBox sequenceNumberTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox encryptionComboBox;
        private System.Windows.Forms.NumericUpDown licenceNumericUpDown;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.Button generateButton;
        private System.Windows.Forms.Button loadButton;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
    }
}

