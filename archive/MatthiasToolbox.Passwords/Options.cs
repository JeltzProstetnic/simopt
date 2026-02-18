using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Security;
using System.Text;
using System.Windows.Forms;
using MatthiasToolbox.Passwords.Utilities;
using Hashtable=System.Collections.Hashtable;

namespace MatthiasToolbox.Passwords
{
    public partial class Options : Form
    {
        private byte[] sIV = Encoding.UTF8.GetBytes("BlueLogicSoftWar");
        private bool Loading = true;
        private byte[] tmpIV;
        private Config.IVMethod tmpIVmode;
        
        public Options()
        {
                    InitializeComponent();
            
                tmpIV = Program.confData.IV;
            tmpIVmode = Program.confData.IVmode;
            
            textBoxCPU.Text = HardwareInfo.GetCPUId();
        }

        private void Options_Load(object sender, EventArgs e)
        {
            // init gui section
            checkBoxPurge.Checked = Program.confData.purgePwd;
            checkBox2Tray.Checked = Program.confData.closeToTray;
            checkBoxAutoXT.Checked = (Program.confData.trayAutoClose > 0);
            textBoxAutoXTtime.Text = Program.confData.trayAutoClose.ToString();
            
            // init IV & protection section
            switch(Program.confData.IVmode)
            {
                // redundant
                //case Config.IVMethod.file:
                //    radioButtonFile.Checked = true;
                //    break;
                case Config.IVMethod.cpu:
                    radioButtonCPU.Checked = true;
                    break;
                case Config.IVMethod.mac:
                    radioButtonMAC.Checked = true;
                    break;
                case Config.IVMethod.hd:
                    radioButtonHD.Checked = true;
                    break;
            }

            checkBox2.Checked = !String.IsNullOrEmpty(Program.confData.Protection);
            
            String selectedProtection = "";
            bool goon = true;
            while(goon)
            {
                goon = false;
                comboBoxHD.Items.Clear();
                HardwareInfo.UpdateUSBDevices();
                String selectedDrive = "";
                foreach (String drive in Environment.GetLogicalDrives())
                {
                    String d = HardwareInfo.GetVolumeSerial(drive.Substring(0, 1));
                    if (d != "")
                    {
                        String item = drive + d;
                        comboBoxHD.Items.Add(item);
                        if (Program.confData.IVmode == Config.IVMethod.hd &&
                            Crypto.ComputeHash(d, "SHA512", sIV) == Program.confData.Drive) 
                            selectedDrive = item;
                        if (Program.confData.Protection != "" && 
                            Crypto.ComputeHash(d, "SHA512", sIV) == Program.confData.Protection)
                            selectedProtection = item;
                    }
                }
                if (Program.confData.IVmode == Config.IVMethod.hd)
                {
                    comboBoxHD.SelectedItem = selectedDrive;
                    if(comboBoxHD.SelectedIndex == -1)
                    {
                        if(MessageBox.Show("IV device not found. Please connect the IV device and retry.","Error",MessageBoxButtons.RetryCancel,MessageBoxIcon.Warning)==DialogResult.Cancel)
                        {
                            Close();
                            return;
                        }
                        goon = true;
                    }
                }
                else if (Program.confData.Protection != "")
                {
                    if (selectedProtection == "")
                    {
                        if (MessageBox.Show("Protection device not found. Please connect the IV device and retry.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                        {
                            Close();
                            return;
                        }
                        goon = true;
                    }
                }
                
            }

            foreach (DictionaryEntry entry in HardwareInfo.usb)
            {
                comboBox1.Items.Add(entry.Key.ToString() + ":\\" + entry.Value.ToString());
                checkBox2.Enabled = true;
            }
            comboBox1.SelectedItem = selectedProtection;
                
            foreach (string s in HardwareInfo.GetAdapters())
            {
                comboBoxMAC.Items.Add(s);
            }
            comboBoxMAC.SelectedItem = Program.confData.Adapter;
            if (Program.confData.IVmode == Config.IVMethod.mac && comboBoxMAC.SelectedIndex == -1)
            {
                while(MessageBox.Show("Network card not found. Please connect the device and retry.","Error",MessageBoxButtons.RetryCancel,MessageBoxIcon.Warning) == DialogResult.Retry)
                {
                    foreach (string s in HardwareInfo.GetAdapters())
                    {
                        comboBoxMAC.Items.Add(s);
                    }
                    comboBoxMAC.SelectedItem = Program.confData.Adapter;
                    if (comboBoxMAC.SelectedIndex != -1) break;
                }
                if (comboBoxMAC.SelectedIndex == -1) Close();
            }
            
            Show();
            Loading = false;
            validate();
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            tmpIV = Crypto.GetRSAIV();
            validate();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            byte[] ivSic;
            // save gui stuff
            Program.confData.closeToTray = checkBox2Tray.Checked;
            Program.confData.purgePwd = checkBoxPurge.Checked;
            Program.confData.trayAutoClose = int.Parse(textBoxAutoXTtime.Text);
            if (!checkBoxAutoXT.Checked) Program.confData.trayAutoClose = 0;
            Program.confData.Save();

            // save IV if changed
            if(Program.confData.IVmode != tmpIVmode || Program.confData.IV != tmpIV)
            {
                Crypto.IV = AssembleIV();
                
                if(!BackupConf())
                {
                    Crypto.IV = Program.confData.IV;
                    return;
                }
                
                if(!OverwriteConf())
                {
                    Crypto.IV = Program.confData.IV;
                    return;
                }

                if(!Program.userData.Backup())
                {
                    if(RestoreConf()) KillConfBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    Crypto.IV = Program.confData.IV;
                    return;
                }
                
                if(!Program.userData.Save())
                {
                    if(RestoreConf()) KillConfBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    Crypto.IV = Program.confData.IV;
                    return;
                }
                
                // update in memory
                ivSic = (byte[])Program.confData.IV.Clone();
                Program.confData.IV = tmpIV;
                Program.confData.IVmode = tmpIVmode;
                
                if (radioButtonCPU.Checked) // cpu mode
                {
                    Program.confData.IVmode = Config.IVMethod.cpu;
                }
                else if (radioButtonMAC.Checked) // mac mode
                {
                    Program.confData.IVmode = Config.IVMethod.mac;
                    Program.confData.Adapter = comboBoxMAC.SelectedItem.ToString();
                }
                else if (radioButtonHD.Checked) // hd mode
                {
                    Program.confData.IVmode = Config.IVMethod.hd;
                    string hd = HardwareInfo.GetVolumeSerial(comboBoxHD.SelectedItem.ToString().Substring(0, 1));
                    Program.confData.Drive = Crypto.ComputeHash(hd, "SHA512", sIV);
                }
                else
                {
                    Program.confData.IVmode = Config.IVMethod.file;
                }

                if(!Program.confData.Save())
                {
                    if(RestoreConf()) KillConfBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    if(Program.userData.Restore()) Program.userData.KillBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    Crypto.IV = ivSic;
                    Program.confData = Config.Load();
                    return;
                }
                else
                {
                    // cleanup
                    KillConfBackup();
                    Program.userData.KillBackup();
                }
            }
            String hashNewProt;
            byte[] ivp = {};
            if (checkBox2.Checked)
            {
                string id = HardwareInfo.GetVolumeSerial(comboBox1.SelectedItem.ToString().Substring(0, 1));
                hashNewProt = Crypto.ComputeHash(id, "SHA512", sIV);
                ivp = Encoding.UTF8.GetBytes(id);
            }
            else
            {
                hashNewProt = "";
            }

            if (Program.confData.Protection != hashNewProt)
            {
                Crypto.IVP = ivp;

                if (!BackupConf()) return;

                if (!OverwriteConf()) return;

                if (!Program.userData.Backup())
                {
                    if (RestoreConf()) KillConfBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    return;
                }

                if (!Program.userData.Save())
                {
                    if (RestoreConf()) KillConfBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    return;
                }

                Program.confData.Protection = hashNewProt;

                if (!Program.confData.Save())
                {
                    if (RestoreConf()) KillConfBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    if (Program.userData.Restore()) Program.userData.KillBackup();
                    else
                    {
                        DialogResult = System.Windows.Forms.DialogResult.Retry;
                    }
                    Program.confData = Config.Load();
                    return;
                }
                else
                {
                    // cleanup
                    KillConfBackup();
                    Program.userData.KillBackup();
                }
            }

            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private bool BackupConf()
        {
            KillConfBackup();
            try
            {
                File.Copy(Program.confFile, Program.confFile + ".bak");
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup " + Program.confFile + ".bak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool RestoreConf()
        {
            try
            {
                File.Delete(Program.confFile);
                File.Copy(Program.confFile + ".bak", Program.confFile);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup " + Program.confFile + ".bak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool KillConfBackup()
        {
            try
            {
                File.Delete(Program.confFile + ".bak");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup " + Program.confFile + ".bak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        private bool OverwriteConf()
        {
            try
            {
                FileStream test = File.OpenWrite(Program.confFile);
                byte[] b = Encoding.UTF8.GetBytes(Crypto.EncryptRSA(Crypto.EncryptSecureString(Program.userData.UserFile), Program.passUser));
                test.Write(b, 0, b.Length);
                test.Flush();
                test.Close();
                // todo: finally clear b
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler beim Speichern", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        private void radioButtonFile_CheckedChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void radioButtonCPU_CheckedChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void radioButtonMAC_CheckedChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void radioButtonHD_CheckedChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void validate()
        {
            if(Loading) return;
            
            if (comboBoxMAC.SelectedIndex == -1) comboBoxMAC.SelectedIndex = 0;
            if (comboBoxHD.SelectedIndex == -1) comboBoxHD.SelectedIndex = 0;
            if (comboBox1.SelectedIndex == -1) comboBox1.SelectedIndex = 0;
            
            comboBoxMAC.Enabled = radioButtonMAC.Checked;
            comboBoxHD.Enabled = radioButtonHD.Checked;
            textBoxCPU.Enabled = radioButtonCPU.Checked;
            
            textBoxIV.Text = assembleIV();
        }

        private String assembleIV()
        {
            byte[] iv = (byte[])tmpIV.Clone();

            if (radioButtonCPU.Checked) // cpu
            {
                iv = Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetCPUId()), tmpIV);
                tmpIVmode = Config.IVMethod.cpu;
            }
            else if (radioButtonMAC.Checked) // mac
            {
                iv = Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetMACAddress(comboBoxMAC.SelectedItem.ToString())), tmpIV);
                tmpIVmode = Config.IVMethod.mac;
            }
            else if (radioButtonHD.Checked) // hd
            {
                iv = Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetVolumeSerial(comboBoxHD.SelectedItem.ToString().Substring(0, 1))), tmpIV);
                tmpIVmode = Config.IVMethod.hd;
            }
            else
            {
                tmpIVmode = Config.IVMethod.file;
            }
            return Encoding.ASCII.GetString(iv);
        }

        private byte[] AssembleIV()
        {
            byte[] iv = (byte[])tmpIV.Clone();

            if (radioButtonCPU.Checked) // cpu
            {
                iv = Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetCPUId()), tmpIV);
            }
            else if (radioButtonMAC.Checked) // mac
            {
                iv = Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetMACAddress(comboBoxMAC.SelectedItem.ToString())), tmpIV);
            }
            else if (radioButtonHD.Checked) // hd
            {
                iv = Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetVolumeSerial(comboBoxHD.SelectedItem.ToString().Substring(0, 1))), tmpIV);
            }
            return iv;
        }
        
        private byte[] Pad(byte[] data, byte[] canvas)
        {
            byte[] result = (byte[])canvas.Clone();
            int max = Math.Min(data.Length, canvas.Length);
            for (int i = 0; i < max; i++)
            {
                result[i] = (byte)(((int)canvas[i] + (int)data[i]) % 255);
            }
            return result;
        }

        private void checkBoxAutoXT_CheckedChanged(object sender, EventArgs e)
        {
            textBoxAutoXTtime.Enabled = checkBoxAutoXT.Checked;
        }

        private void comboBoxMAC_SelectedIndexChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void comboBoxHD_SelectedIndexChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = checkBox2.Checked;
        }
    }
}