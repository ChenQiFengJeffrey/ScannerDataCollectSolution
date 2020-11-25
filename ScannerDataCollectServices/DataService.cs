using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ScannerDataCollectServices
{
    public delegate void ReceiveContent(string content, out String msg);

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DataService : IDataService
    {
        public ReceiveContent ReceiveContent = null;

        public string Submit(string value)
        {
            String msg = String.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                if (null != ReceiveContent)
                    ReceiveContent(value, out msg);
            }
            return msg;
        }
    }
}
