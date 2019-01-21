using System;
using System.Threading;
using System.Windows.Forms;

namespace QFlashPro
{
    public partial class ProgressBox : Form
    {
        public ProgressBox()
        {
            InitializeComponent();
        }

        public static void Show1(IWin32Window owner, bool isButtonVisible)
        {
            var pr = new ProgressBox();
            //pr.button1.Visible = isButtonVisible;

            Thread t1 = new Thread(() =>
            {
                int val = 0;
                while (val < 99)
                {
                    pr.Invoke((MethodInvoker)delegate () { pr.progressBar1.Value = val; });
                    val = 10;
                    Thread.Sleep(1000);
                }
                pr.Invoke((MethodInvoker)delegate () { pr.Close(); });
            }); 
            t1.Start(); 
            pr.Show(owner);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
