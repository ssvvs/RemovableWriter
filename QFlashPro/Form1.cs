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

        private const string WARN_NO_FIRMWARE = "No firmware found for this USB drive";
        private const string WARN_FIRWARE_ACTUAL = "The actual version of firmware is using now";
        private RichTextBox[] _usbInfoTextboxes;
        private Logger _logger;

        private readonly string _curProgVersion;
        private readonly string _curFirmVersion;

        private Dictionary<string, string> _versionMapper;

        private IEnumerable<UsbInfo> _prevUsbInfos;

        public Form1(string curVersion, string[] args)
        {
            _versionMapper = new Dictionary<string, string>();
            _versionMapper.Add("3.95", "0.87");
            _versionMapper.Add("4.11", "1.0");
            _versionMapper.Add("4.72", "1.14");
            _curProgVersion = curVersion;
            _curFirmVersion = _versionMapper[curVersion];
            InitializeComponent();           
            _usbInfoTextboxes = new RichTextBox[] {
                textBox1_2, textBox2_2, textBox3_2, textBox4_2,
                textBox5_2, textBox6_2, textBox7_2, textBox8_2,
                textBox9_2, textBox10_2, textBox11_2, textBox12_2,
                textBox13_2, textBox14_2, textBox15_2, textBox16_2 };
            _logger = new Logger();
            _logger.SetSettings(args);
            UsbManager.Logger = _logger;
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateInfoAboutConnectedDevices();
            }
            catch(Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "Exception", MessageBoxButtons.OK);
            }
        }

        private bool IsActualVersion(string readed)
        {
            if (string.IsNullOrEmpty(_curFirmVersion) || string.IsNullOrEmpty(_curFirmVersion))
                return false;

            if (!double.TryParse(_curFirmVersion, out double currentVersion))
                return false;
            if (!double.TryParse(readed, out double readedVersion))
                return false;

            if (readedVersion < currentVersion)
                return false;

            return true;
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
                    MessageBox.Show(null, WARN_NO_FIRMWARE, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (_prevUsbInfos.Any(d => !IsFirmUsb(d)))
                {
                    MessageBox.Show(null, WARN_NO_FIRMWARE, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (_prevUsbInfos.Any(d => IsActualVersion(GetFirmwareVersion(d))))
                {
                    MessageBox.Show(null, WARN_FIRWARE_ACTUAL, "Error", MessageBoxButtons.OK);
                    return;
                }
               
                foreach (var item in _prevUsbInfos)
                {
                    TryUpdateFirmware(item);
                }

                MessageBox.Show(null, "Successfull", "Updating Firmware",MessageBoxButtons.OK);
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
                    MessageBox.Show(null, WARN_NO_FIRMWARE, "Error", MessageBoxButtons.OK);
                    return;
                }
                var usbInfo = _prevUsbInfos.FirstOrDefault(c => IsFirmUsb(c));
                if (usbInfo == null)
                {
                    MessageBox.Show(null, WARN_NO_FIRMWARE, "Error", MessageBoxButtons.OK);
                    return;
                }

                bool isUpdated = TryUpdateFirmware(usbInfo);
                MessageBox.Show(
                    null,
                    isUpdated ? "Successfull" : WARN_FIRWARE_ACTUAL,
                    "Updating Firmware",
                    MessageBoxButtons.OK);
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
                UpdateInfoAboutConnectedDevices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "Exception", MessageBoxButtons.OK);
            }
        }

        private void UpdateInfoAboutConnectedDevices()
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
                    else
                    {
                        item.ScsiRevision = "1.0";
                    }
                }
            }

            if (_prevUsbInfos != null)
                RefreshBottomView(_prevUsbInfos.ToArray());

            var firstDefUsb = _prevUsbInfos.FirstOrDefault();
            if (firstDefUsb == null)
                return;

            RefreshUpView(firstDefUsb);
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
            //File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.Hidden);
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

        private bool TryUpdateFirmware(UsbInfo info)
        {
            if (IsActualVersion(GetFirmwareVersion(info)))
            {
                return false;
            }

            WriteFirmwareVersion(info, _curFirmVersion);
            ProgressBox prBox = new ProgressBox();
            prBox.ShowDialog();
            //ProgressBox.Show1(this, true);
            return true;
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
            if ((usbInfo.Vid == 0x8644 && usbInfo.Pid == 0x8005)||
                (usbInfo.Vid == 0x090C && usbInfo.Pid == 0x1000)
                //||(usbInfo.Vid == 0x058F && usbInfo.Pid == 0x6387)
                )
                return true;
            return false;
        }

        private void RefreshBottomView(UsbInfo[] usbInfos)
        {
            for (int i = 0; i < usbInfos.Length; i++)
            {
                if (IsFirmUsb(usbInfos[i]))
                {
                    _usbInfoTextboxes[i].Text = $"(SG1581) H27UDG8M2MTR(ED3) Cap:{usbInfos[i].MemorySize / 1000000}MB ID:{usbInfos[i].SerialNumber} Ver:1.8.6.1.912_JBL_161020";
                    _usbInfoTextboxes[i].BackColor = Color.Orange;
                }
                else
                {
                    _usbInfoTextboxes[i].Text = $"(UNKNOWN) UNKNOWN Cap:{usbInfos[i].MemorySize / 1000000}MB ID:{usbInfos[i].SerialNumber} Ver:1.8.6.1.912_JBL_161020";
                    _usbInfoTextboxes[i].BackColor = Color.Red;
                }
            }
            for (int i = usbInfos.Length; i <= 15; i++)
            {
                _usbInfoTextboxes[i].Text = string.Empty;
                _usbInfoTextboxes[i].BackColor = Color.LightGray;
            }
        }

        private void RefreshUpView(UsbInfo usbInfo)
        {
            customInfoTextbox.Text = usbInfo.CustomInfo;
            produceInfoTextbox.Text = usbInfo.ProductInfo;

            vidTextbox.Text = string.Format("{0:x}", usbInfo.Vid);
            pidTextbox.Text = string.Format("{0:x}", usbInfo.Pid);

            customInfoScsiTextbox.Text = usbInfo.ScsiCusomInfo;
            produceInfoScsiTextbox.Text = usbInfo.ScsiProductInfo;
            scsiRevTextbox.Text = usbInfo.ScsiRevision;

            controllerTextbox.Text = usbInfo.Controller;
            memoryTextbox.Text = usbInfo.Memory;            

            fixedPrefixTextbox.Text = "1234";
            autoTextBox.Text = "223";
            increaseArrangeTextbox.Text = "000000000000";
            tildaTexbox.Text = "999999999999";
            serialNumberTextbox.Text = "12340";
            //serialNumberTextbox.Text = usbInfo.SerialNumber;
        }

        private void ClearUpView()
        {
            customInfoTextbox.Text = string.Empty;
            produceInfoTextbox.Text = string.Empty; 

            vidTextbox.Text = string.Empty;
            pidTextbox.Text = string.Empty;

            customInfoScsiTextbox.Text = string.Empty;
            produceInfoScsiTextbox.Text = string.Empty;
            scsiRevTextbox.Text = string.Empty;

            controllerTextbox.Text = string.Empty;
            memoryTextbox.Text = string.Empty;

            fixedPrefixTextbox.Text = string.Empty;
            autoTextBox.Text = string.Empty;
            increaseArrangeTextbox.Text = string.Empty;
            tildaTexbox.Text = string.Empty;
            serialNumberTextbox.Text = string.Empty;
        }
      
    }
}
