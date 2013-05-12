using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TrafficFlow
{
    public static class Program
    {
        public static Form1 form;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            form = new Form1();
            Application.Run(form);
        }

        public static void testDraw()
        {
            TerrainGraph G = new TerrainGraph();
            Visualization D = new Visualization();
            G.buildSampleGraph();
            
            drawCurve(G.getStreet("al. Jana Pawła II"));
            drawCurve(G.getStreet("al. Solidarności"));
            //drawVertices(G.getVertices());
        }

        public static void drawVertices(List<Tuple<int, int>> coor)
        {
            Pen pen = new Pen(Color.FromArgb(180, 0, 0, 0), 4);
            Graphics graphics = form.picture.CreateGraphics();
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 1; i < coor.Count; ++i)
                graphics.DrawEllipse(pen, coor[i].Item1 - 2, coor[i].Item2 - 2, 4, 4);

            pen.Dispose();
            graphics.Dispose();
        }

        public static void drawCurve(List<Tuple<int, int, Color>> coor)
        {
            Graphics graphics = form.picture.CreateGraphics();
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            for (int i = 1; i < coor.Count; ++i)
            {
                Pen pen = new Pen(coor[i].Item3, 5);
                graphics.DrawLine(pen, coor[i - 1].Item1, coor[i - 1].Item2, coor[i].Item1, coor[i].Item2);
                pen.Dispose();
            }
            graphics.Dispose();
        }
    }
}
