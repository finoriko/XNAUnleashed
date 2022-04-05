using System;

namespace SimpleGame
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (SimpleGame game = new SimpleGame())
            {
                game.Run();
            }
        }
    }
}

