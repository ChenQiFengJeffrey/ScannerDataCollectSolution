using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataCollect.Services
{
    [ServiceContract(Namespace = "http://www.meritar.cn")]
    public interface IDataProcess
    {
        [OperationContract]
        String Submit(String value);
    }
}
