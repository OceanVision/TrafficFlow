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
                ((System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(game.Window.Handle)).Icon = new System.Drawing.Icon("icon.ico");
                game.Run();
            }
        }
    }
#endif
}

