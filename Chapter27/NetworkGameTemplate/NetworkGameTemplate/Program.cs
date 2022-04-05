using System;

namespace NetworkGameTemplate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (NetworkGameTemplate game = new NetworkGameTemplate())
            {
                game.Run();
            }
        }
    }
}

