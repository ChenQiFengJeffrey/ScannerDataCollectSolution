using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraPrinting.BarCode;
using DevExpress.DataAccess.ObjectBinding;
using System.Runtime.InteropServices;
using DevExpress.XtraEditors;
using ScannerDataCollect.Common;

namespace ScannerDataCollect.Components
{

    public partial class ExBarCodeControl : UserControl
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        private Color _borderColor = Color.Black;
        private int _borderWidth = 1;

        public String Value = String.Empty;
        public BarCodeConfig Config;
        public BarCodeGeneratorBase Symbology { get; set; }

        //
        // 摘要:
        //  获取或设置控件的边框颜色。
        //
        // 返回结果:
        //  控件的边框颜色 System.Drawing.Color。默认为 System.Drawing.Color.Black
        //  属性的值。
        [Description("组件的边框颜色。"), Category("Appearance")]
        public Color BorderColor
        {
            get
            {
                return _borderColor;
            }
            set
            {
                _borderColor = value;
                this.Invalidate();
            }
        }
        //
        // 摘要:
        //  获取或设置控件的边框宽度。
        //
        // 返回结果:
        //  控件的边框宽度 int。默认为 1
        //  属性的值。
        [Description("组件的边框宽度。"), Category("Appearance")]
        public int BorderWidth
        {
            get
            {
                return _borderWidth;
            }
            set
            {
                _borderWidth = value;
                this.Invalidate();
            }
        }

        private ExBarCodeControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Paint += ExBarCodeControl_Paint;
        }

        public ExBarCodeControl(BarCodeConfig config):this()
        {
            this.Symbology = ObjectCreateAssistant.CreateObjectInstance<BarCodeGeneratorBase>(config.Generator);
            this.Config = config;
            LoadComponent();
        }

        private void LoadComponent()
        {
            this.BorderStyle = BorderStyle.None;
            InitializeBarcodeControl();
            InitializeLabel();
            this.Size = new System.Drawing.Size(Convert.ToInt32(Math.Round(Config.Width)), Convert.ToInt32(Math.Round(Config.Height)) + 15);
        }

        private void InitializeBarcodeControl()
        {
            BarCodeGeneratorBase generator = this.Symbology;
            if (generator is QRCodeGenerator)
            {
                ((QRCodeGenerator)generator).CompactionMode = QRCodeCompactionMode.Byte;                
            }
            if (generator is Code128Generator)
            {
                ((Code128Generator)generator).CharacterSet = Code128Charset.CharsetAuto;
                ((Code128Generator)generator).AddLeadingZero = false;
                Config.Width = Config.Height * 2;
            }

            

            BarCodeControl ctrl = new BarCodeControl() { AutoModule = true, HorizontalAlignment = DevExpress.Utils.HorzAlignment.Center, Location = new System.Drawing.Point(3, 3), Name = Guid.NewGuid().ToString(), Padding = new System.Windows.Forms.Padding(10, 2, 10, 0), /*ctrl.Size = new System.Drawing.Size(100, 77);*/Size = new System.Drawing.Size(Convert.ToInt32(Math.Round(Config.Width)), Convert.ToInt32(Math.Round(Config.Height))), Symbology = this.Symbology, TabIndex = 0, ShowText = false, Text = Config.Content, Dock = DockStyle.Fill };
            
            //ctrl.MouseDoubleClick += Ctrl_MouseDoubleClick;
            //ctrl.MouseDown += Control_MouseDown; 
            this.Controls.Add(ctrl);
        }

        private void InitializeLabel()
        {
            Label label = new Label() { Text = Config.Title, Height = 30, Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Bottom };
            //label.MouseDown += Control_MouseDown;
            this.Controls.Add(label);
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            this.ExBarCodeControl_MouseDown(this, e);
        }

        private void Ctrl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Control parent = ((Control)sender).Parent;
            ExBarCodeControl_DoubleClick(parent, e);
        }

        private void ExBarCodeControl_Paint(object sender, PaintEventArgs e)
        {
            if (this.BorderStyle == BorderStyle.FixedSingle)
            {
                IntPtr hDC = GetWindowDC(this.Handle);
                Graphics g = Graphics.FromHdc(hDC);
                ControlPaint.DrawBorder(
                 g,
                 new Rectangle(0, 0, this.Width, this.Height),
                 _borderColor,
                 _borderWidth,
                 ButtonBorderStyle.Solid,
                 _borderColor,
                 _borderWidth,
                 ButtonBorderStyle.Solid,
                 _borderColor,
                 _borderWidth,
                 ButtonBorderStyle.Solid,
                 _borderColor,
                 _borderWidth,
                 ButtonBorderStyle.Solid);
                g.Dispose();
                ReleaseDC(Handle, hDC);
            }
        }

        private void ExBarCodeControl_ControlAdded(object sender, ControlEventArgs e)
        {
            e.Control.MouseEnter += Control_MouseEnter;
            e.Control.MouseLeave += Control_MouseLeave;
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            this.BorderStyle = BorderStyle.None;
        }

        private void Control_MouseEnter(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BorderColor = Color.Green; 
        }

        private void ExBarCodeControl_ControlRemoved(object sender, ControlEventArgs e)
        {
            e.Control.MouseEnter -= Control_MouseEnter;
            e.Control.MouseLeave -= Control_MouseLeave;
        }

        private void ExBarCodeControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                this.DoDragDrop(this, DragDropEffects.All);
                this.Cursor = Cursors.Arrow;
            }
        }

        private void ExBarCodeControl_DoubleClick(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                Control parent = ((Control)sender).Parent;
                parent.Controls.Remove((Control)sender);
            }
        }
    }
}
