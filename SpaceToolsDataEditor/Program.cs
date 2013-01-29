using System;
using System.Windows.Forms;
using Engine.ComponentSystem;

namespace Space.Tools.DataEditor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Manager.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DataEditor());
        }
    }
}
