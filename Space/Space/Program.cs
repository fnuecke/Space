using System;
using System.Net;

namespace Space
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
       [STAThread]
        static void Main(string[] args)
        {
            using (Spaaace game = new Spaaace())
            {
                game.Run();
            }
        }
    }
#endif
}

