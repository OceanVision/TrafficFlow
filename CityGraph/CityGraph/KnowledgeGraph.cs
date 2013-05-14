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
    public class KnowledgeGraph
    {
        private AdjacencyGraph<int, Edge<int>> graph;
        private Dictionary<int, double> latitude, longitude;
        private Dictionary<Edge<int>, double> distance;

        public KnowledgeGraph(string path)
        {
            graph = new AdjacencyGraph<int, Edge<int>>(true);
            latitude = new Dictionary<int, double>();
            longitude = new Dictionary<int, double>();
            distance = new Dictionary<Edge<int>, double>();

            try
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader fileReader = new StreamReader(fs);

                fileReader.ReadLine();
                fileReader.ReadLine();

                string newLine;
                string[] items;

                while ((newLine = fileReader.ReadLine()) != "Links:")
                {
                    items = newLine.Split(new String[] {" "}, StringSplitOptions.RemoveEmptyEntries);

                    graph.AddVertex(Convert.ToInt32(items[0]));
                    
                    latitude.Add(Convert.ToInt32 (items[0]), Double.Parse (items[1]));
                    longitude.Add(Convert.ToInt32 (items[0]), Double.Parse (items[2]));
                }

                newLine = fileReader.ReadLine();
                while ((newLine = fileReader.ReadLine()) != "Signals:")
                {
                    items = newLine.Split(new String[] {" "}, StringSplitOptions.RemoveEmptyEntries);

                    Edge<int> newEdge = new Edge<int>(Convert.ToInt32 (items[0]), Convert.ToInt32 (items[1]));
                    graph.AddEdge (newEdge);
                    
                    distance.Add (newEdge, Double.Parse(items[2]));
                }

                fileReader.Close();
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
