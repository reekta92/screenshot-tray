// ownership of this project belongs to: github.com/reekta92 (Mehmet Dag)

using System;
using System.Threading;
using System.Windows.Forms;

namespace screenCap
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set up global exception handling
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Use a mutex to ensure only one instance runs
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "screenshot_tool_instance", out createdNew))
            {
                if (createdNew)
                {
                    Application.Run(new Form1());
                }
                else
                {
                    MessageBox.Show("Screenshot Tool is already running!", "Information", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Unexpected error: {e.Exception.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Fatal error: {(e.ExceptionObject as Exception)?.Message}", "Fatal Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}


