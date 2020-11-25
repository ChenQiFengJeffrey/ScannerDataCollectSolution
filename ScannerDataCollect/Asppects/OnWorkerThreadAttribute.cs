using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScannerDataCollect.Asppects
{
    public class OnWorkerThreadAttribute : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            ThreadPool.QueueUserWorkItem(state => invocation.Proceed());
        }
    }
}
