using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScannerDataCollect.Components
{
    public partial class AboutMeForm : DevExpress.XtraEditors.XtraForm
    {
        public AboutMeForm()
        {
            InitializeComponent();
        }

        private void AboutMeForm_Load(object sender, EventArgs e)
        {
            label6.Text = $"Copyright  © 2013 - {DateTime.Now.Year}";
            lblVersion.Text = string.Format("{0}", Assembly.GetEntryAssembly().GetName().Version);
            this.panel1.Capture = true;
            this.panel1.MouseCaptureChanged += Panel1_MouseCaptureChanged;


        }

        private void Panel1_MouseCaptureChanged(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
