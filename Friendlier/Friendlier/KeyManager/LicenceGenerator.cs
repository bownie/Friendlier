using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Xyglo;
using System.Diagnostics;

namespace KeyManager
{
    public partial class LicenceGenerator : Form
    {
        /// <summary>
        /// Instantiate the licence manager 
        /// </summary>
        LicenceManager m_licenceManager = new LicenceManager();

        protected string m_filePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\licences.txt";

        public LicenceGenerator()
        {
            InitializeComponent();

            // Initialise box
            //
            initialise();
        }

        /// <summary>
        /// Initialise the text
        /// </summary>
        protected void initialise()
        {
            // Set the selected item
            //
            encryptionComboBox.SelectedIndex = 0;
            licenceNumericUpDown.Value = 50;
            fillFromLicenceManager();
            setCheckBoxes();

            // Set this in the licence manager to keep updating
            //
            m_licenceManager.m_progressBar = toolStripProgressBar;
        }
       
        /// <summary>
        /// Fill text values from the licence manager
        /// </summary>
        protected void fillFromLicenceManager()
        {
            appNameTextBox.Text = m_licenceManager.m_appName;
            appVersiontextBox.Text = m_licenceManager.m_appVersion;
            sequenceNumberTextBox.Text = m_licenceManager.m_sequenceNumber.ToString();

            validFromCheckBox.Checked = m_licenceManager.m_isValidFromDate;
            validToCheckBox.Checked = m_licenceManager.m_isValidToDate;
            fromDateTimePicker.Value = DateTime.Now;
            toDateTimePicker.Value = DateTime.Now;

            passwordTextBox.Text = m_licenceManager.m_password;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Validate and store all the values to the licencemanager
        /// </summary>
        /// <returns></returns>
        protected bool validateButtons(bool loading = false)
        {
            // If loading only check the password
            if (loading)
            {

                if (passwordTextBox.Text == "")
                {
                    toolStripStatusLabel.Text = "Please fill password to decrypt.";
                    toolStripStatusLabel.ForeColor = Color.Red;
                    return false;
                }

                m_licenceManager.m_password = passwordTextBox.Text;
                return true;
            }


            // Store the latest values
            //
            if (appNameTextBox.Text == "")
            {
                toolStripStatusLabel.Text = "Please fill App Name.";
                toolStripStatusLabel.ForeColor = Color.Red;
                return false;
            }
            m_licenceManager.m_appName = appNameTextBox.Text;

            if (appVersiontextBox.Text == "")
            {
                toolStripStatusLabel.Text = "Please fill App Version.";
                toolStripStatusLabel.ForeColor = Color.Red;
                return false;
            }
            m_licenceManager.m_appVersion = appVersiontextBox.Text;

            try
            {
                m_licenceManager.m_sequenceNumber = Convert.ToInt32(sequenceNumberTextBox.Text);
            }
            catch (Exception)
            {
                if (sequenceNumberTextBox.Text == "")
                {
                    toolStripStatusLabel.Text = "Please fill sequence number.";
                }
                else
                {
                    toolStripStatusLabel.Text = "Sequence must be +ve integer.";
                }
                toolStripStatusLabel.ForeColor = Color.Red;
                sequenceNumberTextBox.ForeColor = Color.Red;
                return false;
            }

            if (passwordTextBox.Text == "")
            {
                toolStripStatusLabel.Text = "Please fill password.";
                toolStripStatusLabel.ForeColor = Color.Red;
                return false;
            }
            m_licenceManager.m_password = passwordTextBox.Text;
            m_licenceManager.m_isValidFromDate = validFromCheckBox.Checked;
            m_licenceManager.m_isValidToDate = validToCheckBox.Checked;
            m_licenceManager.m_validFromDate = fromDateTimePicker.Value;
            m_licenceManager.m_validToDate = toDateTimePicker.Value;

            // 
            switch (encryptionComboBox.SelectedItem.ToString())
            {
                case "RijndaelManaged":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.RijndaelManaged;
                    break;

                case "AES":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.AES;
                    break;

                case "DES":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.DES;
                    break;

                case "Blowfish":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.Blowfish;
                    break;

                default:
                    toolStripStatusLabel.Text = "Unknown encryption type";
                    toolStripStatusLabel.ForeColor = Color.Red;
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Set the value of the encryption in the licence manager based on drop down change
        /// </summary>
        protected void setEncryptionValue()
        {
            // 
            switch (encryptionComboBox.SelectedItem.ToString())
            {
                case "RijndaelManaged":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.RijndaelManaged;
                    break;

                case "AES":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.AES;
                    break;

                case "DES":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.DES;
                    break;

                case "Blowfish":
                    m_licenceManager.m_encryptionMethod = EncryptionMethod.Blowfish;
                    break;

                default:
                    toolStripStatusLabel.Text = "Unknown encryption type";
                    toolStripStatusLabel.ForeColor = Color.Red;
                    break;
            }
        }


        /// <summary>
        /// When we click on generate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void generateButton_Click(object sender, EventArgs e)
        {
            // Do the validation and set these in the licence generator if valid
            //
            if (!validateButtons())
            {
                return;
            }

            // now fetch number of keys to generate
            //
            int keysToGenerate = 0;

            try
            {
                keysToGenerate = Convert.ToInt32(licenceNumericUpDown.Value);
            }
            catch (Exception)
            {
                toolStripStatusLabel.Text = "Invalid number of licences.";
                toolStripStatusLabel.ForeColor = Color.Red;
                return;
            }

            // At this point we have passed validation
            //
            toolStripStatusLabel.ForeColor = Color.Black;
            toolStripStatusLabel.Text = "Generating keys...";

            // All done setting up the licence manager - generate the output file
            //
            try
            {
                m_licenceManager.generateLicences(keysToGenerate, m_filePath);
            }
            catch (Exception fe)
            {
                MessageBox.Show("Failed to write key file " + fe.Message);
                toolStripStatusLabel.Text = "Failure";
                toolStripStatusLabel.ForeColor = Color.Red;
                return;
            }

            // Set message and reset keys
            //
            toolStripStatusLabel.Text = keysToGenerate.ToString() + " keys generated.";

            // This will have been updated by the licencemanager
            //
            sequenceNumberTextBox.Text = m_licenceManager.m_sequenceNumber.ToString();

            MessageBox.Show("Licence file available at " + m_filePath);

            //toolStripProgressBar.Value = 0;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!validateButtons(true))
                {
                    return;
                }

                int linesLoaded = m_licenceManager.fetchHighestSequence(m_filePath);
                toolStripStatusLabel.Text = linesLoaded.ToString() + " keys loaded.";

                string text = "Loaded " + linesLoaded + " keys and got a high sequence number of " + m_licenceManager.m_possibleLicenceDetails.highValue + " with details:\n\n";
                text += "      App Name    : " + m_licenceManager.m_possibleLicenceDetails.appName + "\n";
                text += "      App Version : " + m_licenceManager.m_possibleLicenceDetails.appVersion + "\n";
                text += "      From Date   : " + m_licenceManager.m_possibleLicenceDetails.validFromDate + "\n";
                text += "      To Date     : " + m_licenceManager.m_possibleLicenceDetails.validToDate + "\n\n\n";
                text += "Do you want to load these values and set the sequence one above this value?";
                DialogResult result = MessageBox.Show(text, "Load values from file?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    m_licenceManager.setPossibleValues();
                    fillFromLicenceManager();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Handle callback from selection on combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void encryptionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setEncryptionValue();
        }

        protected void setCheckBoxes()
        {
            toDateTimePicker.Enabled = validToCheckBox.Checked;
            fromDateTimePicker.Enabled = validFromCheckBox.Checked;
        }

        private void validFromCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            setCheckBoxes();
        }

        private void validToCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            setCheckBoxes();
        }

        private void LicenceGenerator_Load(object sender, EventArgs e)
        {

        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Licence Generator brought to you by @xyglo", "About");
        }

        private void fromDateTimePicker_ValueChanged(object sender, EventArgs e)
        {

        }

        private void toDateTimePicker_ValueChanged(object sender, EventArgs e)
        {

        }

        private void appVersiontextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void sequenceNumberTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void licenceNumericUpDown_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Get some code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void getCodeButton_Click(object sender, EventArgs e)
        {
            Process.Start("ClientDecrypt.txt");
        }

        private void appNameTextBox_TextChanged(object sender, EventArgs e)
        {
        }

    }
}
