using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace ScannerDataWindowsService
{
    public partial class ScannerDataService : ServiceBase
    {
        private static object locko = new object();
        System.Timers.Timer timer = new System.Timers.Timer();

        public ScannerDataService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            lock (locko)
            {
                Log.WriteLog("服务启动");
                //设置间隔时间
                int IntervalTime = 5;
                //1000毫秒=1秒，与间隔时间相乘，计算共多少毫秒
                string sc = ConfigurationManager.AppSettings["IntervalTime"];
                if (!string.IsNullOrEmpty(sc))
                {
                    IntervalTime = Convert.ToInt32(sc);
                }
                this.timer.Interval = 1000 * IntervalTime;
                timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
                timer.Enabled = true;
            }
        }



        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StartUploadDataAsync();
        }

        delegate void AsyncStartUploadData();

        private void StartUploadDataAsync()
        {
            AsyncStartUploadData caller = StartUploadData;
            caller.BeginInvoke(StartUploadDataBack, caller);
        }
        private void StartUploadDataBack(IAsyncResult ar)
        {
            var caller = (AsyncStartUploadData)ar.AsyncState;
            caller.EndInvoke(ar);
        }

        private void StartUploadData()
        {
            lock (locko)
            {
                try
                {
                    string sql = "select * from scanlabelrec where whetherupload = 0 and classify in ('总单','分单') and scanningtime !=''";
                    DataTable data = new SQLiteHelper().ExecuteQuery(sql);
                    if (data.Rows.Count > 0)
                    {
                        foreach (DataRow row in data.Rows)
                        {
                            string sltime = row["sltime"].ToString();
                            if (string.IsNullOrWhiteSpace(sltime))
                            {
                                row["sltime"] = "00:00:00";
                            }
                        }
                        string json = JsonConvert.SerializeObject(data);
                        string url = ConfigurationManager.AppSettings["upjosn"];

                        WebServiceHannan.HannanService service = new WebServiceHannan.HannanService();
                        service.Url = url;
                        service.Timeout = 600000;//定义请求时间10分钟
                        string retstr = service.SetHanNanJsonData(json);

                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                        dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(retstr);
                        if (dictionary.ContainsKey("Status"))
                        {
                            string retdatastr = string.Empty;
                            if (Convert.ToBoolean(dictionary["Status"].Trim()))
                            {
                                retdatastr = dictionary["Data"].Trim();
                                Log.WriteLog("传输json成功");
                            }
                            else
                            {
                                retdatastr = dictionary["Data"].Trim();
                                string message = dictionary["Message"].Trim();
                                Log.WriteLog(string.Format("传输json异常，{0}", message));
                            }
                            if (!string.IsNullOrWhiteSpace(retdatastr))
                            {
                                string sql2 = string.Format("update scanlabelrec set whetherupload={0} where oid in ({1})", 1, retdatastr);
                                int sqlrow = new SQLiteHelper().ExecuteNonQuery(sql2);
                                if (sqlrow > 0)
                                    Log.WriteLog("修改数据成功");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errInfo = ex.StackTrace;
                    Log.WriteLog("定时程序异常...." + errInfo.Substring(errInfo.LastIndexOf("\\") + 1, errInfo.Length - errInfo.LastIndexOf("\\") - 1) + "--------" + ex.Message);
                }
            }
        }


        protected override void OnStop()
        {
            lock (locko)
            {
                if (this.timer != null)
                {
                    this.timer.Enabled = false;
                    this.timer.Stop();
                    this.timer.Dispose();
                    this.timer.Close();
                }
                Log.WriteLog("服务停止");
            }
        }
    }
}
