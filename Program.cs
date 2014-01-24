using System;
using System.Windows.Forms;

namespace PEUtility
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string argument = (args.Length == 1) ? args[0] : null;
            Application.Run(new MainWindow(argument));
        }
    }
}
