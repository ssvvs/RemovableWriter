namespace QFlashPro.Code
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class UsbInfo
    {
        public string CustomInfo { get; set; }
        public string ProductInfo { get; set; }
        public string ScsiCusomInfo { get; set; }
        public string ScsiProductInfo { get; set; }
        public string ScsiRevision { get; set; }
        public int Vid { get; set; }
        public int Pid { get; set; }
        public string Controller { get; set; }
        public long MemorySize { get; set; }
        public string Memory { get; set; }
        public string VolumeLabel { get; set; }
        public string SerialNumber { get; set; }
       
    }
}
