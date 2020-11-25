using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScannerDataCollect.Components
{
    public partial class FrmServiceIP : Form
    {
        private List<String> _iplist;
        public FrmServiceIP(List<String> iplist)
        {

            _iplist = iplist;
            InitializeComponent();
        }

        private void FrmServiceIP_Load(object sender, EventArgs e)
        {
            foreach (String ip in _iplist)
            {
                this.flowLayoutPanel1.Controls.Add(GetControl(ip, ip, "DevExpress.XtraPrinting.BarCode.QRCodeGenerator", 150, 150));
            }

            this.Width = 150 * _iplist.Count + 20;
        }

        private ExBarCodeControl GetControl(String title, String content, String generator, double width, double height)
        {
            BarCodeConfig config = new BarCodeConfig()
            {
                Title = title.Length == 0 ? content : title,
                Content = content,
                Generator = generator.ToString(),
                Width = width,
                Height = height
            };

            return new ExBarCodeControl(config);
        }
    }
}
