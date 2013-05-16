using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Graph
{
    public class AbstractGraph
    {
        private AdjacencyGraph<int, Edge<int>> graph;
        private Dictionary<Edge<int>, int[]> middlePoints;

        public AbstractGraph (string path)
        {
            graph = new AdjacencyGraph<int, Edge<int>>(true);
            middlePoints = new Dictionary<Edge<int>, int[]>();

            try
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader fileReader = new StreamReader(fs);

                string newLine;
                string[] items;
                int[] vertices;

                while ((newLine = fileReader.ReadLine()) != null)
                {
                    items = newLine.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    vertices = new int[items.Length - 1];
                    for (int i = 1; i < items.Length; i++)
                    {
                        vertices[i - 1] = Convert.ToInt32(items[i]);
                    }

                    Edge<int> newEdge = new Edge<int>(vertices[0], vertices[vertices.Length - 1]);

                    graph.AddVerticesAndEdge(newEdge);

                    middlePoints.Add(newEdge, vertices);
                }

                fileReader.Close();
                fs.Close();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }
    }
}
