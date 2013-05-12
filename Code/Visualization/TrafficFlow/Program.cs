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
            G.addVertex(444, 114 - 22);
            G.addEdge(0, G.addVertex(384, 250 - 22), "Aleja Trzech Wieszczów");
            G.addEdge(1, G.addVertex(359, 342 - 22), "Aleja Trzech Wieszczów");
            G.addEdge(2, G.addVertex(48, 214 - 22), "Czarnowiejska");
            drawCurve(Color.FromArgb(90, 50, 50, 50), G.streetCurve("Aleja Trzech Wieszczów"));
            drawCurve(Color.FromArgb(170, 178, 34, 34), G.streetCurve("Czarnowiejska"));
        }

        public static void drawCurve(Color color, List<Tuple<int, int>> coor)
        {
            Brush brush = new SolidBrush(color);
            Pen pen = new Pen(color, 5);
            Graphics graphics = form.picture.CreateGraphics();
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            for (int i = 1; i < coor.Count; ++i)
                graphics.DrawLine(pen, coor[i - 1].Item1, coor[i - 1].Item2, coor[i].Item1, coor[i].Item2);
            pen.Dispose();
            graphics.Dispose();
        }
    }
}
