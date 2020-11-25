using Aspose.Cells;
using Autofac.Extras.DynamicProxy;
using AutoUpdaterDotNET;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using Newtonsoft.Json;
using ScannerDataCollect.Common;
using ScannerDataCollect.Components;
using ScannerDataCollect.Core;
using ScannerDataCollect.Services;
using ScannerDataCollectServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace ScannerDataCollect
{

    public partial class MainApp : Form
    {
        static object lockObj = new object();
        private delegate void SetTextCallback(string text, out String msg);
        private delegate void SetBtnRunCallback(string text, Color color, bool enabled);
        private delegate void GridContrlRefreshCallback(GridControl grid, DataSet dataSet, String datamember);

        private ServiceHost serviceHost = null;
        private volatile bool isServiceStart = false;
        SynchronizationContext m_SyncContext = null;
        DataSet ds = null;
        public MainApp()
        {
            InitializeComponent();
            ElevatedDragDropManager.Instance.EnableDragDrop(gridControl2.Handle);
            // Enable elevated drag drop on listView1. Note that I used the Handle property
            ElevatedDragDropManager.Instance.ElevatedDragDrop += Instance_ElevatedDragDrop;
            m_SyncContext = SynchronizationContext.Current;
            ds = new DataSet();
            SvrUtils.Instance.OnStarted += Service_OnStarted;
            SvrUtils.Instance.OnStarting += Service_OnStarting;
            SvrUtils.Instance.OnStoped += Service_OnStoped;
            SvrUtils.Instance.OnStopping += Service_OnStopping;
            SvrUtils.Instance.OnGetResponse += Service_OnGetResponse;
        }

        private void Instance_ElevatedDragDrop(object sender, ElevatedDragDropArgs e)
        {
            if (e.HWnd == this.gridControl2.Handle)
            {
                foreach (string file in e.Files)
                {
                    AnalysisExcelFileAsync(file);
                }
            }
        }

        private void AnalysisExcelFile(String file)
        {
            Workbook workbook = new Workbook(file);
            Worksheet worksheet = workbook.Worksheets[0];
            if (ds.Tables.Contains("Info")) ds.Tables.Remove("Info");
            DataTable dt = new DataTable("Info");
            dt.Columns.Add(new DataColumn("分类"));
            dt.Columns.Add(new DataColumn("省份"));
            dt.Columns.Add(new DataColumn("受理日期"));
            dt.Columns.Add(new DataColumn("受理时间"));
            dt.Columns.Add(new DataColumn("交货单号"));
            dt.Columns.Add(new DataColumn("送达方名称"));
            dt.Columns.Add(new DataColumn("客户简称"));
            dt.Columns.Add(new DataColumn("交货单金额"));
            dt.Columns.Add(new DataColumn("总单号"));
            dt.Columns.Add(new DataColumn("总箱数"));
            dt.Columns.Add(new DataColumn("生成时间"));
            dt.Columns.Add(new DataColumn("扫描时间"));
            dt.Columns.Add(new DataColumn("箱号"));
            dt.Columns.Add(new DataColumn("已扫描"));
            ds.Tables.Add(dt);
            DataTable data = worksheet.Cells.ExportDataTableAsString(0, 0, worksheet.Cells.MaxRow + 1, worksheet.Cells.MaxColumn + 1, true);
            for (int i = 0; i < data.Rows.Count; i++)
            {
                if (data.Columns.Contains("交货单号"))
                {
                    if (String.IsNullOrEmpty(data.Rows[i]["交货单号"]?.ToString())) continue;
                }
                if (i > worksheet.Cells.MaxDataRow) return;
                String masterbillno = data.Columns.Contains("总单号") ? data.Rows[i]["总单号"]?.ToString() : "";
                String subbillno = data.Columns.Contains("交货单号") ? data.Rows[i]["交货单号"]?.ToString() : "";
                dt.Rows.Add(
                    String.IsNullOrEmpty(masterbillno) ? "未分类" : masterbillno == subbillno ? "总单" : "分单",
                    data.Columns.Contains("省份") ? data.Rows[i]["省份"]?.ToString() : "",
                    data.Columns.Contains("受理日期") ? data.Rows[i]["受理日期"]?.ToString() : "",
                    data.Columns.Contains("受理时间") ? data.Rows[i]["受理时间"]?.ToString() : "",
                    data.Columns.Contains("交货单号") ? data.Rows[i]["交货单号"]?.ToString() : "",
                    data.Columns.Contains("送达方名称") ? data.Rows[i]["送达方名称"]?.ToString() : "",
                    data.Columns.Contains("客户简称") ? data.Rows[i]["客户简称"]?.ToString() : "",
                    data.Columns.Contains("交货单金额") ? data.Rows[i]["交货单金额"]?.ToString() : "",
                    data.Columns.Contains("总单号") ? data.Rows[i]["总单号"]?.ToString() : "",
                    data.Columns.Contains("总箱数") ? data.Rows[i]["总箱数"]?.ToString() : "",
                    data.Columns.Contains("生成时间") ? data.Rows[i]["生成时间"]?.ToString() : "",
                    data.Columns.Contains("扫描时间") ? data.Rows[i]["扫描时间"]?.ToString() : ""
                    );
            }
            if (dt.Rows.Count == 0) return;
            if (this.gridControl2.InvokeRequired)
            {
                GridContrlRefreshCallback d = new GridContrlRefreshCallback(GridContrl_Refresh);
                this.Invoke(d, new object[] { gridControl2, ds, "Info" });
            }
            else
            {
                GridContrl_Refresh(gridControl2, ds, "Info");
            }
        }

        private void GridContrl_Refresh(GridControl grid, DataSet dataSet, String datamember)
        {
            grid.DataSource = ds;
            grid.DataMember = "Info";
        }

        delegate void AsyncAnalysisExcelFile(String file);
        private void AnalysisExcelFileAsync(String file)
        {
            AsyncAnalysisExcelFile caller = AnalysisExcelFile;
            caller.BeginInvoke(file, AnalysisExcelFileCallBack, caller);
        }

        private void AnalysisExcelFileCallBack(IAsyncResult ar)
        {
            var caller = (AsyncAnalysisExcelFile)ar.AsyncState;
            caller.EndInvoke(ar);
        }

        private void Service_OnGetResponse(object sender, ResponseMessageEventArgs e)
        {
            String msg = String.Empty;
            ReceiveContent(e.ResponseText, out msg);
            e.ResultMsg = msg;
        }

        private void Service_OnStopping()
        {
            if (this.btnRun.InvokeRequired)
            {
                SetBtnRunCallback d = new SetBtnRunCallback(SetBtnRun);
                this.Invoke(d, new object[] { "关闭中...", Color.Yellow, false });
            }
            else
            {
                SetBtnRun("关闭中...", Color.Yellow, false);
            }
        }

        private void Service_OnStoped()
        {
            if (this.btnRun.InvokeRequired)
            {
                SetBtnRunCallback d = new SetBtnRunCallback(SetBtnRun);
                this.Invoke(d, new object[] { "运行", Color.Red, true });
            }
            else
            {
                SetBtnRun("运行", Color.Red, true);
            }
        }

        private void Service_OnStarted()
        {
            if (this.btnRun.InvokeRequired)
            {
                SetBtnRunCallback d = new SetBtnRunCallback(SetBtnRun);
                this.Invoke(d, new object[] { "停止", Color.Green, true });
            }
            else
            {
                SetBtnRun("停止", Color.Green, true);
            }
        }

        private void Service_OnStarting()
        {
            if (this.btnRun.InvokeRequired)
            {
                SetBtnRunCallback d = new SetBtnRunCallback(SetBtnRun);
                this.Invoke(d, new object[] { "启动中...", Color.Yellow, false });
            }
            else
            {
                SetBtnRun("启动中...", Color.Yellow, false);
            }
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            StartServicesAsync();
        }

        private void Init_Table()
        {
            if (ds.Tables.Contains("Data")) ds.Tables.Remove("Data");
            DataTable dt = new DataTable("Data");

            dt.Columns.Add(new DataColumn("交货单号"));
            dt.Columns.Add(new DataColumn("箱件数"));
            dt.Columns.Add(new DataColumn("箱号"));
            dt.Columns.Add(new DataColumn("服务站名称"));
            dt.Columns.Add(new DataColumn("服务站代码"));
            dt.Columns.Add(new DataColumn("生成时间"));
            dt.Columns.Add(new DataColumn("分单号"));
            dt.Columns.Add(new DataColumn("扫描时间"));
            dt.Columns.Add(new DataColumn("唯一码"));
            ds.Tables.Add(dt);
        }

        private void DataTable_Refresh()
        {
            if (ds.Tables.Contains("Data"))
            {
                this.toolStripStatusLabel1.Text = $"共{ds.Tables["Data"].Rows.Count}条记录";
            }
            else
            {
                this.toolStripStatusLabel1.Text = $"共0条记录";
            }

        }

        private void StartServices()
        {
            if (SvrUtils.Instance.ServiceHost == null)
            {
                try
                {
                    SvrUtils.Instance.Start();
                }
                catch
                {
                    SvrUtils.Instance.Abort();
                }
            }
            else
            {
                SvrUtils.Instance.Close();
            }

        }

        private void SetBtnRun(string text, Color color, bool enabled)
        {
            this.btnRun.Enabled = enabled;
            this.btnRun.BackColor = color;
            switch (SvrUtils.Instance.Status)
            {
                case SvrStatus.Started:
                    this.ssbServiceAddr.Image = Resource.Runing;
                    foreach (String ip in SvrUtils.Instance.ListenerIPList)
                    {
                        this.flowLayoutPanel1.Controls.Add(GetControl(ip, ip, "DevExpress.XtraPrinting.BarCode.QRCodeGenerator", 150, 150));
                    }
                    break;
                case SvrStatus.Stoped:
                    this.ssbServiceAddr.Image = Resource.Stop;
                    this.flowLayoutPanel1.Controls.Clear();
                    break;
                default:
                    this.ssbServiceAddr.Image = Resource.Starting; break;
            }
            this.btnRun.Text = text;
        }

        private void ReceiveContent(String value, out String msg)
        {
            // InvokeRequired需要比较调用线程ID和创建线程ID
            // 如果它们不相同则返回true
            if (this.gridControl1.InvokeRequired)
            {
                msg = String.Empty;
                SetTextCallback d = new SetTextCallback(ReceiveContent);
                object[] parameters = new object[] { value, null };
                this.Invoke(d, parameters);
                msg = Convert.ToString(parameters[1]);
            }
            else
            {
                lock (lockObj)
                {
                    Dictionary<String, List<String>> cntrnumber = new Dictionary<String, List<String>>();
                    String regexPattern = @"(?<ServiceCode>.*)\|(?<ServiceName>.*)\|(?<CntrNo>.*)\|(?<LabelPrintTime>.*)\|(?<SubDeliverNo>.*)\|(?<DeliveryNo>.*)";

                    List<String> result = JsonConvert.DeserializeObject<List<String>>(value);
                    if (result == null)
                    {
                        msg = String.Empty;
                        return;
                    }
                    DateTime time = DateTime.Now;
                    DataTable table = ds.Tables["Data"];
                    foreach (String str in result)
                    {
                        Match match = Regex.Match(str, regexPattern);
                        if (!match.Success) continue;
                        String subdeliverno = match.Groups["SubDeliverNo"].Value.Trim();
                        String deliveryno = match.Groups["DeliveryNo"].Value.Trim();
                        String scanTime = null;
                        String labelPrintTime = null;
                        String cntrQty = null;


                        if (table.Select($"唯一码 = '{match.Groups["DeliveryNo"].Value.Trim() + match.Groups["CntrNo"].Value.Trim()}'").Count() == 0)
                        {
                            if (!cntrnumber.ContainsKey(deliveryno))
                            {
                                cntrnumber.Add(deliveryno, new List<string>());
                            }

                            cntrnumber[deliveryno].Add(match.Groups["CntrNo"].Value.Trim().Split('-')[1].PadLeft(3, '0'));

                            cntrQty = match.Groups["CntrNo"].Value.Trim().Split('-')[0];
                            scanTime = time.ToString("yyyy/MM/dd HH:ss");
                            labelPrintTime = match.Groups["LabelPrintTime"].Value.Trim();
                            table.Rows.Add(
                                match.Groups["DeliveryNo"].Value.Trim(),
                                cntrQty,
                                match.Groups["CntrNo"].Value.Trim().Split('-')[1].PadLeft(3, '0'),
                                match.Groups["ServiceName"].Value.Trim(),
                                match.Groups["ServiceCode"].Value.Trim(),
                                match.Groups["LabelPrintTime"].Value.Trim(),
                                match.Groups["SubDeliverNo"].Value.Trim(),
                                scanTime,
                                match.Groups["DeliveryNo"].Value.Trim() + match.Groups["CntrNo"].Value.Trim()
                            );
                        }

                        if (ds.Tables.Contains("Info"))
                        {
                            List<String> subdelivernolist = new List<string>();
                            var dt = ds.Tables["Info"];
                            subdelivernolist.Add($"'{deliveryno}'");
                            if (!String.IsNullOrEmpty(subdeliverno))
                            {
                                String subdelivernopattern = @"(?<subdeliverno>\d+)/?";
                                MatchCollection sdnpMatchList = Regex.Matches(subdeliverno, subdelivernopattern);
                                foreach (Match sdnpMatch in sdnpMatchList)
                                {
                                    if (!sdnpMatch.Success) continue;
                                    subdelivernolist.Add($"'{sdnpMatch.Groups["subdeliverno"].ToString()}'");
                                }
                            }

                            List<DataRow> dataRows = dt.Select($"交货单号 in ({String.Join(",", subdelivernolist)}) and (len(总单号) = 0 or 总单号 is null)").ToList();

                            foreach (DataRow row in dataRows)
                            {
                                String billType;

                                row["总单号"] = deliveryno;
                                row["分类"] = billType = String.IsNullOrEmpty(row["总单号"]?.ToString()) ? "未分类" : row["总单号"].ToString() == row["交货单号"].ToString() ? "总单" : "分单";


                                if (billType == "总单" && cntrnumber.ContainsKey(deliveryno))
                                {
                                    if (String.IsNullOrEmpty(row["箱号"]?.ToString()))
                                        row["箱号"] = String.Join(",", cntrnumber[deliveryno].Distinct());
                                    else
                                    {
                                        cntrnumber[deliveryno].AddRange(row["箱号"].ToString().Split(','));
                                        row["箱号"] = String.Join(",", cntrnumber[deliveryno].Distinct());
                                    }
                                }

                                if (billType == "总单" && !String.IsNullOrEmpty(row["箱号"]?.ToString()))
                                {
                                    row["已扫描"] = row["箱号"].ToString().Split(',').Count();
                                }

                                if (!String.IsNullOrEmpty(cntrQty) && billType == "总单")
                                {
                                    row["总箱数"] = String.IsNullOrEmpty(row["已扫描"]?.ToString()) ? $"{ cntrQty}" : $"{cntrQty}";
                                }



                                if (!String.IsNullOrEmpty(scanTime))
                                {
                                    row["扫描时间"] = String.IsNullOrEmpty(row["扫描时间"]?.ToString()) ? scanTime : row["扫描时间"];
                                }

                                if (!String.IsNullOrEmpty(labelPrintTime))
                                {
                                    row["生成时间"] = String.IsNullOrEmpty(row["生成时间"]?.ToString()) ? labelPrintTime : row["生成时间"];
                                }
                            }

                            if (gridView2.GetSelectedRows().Count() != 0)
                            {
                                string custname = gridView2.GetRowCellValue(gridView2.FocusedRowHandle, "客户简称").ToString();
                                string masterbillno = gridView2.GetRowCellValue(gridView2.FocusedRowHandle, "总单号").ToString();
                                BillDetailList(custname, masterbillno);
                            }
                        }
                        DataTable_Refresh();
                    }

                    msg = String.Empty;
                }
            }
        }

        public void RunService(object host)
        {
            ServiceHost serviceHost = (ServiceHost)host;
            try
            {
                serviceHost.Open();

                while (isServiceStart)
                {

                }
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occured: {0}", ce.Message);
                serviceHost.Abort();
            }
        }



        private void MainApp_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (ds.Tables.Contains("Info"))
            {
                DataTable dt = ds.Tables["Info"];
                if (dt.Rows.Count > 0)
                    StartSaveDataAsync(dt);
            }
            if (serviceHost != null) serviceHost.Close();
            System.Environment.Exit(0);
        }

        void AutoUpdater_ApplicationExitEvent()
        {
            Text = @"正在关闭应用...";
            Thread.Sleep(5000);
            Application.Exit();
        }
        private void MainApp_Load(object sender, EventArgs e)
        {
            String url = ConfigurationManager.AppSettings["updateurl"];
            if (String.IsNullOrEmpty(url)) return;
            this.Hide();
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.Start(url);
            this.Show();
            this.Init_Table();
            gridControl1.DataSource = this.ds;
            gridControl1.DataMember = "Data";
            StartServicesAsync();
        }

        delegate void AsyncStartServices();
        private void StartServicesAsync()
        {
            AsyncStartServices caller = StartServices;
            caller.BeginInvoke(StartServicesCallBack, caller);
        }

        private void StartServicesCallBack(IAsyncResult ar)
        {
            var caller = (AsyncStartServices)ar.AsyncState;
            caller.EndInvoke(ar);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filter = "Excel文件(*.xls,*.xlsx)|*.xls;*.xlsx" };
            DialogResult result = dialog.ShowDialog();
            if (result.Equals(DialogResult.OK))
            {
                Workbook workbook = new Workbook();
                workbook.Worksheets.Clear();
                GenerateWorksheet(workbook, "数据收集", this.gridView1);

                workbook.Save(dialog.FileName);

                Process.Start(dialog.FileName);
            }
        }

        private void GenerateWorksheet(Workbook workbook, String sheetname, GridView gridview)
        {
            Worksheet worksheet = workbook.Worksheets.Add(sheetname);
            GetWorksheet(worksheet, gridview);
        }

        private void GetWorksheet(Worksheet worksheet, GridView gridview)
        {
            MemoryStream stream1 = new MemoryStream();
            gridview.Export(DevExpress.XtraPrinting.ExportTarget.Xlsx, stream1);
            Workbook workbook = new Workbook(stream1);
            worksheet.Copy(workbook.Worksheets[0]);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (!ds.Tables.Contains("Data")) return;
            ds.Tables["Data"].Rows.Clear();
            gridControl1.DataSource = this.ds;
            gridControl1.DataMember = "Data";
            DataTable_Refresh();
        }

        private void SsbServiceAddr_Click(object sender, EventArgs e)
        {
            //if (SvrUtils.Instance.isServiceStart)
            //{
            //    FrmServiceIP form = new FrmServiceIP(SvrUtils.Instance.ListenerIPList);
            //    form.StartPosition = FormStartPosition.CenterScreen;
            //    form.Show();
            //}

        }

        private void SsbServiceAddr_ButtonClick(object sender, EventArgs e)
        {
            if (this.flowLayoutPanel1.Visible)
            {
                this.flowLayoutPanel1.Hide();
            }
            else
            {
                this.flowLayoutPanel1.Show();
            }
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

        private void GridControl2_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void BillDetailList(String custName, String masterBillNo)
        {
            String criteria = $"(客户简称 = '{custName}' And (len(总单号) = 0 or 总单号 is null))";
            if (!String.IsNullOrEmpty(masterBillNo))
            {
                criteria += $" or (总单号 = '{masterBillNo}')";
            }
            var list = ds.Tables["Info"].Select(criteria).Select(p => new { billno = p["交货单号"], masterbillno = p["总单号"] });
            this.listView1.Items.Clear();
            foreach (var l in list)
            {
                ListViewItem item = new ListViewItem(l.billno.ToString())
                {
                    UseItemStyleForSubItems = false
                };
                var subitem = new ListViewSubItem(item, String.IsNullOrEmpty(l.masterbillno?.ToString()) ? "未关联" : "已关联")
                {
                    ForeColor = String.IsNullOrEmpty(l.masterbillno?.ToString()) ? Color.Red : Color.Green
                };
                item.SubItems.Add(subitem);
                this.listView1.Items.Add(item);
            }
        }

        private void GridControl2_MouseClick(object sender, MouseEventArgs e)
        {
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hInfo = gridView2.CalcHitInfo(new Point(e.X, e.Y));
            if (e.Button == MouseButtons.Left)
            {
                if (hInfo.InRow)
                {
                    string custname = gridView2.GetRowCellValue(gridView2.FocusedRowHandle, "客户简称").ToString();
                    string masterbillno = gridView2.GetRowCellValue(gridView2.FocusedRowHandle, "总单号").ToString();
                    BillDetailList(custname, masterbillno);
                }
            }
        }

        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = listView1.HitTest(e.X, e.Y);
            if (info.Item == null) return;
            String status = info.Item.SubItems[1].Text;
            int curSelectRow = gridView2.GetSelectedRows()[0];
            String masterbillno = this.gridView2.GetRowCellValue(curSelectRow, "总单号")?.ToString();
            String subbillno = this.gridView2.GetRowCellValue(curSelectRow, "交货单号")?.ToString();
            string custname = gridView2.GetRowCellValue(gridView2.FocusedRowHandle, "客户简称").ToString();
            switch (status)
            {
                case "未关联":
                    if (String.IsNullOrEmpty(masterbillno) && info.Item.Text == subbillno)
                    {
                        DataRow row = ds.Tables["info"].Select($"交货单号 = '{subbillno}'").FirstOrDefault();
                        if (row != null)
                        {
                            row["总单号"] = subbillno;
                            row["分类"] = String.IsNullOrEmpty(row["总单号"]?.ToString()) ? "未分类" : row["总单号"].ToString() == row["交货单号"].ToString() ? "总单" : "分单";
                        }
                        masterbillno = subbillno;
                    }
                    else if (!String.IsNullOrEmpty(masterbillno))
                    {
                        DataRow row = ds.Tables["info"].Select($"交货单号 = '{info.Item.Text}'").FirstOrDefault();
                        if (row != null)
                        {
                            row["总单号"] = masterbillno;
                            row["分类"] = String.IsNullOrEmpty(row["总单号"]?.ToString()) ? "未分类" : row["总单号"].ToString() == row["交货单号"].ToString() ? "总单" : "分单";
                        }
                    }
                    else if (String.IsNullOrEmpty(masterbillno) && info.Item.Text != subbillno)
                    {
                        MessageBox.Show($"交货单号为{info.Item.Text}不能与{masterbillno}相关联，因为{masterbillno}不是总单号", "提示");
                        return;
                    }

                    this.gridView2.RefreshData();
                    BillDetailList(custname, masterbillno);
                    break;
                case "已关联":
                    DataRow[] rows = ds.Tables["info"].Select($"总单号 = '{masterbillno}'");
                    if (masterbillno == info.Item.Text && rows.Count() == 1)
                    {
                        DataRow row = ds.Tables["info"].Select($"交货单号 = '{masterbillno}'").FirstOrDefault();
                        if (row != null)
                        {
                            row["总单号"] = "";
                            row["总箱数"] = "";
                            row["已扫描"] = "";
                            row["分类"] = String.IsNullOrEmpty(row["总单号"]?.ToString()) ? "未分类" : row["总单号"] == row["交货单号"] ? "总单" : "分单";
                        }
                    }
                    else if (masterbillno != info.Item.Text && rows.Count() > 1)
                    {
                        DataRow row = ds.Tables["info"].Select($"交货单号 = '{info.Item.Text}'").FirstOrDefault();
                        if (row != null)
                        {
                            row["总单号"] = "";
                            row["总箱数"] = "";
                            row["已扫描"] = "";
                            row["分类"] = String.IsNullOrEmpty(row["总单号"]?.ToString()) ? "未分类" : row["总单号"] == row["交货单号"] ? "总单" : "分单";
                        }
                    }
                    else if (masterbillno == info.Item.Text && rows.Count() > 1)
                    {
                        MessageBox.Show($"交货单号为{info.Item.Text}为主单号，其它有多个分单号，请先取消项下所有分单后，取消总单", "提示");
                        return;
                    }
                    this.gridView2.RefreshData();
                    BillDetailList(custname, masterbillno);
                    break;
            }
        }

        private void Tssb_abountme_Click(object sender, EventArgs e)
        {
            AboutMeForm frmAboutMe = new AboutMeForm() { StartPosition = FormStartPosition.CenterParent };
            frmAboutMe.ShowDialog(this);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (ds.Tables.Contains("Info"))
            {
                DataTable dt = ds.Tables["Info"];
                StartSaveDataAsync(dt);
            }
        }

        delegate void AsyncStartSaveData(DataTable dt);

        private void StartSaveDataAsync(DataTable dt)
        {
            AsyncStartSaveData caller = StartSaveData;
            caller.BeginInvoke(dt, StartSaveDataBack, caller);
        }
        private void StartSaveDataBack(IAsyncResult ar)
        {
            var caller = (AsyncStartSaveData)ar.AsyncState;
            caller.EndInvoke(ar);
        }

        /// <summary>
        /// 保存excel数据
        /// </summary>
        private void StartSaveData(DataTable data)
        {
            lock (lockObj)
            {
                StringBuilder sb1 = new StringBuilder();
                sb1.Append("select deliveryno,zdh from scanlabelrec where deliveryno in");
                sb1.Append("(");
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    sb1.Append(string.Format("'{0}'", data.Rows[i]["交货单号"].ToString()));
                    if (data.Rows.Count != i + 1)
                        sb1.Append(",");
                }
                sb1.Append(")");
                DataTable dt = new SQLiteHelper().ExecuteQuery(sb1.ToString());

                StringBuilder sb2 = new StringBuilder();
                sb2.Append("insert into ScanLabelRec(classify,province,sldate,sltime,deliveryno,sdfname,client,deliverymoney,zdh,zxs,generatedtime,scanningtime)");
                sb2.Append(" values(");
                sb2.Append("@classify,@province,@sldate,@sltime,@deliveryno,@sdfname,@client,@deliverymoney,@zdh,@zxs,@generatedtime,@scanningtime");
                sb2.Append(")");
                List<SQLiteParameter[]> list1 = new List<SQLiteParameter[]>();

                StringBuilder sb3 = new StringBuilder();
                sb3.Append("update scanlabelrec set classify=@classify,zdh=@zdh,zxs=@zxs,generatedtime=@generatedtime,scanningtime=@scanningtime,whetherupload=@whetherupload");
                sb3.Append(" where deliveryno=@deliveryno");
                List<SQLiteParameter[]> list2 = new List<SQLiteParameter[]>();

                bool fltinsert = false;
                bool fltupdate = false;
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    List<DataRow> dataRows = dt.Select($"deliveryno='{data.Rows[i]["交货单号"].ToString()}'").ToList();
                    if (dataRows.Count > 0)
                    {
                        if (string.IsNullOrWhiteSpace(dataRows[0]["zdh"].ToString()))
                        {
                            if (!string.IsNullOrWhiteSpace(data.Rows[i]["总单号"].ToString()))
                            {
                                fltupdate = true;
                                SQLiteParameter[] parameters = {
                                    SQLiteHelper.MakeSQLiteParameter("@classify", DbType.String,data.Rows[i]["分类"].ToString().Trim()),
                                    SQLiteHelper.MakeSQLiteParameter("@zdh", DbType.String,data.Rows[i]["总单号"].ToString().Trim()),
                                    SQLiteHelper.MakeSQLiteParameter("@zxs", DbType.String,data.Rows[i]["总箱数"].ToString().Trim()),
                                    SQLiteHelper.MakeSQLiteParameter("@generatedtime", DbType.String,data.Rows[i]["生成时间"].ToString().Trim()),
                                    SQLiteHelper.MakeSQLiteParameter("@scanningtime", DbType.String,data.Rows[i]["扫描时间"].ToString().Trim()),
                                    SQLiteHelper.MakeSQLiteParameter("@deliveryno", DbType.String,data.Rows[i]["交货单号"].ToString().Trim()),
                                    SQLiteHelper.MakeSQLiteParameter("@whetherupload", DbType.Int32,0)
                                };
                                list2.Add(parameters);
                            }
                        }
                    }
                    else
                    {
                        fltinsert = true;
                        SQLiteParameter[] parameters = {
                                SQLiteHelper.MakeSQLiteParameter("@classify", DbType.String,data.Rows[i]["分类"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@province", DbType.String,data.Rows[i]["省份"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@sldate", DbType.String,data.Rows[i]["受理日期"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@sltime", DbType.String,data.Rows[i]["受理时间"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@deliveryno", DbType.String,data.Rows[i]["交货单号"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@sdfname", DbType.String,data.Rows[i]["送达方名称"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@client", DbType.String,data.Rows[i]["客户简称"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@deliverymoney", DbType.String,data.Rows[i]["交货单金额"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@zdh", DbType.String,data.Rows[i]["总单号"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@zxs", DbType.String,data.Rows[i]["总箱数"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@generatedtime", DbType.String,data.Rows[i]["生成时间"].ToString().Trim()),
                                SQLiteHelper.MakeSQLiteParameter("@scanningtime", DbType.String,data.Rows[i]["扫描时间"].ToString().Trim()),
                            };
                        list1.Add(parameters);
                    }
                }
                try
                {
                    if (fltinsert)
                        new SQLiteHelper().ExecuteNonQueryBatch(sb2.ToString(), list1);
                    if (fltupdate)
                        new SQLiteHelper().ExecuteNonQueryBatch(sb3.ToString(), list2);
                    MessageBox.Show("保存成功");
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }


        private void btn_Excel_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filter = "Excel文件(*.xls,*.xlsx)|*.xls;*.xlsx" };
            DialogResult result = dialog.ShowDialog();
            if (result.Equals(DialogResult.OK))
            {
                Workbook workbook = new Workbook();
                workbook.Worksheets.Clear();
                GenerateWorksheet(workbook, "数据查询", this.gridView2);

                workbook.Save(dialog.FileName);

                Process.Start(dialog.FileName);
            }
        }
    }
}
