using System;

namespace TunnelVision
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (TunnelVision game = new TunnelVision())
            {
                game.Run();
            }
        }
    }
}

