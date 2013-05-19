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
        private Button ZoomIn, ZoomOut, LoadGraph;
        public Label Debug;

        //initializes all controls
        public void InitControls()
        {
            //var form = (Form)Form.FromHandle(Window.Handle);
            //form.WindowState = FormWindowState.Maximized;

            //zoom buttons initialization
            ZoomIn = new Button();
            ZoomIn.Text = "Przybliż";
            ZoomIn.Size = new Size(100, 30);
            ZoomIn.FlatStyle = FlatStyle.Flat;
            ZoomIn.ForeColor = Color.FromArgb(55, 53, 53);
            ZoomIn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            ZoomIn.Location = new Point(Window.ClientBounds.Width - 130, Window.ClientBounds.Height - 100);
            Control.FromHandle(Window.Handle).Controls.Add(ZoomIn);
            ZoomIn.Click += new System.EventHandler(ZoomIn_Click);

            ZoomOut = new Button();
            ZoomOut.Text = "Oddal";
            ZoomOut.Size = new Size(100, 30);
            ZoomOut.FlatStyle = FlatStyle.Flat;
            ZoomOut.ForeColor = Color.FromArgb(55, 53, 53);
            ZoomOut.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            ZoomOut.Location = new Point(Window.ClientBounds.Width - 130, Window.ClientBounds.Height - 60);
            Control.FromHandle(Window.Handle).Controls.Add(ZoomOut);
            ZoomOut.Click += new System.EventHandler(ZoomOut_Click);

            LoadGraph = new Button();
            LoadGraph.Text = "Wczytaj graf";
            LoadGraph.Size = new Size(130, 30);
            LoadGraph.FlatStyle = FlatStyle.Flat;
            LoadGraph.ForeColor = Color.FromArgb(55, 53, 53);
            LoadGraph.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            LoadGraph.Location = new Point(30, Window.ClientBounds.Height - 60);
            Control.FromHandle(Window.Handle).Controls.Add(LoadGraph);
            LoadGraph.Click += new System.EventHandler(LoadGraph_Click);

            Debug = new Label();
            Debug.AutoSize = true;
            Debug.Text = "Start";
            Debug.Location = new Point(200, Window.ClientBounds.Height - 50);
            Control.FromHandle(Window.Handle).Controls.Add(Debug);
        }

        private void ZoomIn_Click(object sender, EventArgs e)
        {
            ZoomUpdate(true);
        }

        private void ZoomOut_Click(object sender, EventArgs e)
        {
            ZoomUpdate(false);
        }

        private void LoadGraph_Click(object sender, EventArgs e)
        {
            BuildNetwork();
        }

        private void Message(string m)
        {
            MessageBox.Show(m);
        }
    }
}
