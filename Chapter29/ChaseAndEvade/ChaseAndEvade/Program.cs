using System;

namespace ChaseAndEvade
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (ChaseAndEvade game = new ChaseAndEvade())
            {
                game.Run();
            }
        }
    }
}

