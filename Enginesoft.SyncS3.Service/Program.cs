using System;
using System.ServiceProcess;

namespace Enginesoft.SyncS3.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            if (System.Diagnostics.Debugger.IsAttached)
            {
                EntryService service = new EntryService();
                service.Start();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new EntryService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
                
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e == null || e.ExceptionObject == null)
                return;

            var exception = (Exception)e.ExceptionObject;
            ServiceUtils.WriteLog(string.Format("ERROR -- CurrentDomain_UnhandledException -- {0}", exception.Message));
        }
    }
}
