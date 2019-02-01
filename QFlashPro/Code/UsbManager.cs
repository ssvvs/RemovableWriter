namespace QFlashPro.Code
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Text;


    public class UsbManager
    {
        public static Logger Logger { get; set; }
        
        public static bool GetDriveVidPid(string szDriveName, ref UsbInfo info)
        {
            bool bResult = false;
            string szSerialNumberDevice = null;
            Logger.Debug(() => $"GetDriveVidPid worked");

            ManagementObject oLogicalDisk = new ManagementObject("Win32_LogicalDisk.DeviceID='" + szDriveName.TrimEnd('\\') + "'");
            foreach (ManagementObject oDiskPartition in oLogicalDisk.GetRelated("Win32_DiskPartition"))
            {
                foreach (ManagementObject oDiskDrive in oDiskPartition.GetRelated("Win32_DiskDrive"))
                {
                    string szPNPDeviceID = oDiskDrive["PNPDeviceID"].ToString();

                    Logger.Debug(() => $"PnpDeviceId = {szPNPDeviceID}");

                    if (!szPNPDeviceID.StartsWith("USBSTOR"))
                        throw new Exception(szDriveName + " ist kein USB-Laufwerk.");


                    info.SerialNumber = parseSerialFromDeviceID(szPNPDeviceID);
                    info.ScsiRevision = parseRevFromDeviceID(szPNPDeviceID);
                    info.ScsiCusomInfo = parseVenFromDeviceID(szPNPDeviceID);
                    info.ScsiProductInfo = parseProdFromDeviceID(szPNPDeviceID);
                    info.CustomInfo = parseVenFromDeviceID(szPNPDeviceID);
                    info.ProductInfo = parseProdFromDeviceID(szPNPDeviceID);


                    string[] aszToken = szPNPDeviceID.Split(new char[] { '\\', '&' });
                    szSerialNumberDevice = aszToken[aszToken.Length - 2];
                    string deviceNumber = szSerialNumberDevice;
                    Logger.Debug(() => $"serial number device = {deviceNumber}");
                }
            }

            if (null != szSerialNumberDevice)
            {
                ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(@"root\CIMV2", "Select * from Win32_USBHub");
                foreach (ManagementObject oResult in oSearcher.Get())
                {
                    object oValue = oResult["DeviceID"];
                    if (oValue == null)
                        continue;

                    string szDeviceID = oValue.ToString();

                    Logger.Debug(() => $"szDeviceID = {szDeviceID}");


                    string[] aszToken = szDeviceID.Split(new char[] { '\\' });
                    if (szSerialNumberDevice != aszToken[aszToken.Length - 1])
                        continue;

                    int nTemp = szDeviceID.IndexOf(@"VID_");
                    if (0 > nTemp)
                        continue;

                    nTemp += 4;
                    string sVid = szDeviceID.Substring(nTemp, 4);
                    info.Vid = ushort.Parse(sVid, System.Globalization.NumberStyles.AllowHexSpecifier);
                   
                    Logger.Debug(() => $"vid = {sVid}");

                    nTemp += 4;
                    nTemp = szDeviceID.IndexOf(@"PID_", nTemp);
                    if (0 > nTemp)
                        continue;

                    nTemp += 4;
                    string sPid = szDeviceID.Substring(nTemp, 4);
                    info.Pid = ushort.Parse(sPid, System.Globalization.NumberStyles.AllowHexSpecifier);
                    Logger.Debug(() => $"pid = {sVid}");
                    bResult = true;
                    break;
                }
            }

            return bResult;
        }

        private static string parseSerialFromDeviceID(string deviceId)
        {
            Logger.Debug(() => $" parseSerial from id = {deviceId}");
            try
            {
                string[] splitDeviceId = deviceId.Split('\\');
                string[] serialArray;
                string serial;
                int arrayLen = splitDeviceId.Length - 1;

                serialArray = splitDeviceId[arrayLen].Split('&');
                serial = serialArray[0];

                return serial;
            }
            catch (Exception ex)
            {
                Logger.Debug(() => $"{ex}");
            }

            return string.Empty;
        }

        private static string parseVenFromDeviceID(string deviceId)
        {
            Logger.Debug(() => $" parsevendor from id = {deviceId}");

            try
            {

                string[] splitDeviceId = deviceId.Split('\\');
                string Ven;
                //Разбиваем строку на несколько частей. 
                //Каждая чаcть отделяется по символу &
                string[] splitVen = splitDeviceId[1].Split('&');

                Ven = splitVen[1].Replace("VEN_", "");
                Ven = Ven.Replace("_", " ");
                return Ven;
            }
            catch (Exception ex)
            {
                Logger.Debug(() => $"{ex}");
            }

            return string.Empty;
        }

        private static string parseProdFromDeviceID(string deviceId)
        {
            try
            {
                string[] splitDeviceId = deviceId.Split('\\');
                string Prod;
                //Разбиваем строку на несколько частей. 
                //Каждая чаcть отделяется по символу &
                string[] splitProd = splitDeviceId[1].Split('&');

                Prod = splitProd[2].Replace("PROD_", "");
                Prod = Prod.Replace("_", " ");
                return Prod;
            }
            catch (Exception ex)
            {
                Logger.Debug(() => $"{ex}");
            }

            return string.Empty;
        }

        private static string parseRevFromDeviceID(string deviceId)
        {
            try
            {
                string[] splitDeviceId = deviceId.Split('\\');
                string Rev;
                //Разбиваем строку на несколько частей. 
                //Каждая чаcть отделяется по символу &
                string[] splitRev = splitDeviceId[1].Split('&');

                Rev = splitRev[3].Replace("REV_", "");
                ;
                Rev = Rev.Replace("_", " ");
                return Rev;
            }
            catch (Exception ex)
            {
                Logger.Debug(() => $"{ex}");
            }

            return string.Empty;
        }


        public static IEnumerable<UsbInfo> GetAllUsbNames()
        {
            List<UsbInfo> result = new List<UsbInfo>();

            DriveInfo[] drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable).ToArray();


            foreach (DriveInfo drive in drives)
            {
                string diskName = drive.Name;               
                // Add the HDD to the list (use the Model field as the item's caption)
                UsbInfo curUsbInfo = new UsbInfo();
                Logger.Debug(() => $"Getting info from {diskName}");
                GetDriveVidPid(diskName, ref curUsbInfo);

                curUsbInfo.MemorySize = drive.TotalSize;
                curUsbInfo.VolumeLabel = diskName;
                
                result.Add(curUsbInfo);
                //result.Add($"{diskName} - {vid} - {pid}");
            }

            return result;
        }


        public static string[] GetAllUsbNames1()
        {
            List<string> result = new List<string>();

            ManagementObjectSearcher mosDisks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            // Loop through each object (disk) retrieved by WMI

            foreach (ManagementObject moDisk in mosDisks.Get())

            {

                string diskName = moDisk["Model"].ToString();
                ushort vid = 0;
                ushort pid = 0;
                // Add the HDD to the list (use the Model field as the item's caption)

               // GetDriveVidPid(diskName, ref vid, ref pid);
                result.Add($"{diskName} - {vid} - {pid}");
            }

            return result.ToArray();
        }
    }
}

