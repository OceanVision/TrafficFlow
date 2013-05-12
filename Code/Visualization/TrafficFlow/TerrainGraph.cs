using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficFlow
{
    public class TerrainGraph
    {
        public class StreetInfo
        {
            public int it;
            public int segments;

            public StreetInfo(int i)
            {
                it = i; segments = 0;
            }
        }

        public static Dictionary<string, StreetInfo> Streets = new Dictionary<string, StreetInfo>()
        {
            {"Aleja Trzech Wieszczów", new StreetInfo(0)},
            {"Czarnowiejska", new StreetInfo(1)}
        };

        public class Vertex
        {
            public int X, Y;
            public List<Edge> Adj;
            public bool visited;

            public Vertex(int x, int y)
            {
                this.X = x;
                this.Y = y;
                this.visited = false;
                this.Adj = new List<Edge>();
            }
        }

        public class Edge
        {
            public int street;
            public int color;
            public Vertex target;

            public Edge(int s, Vertex u)
            {
                street = s;
                color = 0;
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
            this.Vertices.Add(new Vertex(x, y));
            return this.Vertices.Count - 1;
        }

        public void addEdge(int u, int v, string street)
        {
            this.Vertices[u].Adj.Add(new Edge(Streets[street].it, this.Vertices[v]));
            Streets[street].segments++;
        }

        private void visitVertex(Vertex u, int street, List<Tuple<int, int>> result)
        {
            foreach (Edge e in u.Adj)
                if (!e.target.visited)
                {
                    if (e.street == street)
                    {
                        if (result.Count == 0)
                            result.Add(new Tuple<int, int>(u.X, u.Y));
                        result.Add(new Tuple<int, int>(e.target.X, e.target.Y));
                    }
                    visitVertex(e.target, street, result);
                }
            
            u.visited = true;
        }

        private void DFS(int street, List<Tuple<int, int>> result)
        {
            foreach (Vertex u in this.Vertices)
                u.visited = false;

            foreach (Vertex u in this.Vertices)
                if (!u.visited)
                    visitVertex(u, street, result);
        }

        public List<Tuple<int, int>> streetCurve(string street)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            this.DFS(Streets[street].it, result);
            return result;
        }
    }
}
