using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataCollect.Services
{
    public class ResponseMessageEventArgs : EventArgs
    {
        public String ResponseText { get; set; }

        public String ResultMsg { get; set; }
    }
}
