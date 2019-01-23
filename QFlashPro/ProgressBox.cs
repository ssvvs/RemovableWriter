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

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Value += 8;
            if(progressBar1.Value >= 90)
                this.Close();
        }

        private void ProgressBox_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void ProgressBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Enabled = false;
        }
    }
}
