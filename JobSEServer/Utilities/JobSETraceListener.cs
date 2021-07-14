using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Utilities
{
    public class JobSETraceListener : DefaultTraceListener
    {
        public override void Write(string message)
        {
            base.Write(string.Format("[{0}] ", DateTime.Now.ToString("s")));
            base.Write(message);
        }

        protected override void Dispose(bool disposing)
        {
            WriteLine(string.Format("------------ [{0}] Server Stopped -------------", DateTime.Now.ToString("s")));
            base.Dispose(disposing);
        }
    }
}
