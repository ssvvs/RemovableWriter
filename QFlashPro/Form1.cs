using QFlashPro.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QFlashPro
{
    public partial class Form1 : Form
    {
        private const string DEFAUL_CONTROLLER = "Silicon Motion SM3257EN SE";
        private const string DEFAULT_MEMORY = "Hynix H27QDG8T2B8R MLC";
        private const string UNKNOWN = "Unknown";

        private IEnumerable<UsbInfo> _prevUsbInfos;

        public Form1()
        {
            InitializeComponent();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            try
            {

            }
            catch(Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "Exception", MessageBoxButtons.OK);
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void massUpdateButton_Click(object sender, EventArgs e)
        {
            try
            {

                if (_prevUsbInfos == null)
                {
                    MessageBox.Show(null, "No firmware found for this USB dirve", "Error", MessageBoxButtons.OK);
                    return;
                }

                foreach (var item in _prevUsbInfos)
                {
                    if (IsFirmUsb(item))
                        continue;
                    TryUpdateFirmware(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "Exception", MessageBoxButtons.OK);
            }
        }

        private void updateFirmwareButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_prevUsbInfos == null)
                {
                    MessageBox.Show(null, "No firmware found for this USB dirve", "Error", MessageBoxButtons.OK);
                    return;
                }
                var usbInfo = _prevUsbInfos.FirstOrDefault(c => IsFirmUsb(c));
                if (usbInfo == null)
                {
                    MessageBox.Show(null, "No firmware found for this USB dirve", "Error", MessageBoxButtons.OK);
                    return;
                }

                TryUpdateFirmware(usbInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "Exception", MessageBoxButtons.OK);
            }
        }

        private void updateDiskInfoButton_Click(object sender, EventArgs e)
        {
            try
            {
                _prevUsbInfos = UsbManager.GetAllUsbNames();
                CorrectDataInfo(_prevUsbInfos);

                foreach (var item in _prevUsbInfos)
                {
                    if (IsFirmUsb(item))
                    {
                        var version = GetFirmwareVersion(item);
                        if (!string.IsNullOrEmpty(version))
                        {
                            item.ScsiRevision = version;
                        }
                    }
                }

                var firstDefUsb = _prevUsbInfos.FirstOrDefault();
                if (firstDefUsb == null)
                    return;

                RefreshView(firstDefUsb);
            }
            catch (Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "Exception", MessageBoxButtons.OK);
            }
        }


        private void WriteFirmwareVersion(UsbInfo info, string version)
        {
            string sysPath = info.VolumeLabel + "System Volume Information";

            if (!Directory.Exists(sysPath))
            {
                var diInfo = Directory.CreateDirectory(sysPath);
                diInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            string filePath = sysPath + "\\keyver";

            byte majorVersion = byte.Parse(version.Substring(0, version.IndexOf('.')));
            byte minorVersion = byte.Parse(version.Substring(version.IndexOf('.') + 1, version.Length - version.IndexOf('.') - 1));

            using (FileStream sw = File.Create(filePath))
            {
                sw.WriteByte(majorVersion);
                sw.WriteByte(minorVersion);
            }
            // File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.Hidden);
            File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.Hidden);
        }
        private string GetFirmwareVersion(UsbInfo info)
        {
            string sysPath = info.VolumeLabel + "System Volume Information";
            string filePath = sysPath + "\\keyver";

            if (!Directory.Exists(sysPath) || !File.Exists(filePath))
                return string.Empty;

            int majorVersion;
            int minorVersion;

            using (FileStream sw = File.OpenRead(filePath))
            {
                majorVersion = sw.ReadByte();
                minorVersion = sw.ReadByte();
            }

            return $"{majorVersion}.{minorVersion}";
        }

        private void TryUpdateFirmware(UsbInfo info)
        {
            if (!string.IsNullOrEmpty(GetFirmwareVersion(info)))
            {               
                return;
            }

            WriteFirmwareVersion(info, "1.14");

            MessageBox.Show(null, "This device will updated", "Updating Firmware", MessageBoxButtons.OK);
        }

        private void CorrectDataInfo(IEnumerable<UsbInfo> usbInfos)
        {
            foreach (var usbinfo in usbInfos)
            {
                if (IsFirmUsb(usbinfo))
                {
                    usbinfo.Memory = DEFAULT_MEMORY;
                    usbinfo.Controller = DEFAUL_CONTROLLER;
                }
                else
                {
                    usbinfo.Memory = UNKNOWN;
                    usbinfo.Controller = UNKNOWN;
                }
            }
        }

        private bool IsFirmUsb(UsbInfo usbInfo)
        {
            if (usbInfo.Vid == 8644 && usbInfo.Pid == 8005)
                return true;
            return false;
        }

        private void RefreshView(UsbInfo usbInfo)
        {
            customInfoTextbox.Text = usbInfo.CustomInfo;
            produceInfoTextbox.Text = usbInfo.ProductInfo;

            vidTextbox.Text = usbInfo.Vid.ToString();
            pidTextbox.Text = usbInfo.Pid.ToString();

            customInfoScsiTextbox.Text = usbInfo.ScsiCusomInfo;
            produceInfoScsiTextbox.Text = usbInfo.ScsiProductInfo;
            scsiRevTextbox.Text = usbInfo.ScsiRevision;

            controllerTextbox.Text = usbInfo.Controller;
            memoryTextbox.Text = usbInfo.Memory;
            serialNumberTextbox.Text = usbInfo.SerialNumber;
        }
      
    }
}
