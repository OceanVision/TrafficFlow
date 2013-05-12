using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace TrafficFlow
{
    public class TerrainGraph
    {
        public class StreetInfo
        {
            public int ID;
            public Vertex start1;
            public List<Edge> segments1;

            public StreetInfo(int i)
            {
                ID = i;
                segments1 = new List<Edge>();
            }
        }

        public static Dictionary<string, StreetInfo> Streets = new Dictionary<string, StreetInfo>()
        {
            {"al. Solidarności", new StreetInfo(0)},
            {"al. Jana Pawła II", new StreetInfo(1)}
        };

        public class Vertex
        {
            public int ID;
            public int X, Y;
            public List<Edge> Adj;
            public bool visited;

            public Vertex(int id, int x, int y)
            {
                this.ID = id;
                this.X = x;
                this.Y = y;
                this.visited = false;
                this.Adj = new List<Edge>();
            }
        }

        public class Edge
        {
            public int street;
            public Color color;
            public Vertex target;

            public Edge(int s, Color c, Vertex u)
            {
                street = s;
                color = c;
                target = u;
            }
        }

        public List<Vertex> Vertices;

        public TerrainGraph()
        {
            this.Vertices = new List<Vertex>();
        }

        public int addVertex(int x, int y)
        {
            this.Vertices.Add(new Vertex(this.Vertices.Count - 1, x, y));
            return this.Vertices.Count - 1;
        }

        public void addEdge(int u, int v, string street, Color c)
        {
            Edge e = new Edge(Streets[street].ID, c, this.Vertices[v]);
            this.Vertices[u].Adj.Add(e);
            if (Streets[street].segments1.Count == 0)
                Streets[street].start1 = this.Vertices[u];
            Streets[street].segments1.Add(e);
        }

        public List<Tuple<int, int, Color>> getStreet(string street)
        {
            List<Tuple<int, int, Color>> result = new List<Tuple<int, int, Color>>();

            StreetInfo tmp = Streets[street];
            result.Add(new Tuple<int, int, Color>(tmp.start1.X, tmp.start1.Y, Color.Empty));
            foreach(Edge e in tmp.segments1)
                result.Add(new Tuple<int, int, Color>(e.target.X, e.target.Y, e.color));
            return result;
        }

        public List<Tuple<int, int>> getVertices()
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            foreach(Vertex u in this.Vertices)
                result.Add(new Tuple<int,int>(u.X, u.Y));
            return result;
        }

        public void buildSampleGraph()
        {
            this.addVertex(434, 517 - 22);
            this.addVertex(573, 442 - 22);
            this.addVertex(776, 332 - 22);
            this.addVertex(833, 301 - 22);
            this.addVertex(931, 251 - 22);
            this.addEdge(0, 1, "al. Solidarności", Color.FromArgb(100, 70, 70, 70));
            this.addEdge(1, 2, "al. Solidarności", Color.FromArgb(100, 70, 70, 70));
            this.addEdge(2, 3, "al. Solidarności", Color.FromArgb(170, Color.Firebrick));
            this.addEdge(3, 4, "al. Solidarności", Color.FromArgb(100, 70, 70, 70));

            this.addVertex(354, 25 - 22);
            this.addVertex(414, 138 - 22);
            this.addVertex(439, 186 - 22);
            this.addVertex(456, 227 - 22);
            this.addVertex(517, 341 - 22);
            this.addVertex(568, 429 - 22);
            this.addVertex(611, 517 - 22);
            this.addEdge(5, 6, "al. Jana Pawła II", Color.FromArgb(170, Color.Firebrick));
            this.addEdge(6, 7, "al. Jana Pawła II", Color.FromArgb(170, Color.Firebrick));
            this.addEdge(7, 8, "al. Jana Pawła II", Color.FromArgb(100, 70, 70, 70));
            this.addEdge(8, 9, "al. Jana Pawła II", Color.FromArgb(100, 70, 70, 70));
            this.addEdge(9, 10, "al. Jana Pawła II", Color.FromArgb(100, 70, 70, 70));
            this.addEdge(10, 1, "al. Jana Pawła II", Color.FromArgb(100, 70, 70, 70));
            this.addEdge(10, 11, "al. Jana Pawła II", Color.FromArgb(100, 70, 70, 70));
        }
    }
}
