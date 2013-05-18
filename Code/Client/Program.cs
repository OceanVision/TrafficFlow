using System;

namespace TrafficFlow
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Visualization game = new Visualization())
            {
                game.Run();
            }
        }
    }
#endif
}

