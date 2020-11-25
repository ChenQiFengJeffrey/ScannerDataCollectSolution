using ScannerDataCollect.Common;
using ScannerDataCollect.Components;
using ScannerDataCollectServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataCollect.Services
{
    public enum SvrStatus
    {
        Starting,
        Started,
        Stoped,
        Stoping
    }
    public class SvrUtils
    {
        public delegate void GetResponseHandle(object sender, ResponseMessageEventArgs e);
        public event GetResponseHandle OnGetResponse;
        public delegate void ServiceStartedHandle();
        public event ServiceStartedHandle OnStarted;
        public delegate void ServiceStartingHandle();
        public event ServiceStartingHandle OnStarting;
        public delegate void ServiceStopedHandle();
        public event ServiceStopedHandle OnStoped;
        public delegate void ServiceStoppingHandle();
        public event ServiceStoppingHandle OnStopping;

        private static object objectlockCheck = new Object();
        private static volatile SvrUtils instance;

        public SvrStatus Status { get; set; }

        public bool isServiceStart { get { return Status.Equals(SvrStatus.Started); } }
        public List<string> ListenerIPList { get; set; }
        public ServiceHost ServiceHost { get; set; }

        private SvrUtils()
        {
            ListenerIPList = new List<string>();
            Status = SvrStatus.Stoped;
        }

        /// <summary>
        /// 获取实例
        /// </summary>
        public static SvrUtils Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objectlockCheck)
                    {
                        if (instance == null)
                            instance = new SvrUtils();
                    }
                }

                return instance;
            }
        }

        public void Start()
        {
            if (ServiceHost == null)
            {
                try
                {
                    Uri ipaddr = new Uri(string.Format("http://{0}:8000/calculator", "localhost"));
                    DataService dataService = new DataService();
                    ServiceHost = new ServiceHost(dataService, ipaddr);
                    dataService.ReceiveContent += ReceiveContent;
                    ServiceHost.Opened += ServiceHost_Opened;
                    ServiceHost.Closed += ServiceHost_Closed;
                    ServiceHost.Opening += ServiceHost_Opening;
                    ServiceHost.Closing += ServiceHost_Closing;
                    ServiceHost.AddServiceEndpoint(
                        typeof(ScannerDataCollectServices.IDataService),
                        new BasicHttpBinding(),
                        "Calculator");

                    // Enable metadata exchange - this is needed for NetCfSvcUtil to discover us
                    ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                    smb.HttpGetEnabled = true;
                    ServiceHost.Description.Behaviors.Add(smb);
                    ServiceHost.Open();

                    
                }
                catch
                {
                    Abort();
                }
            }
        }

        public void Abort()
        {
            if(this.ServiceHost != null)
            {
                this.ServiceHost.Abort();
                this.ServiceHost = null;
            }
        }

        public void Close()
        {
            if (this.ServiceHost != null)
            {
                this.ServiceHost.Close();
                this.ServiceHost = null;
            }
        }

        private void ReceiveContent(String value, out String msg)
        {
            msg = String.Empty;
            if (OnGetResponse != null)
            {
                ResponseMessageEventArgs arg = new ResponseMessageEventArgs() { ResponseText = value };
                OnGetResponse(this, arg);
                msg = arg.ResultMsg;
            }

        }

        private void ServiceHost_Opened(object sender, EventArgs e)
        {
            ListenerIPList = NetworkUtils.GetLocalIP();
            Status = SvrStatus.Started;
            if (OnStarted != null)
            {
                OnStarted();
            }
        }

        private void ServiceHost_Closed(object sender, EventArgs e)
        {
            ListenerIPList.Clear();
            Status = SvrStatus.Stoped;
            if (OnStoped != null)
            {
                OnStoped();
            }
        }

        private void ServiceHost_Opening(object sender, EventArgs e)
        {
            Status = SvrStatus.Starting;
            if (OnStarting != null)
            {
                OnStarting();
            }
        }

        private void ServiceHost_Closing(object sender, EventArgs e)
        {
            Status = SvrStatus.Stoping;
            if (OnStopping != null)
            {
                OnStopping();
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
    }
}
