using System;
using System.Diagnostics;
using System.Reflection;

namespace Space
{
    /// <summary>
    /// Program entry.
    /// </summary>
    partial class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new Program())
            {
                // Get some general system information, for reference.
                var assembly = Assembly.GetExecutingAssembly().GetName();
#if DEBUG
                const string build = "Debug";
#else
                const string build = "Release";
#endif
                Logger.Info("--------------------------------------------------------------------------------");
                Logger.Info("{0} {1} (Attached debugger: {2}) running under {3}",
                            assembly.Name, build, Debugger.IsAttached, Environment.OSVersion.VersionString);
                Logger.Info("Build Version: {0}", assembly.Version);
                Logger.Info("CLR Version: {0}", Environment.Version);
                Logger.Info("CPU Count: {0}", Environment.ProcessorCount);
                Logger.Info("Starting up...");
                game.Run();
                Logger.Info("Shutting down...");
            }
        }
    }
}
