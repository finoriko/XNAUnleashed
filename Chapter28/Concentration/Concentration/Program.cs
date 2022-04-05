using System;

namespace Concentration
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Concentration game = new Concentration())
            {
                game.Run();
            }
        }
    }
}

