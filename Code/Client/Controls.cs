using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace TrafficFlow
{
    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        private Button zoomIn, zoomOut;
        public Label debug;

        //initializes all controls
        public void initControls()
        {
            //zoom buttons initialization
            zoomIn = new Button();
            zoomIn.Text = "Przybliż";
            zoomIn.Size = new Size(100, 30);
            zoomIn.FlatStyle = FlatStyle.Flat;
            zoomIn.ForeColor = Color.FromArgb(55, 53, 53);
            zoomIn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            zoomIn.Location = new Point(Window.ClientBounds.Width - 130, Window.ClientBounds.Height - 100);
            Control.FromHandle(Window.Handle).Controls.Add(zoomIn);
            zoomIn.Click += new System.EventHandler(zoomIn_Click);

            zoomOut = new Button();
            zoomOut.Text = "Oddal";
            zoomOut.Size = new Size(100, 30);
            zoomOut.FlatStyle = FlatStyle.Flat;
            zoomOut.ForeColor = Color.FromArgb(55, 53, 53);
            zoomOut.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            zoomOut.Location = new Point(Window.ClientBounds.Width - 130, Window.ClientBounds.Height - 60);
            Control.FromHandle(Window.Handle).Controls.Add(zoomOut);
            zoomOut.Click += new System.EventHandler(zoomOut_Click);

            debug = new Label();
            debug.AutoSize = true;
            debug.Text = "Start";
            debug.Location = new Point(Window.ClientBounds.Width - 200, Window.ClientBounds.Height - 60);
            Control.FromHandle(Window.Handle).Controls.Add(debug);
        }

        private void zoomIn_Click(object sender, EventArgs e)
        {
            zoomUpdate(zoomInc);
        }

        private void zoomOut_Click(object sender, EventArgs e)
        {
            zoomUpdate(-zoomInc);
        }
    }
}
