using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScannerDataCollect.Asppects
{
    public class OnGuiThreadAttribute : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Control control = ((Control)invocation.InvocationTarget);

            invocation.Proceed();
            if (!control.InvokeRequired)
            {
                // We are already in the GUI thread. Proceed.
                invocation.Proceed();
            }
            else
            {
                // Invoke the target method synchronously. 
                control.Invoke(new Action(invocation.Proceed));

            }
        }
    }
}
