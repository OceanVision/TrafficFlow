using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Serialization;
using System.Xml;

namespace Graph
{
    public class CityGraph
    {
        private AdjacencyGraph<MyNode, MyEdge> graph;

        public CityGraph(String path)
        {
            int id = 0;
            Dictionary<int, double> latitudeDic = new Dictionary<int, double>();
            Dictionary<int, double> longitudeDic = new Dictionary<int, double>();
            Dictionary<int, string> openIdDic = new Dictionary<int, string>();
            int startNode = 0;
            int endNode = 0;
            double distance = 0;
            double lanes = 0;
            double avgcard = 0;

            XmlTextReader xmlReader = new XmlTextReader(path); 
 
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "graph")
                {
                    graph = new AdjacencyGraph<MyNode, MyEdge>(true);
                    break;
                }
            }

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "node")
                {
                    id = Int32.Parse(xmlReader.GetAttribute("id").Substring(1));
                    while (xmlReader.NodeType != XmlNodeType.EndElement)
                    {
                        xmlReader.Read();
                        if (xmlReader.Name == "data")
                        {
                            if (xmlReader.GetAttribute("key") == "v_latitude")
                            {
                                xmlReader.Read();
                                latitudeDic[id] = double.Parse(xmlReader.Value.Replace(".",","));
                            }
                            if (xmlReader.GetAttribute("key") == "v_longitude")
                            {
                                xmlReader.Read();
                                longitudeDic[id] = double.Parse(xmlReader.Value.Replace(".", ","));
                            }
                            if (xmlReader.GetAttribute("key") == "v_openid")
                            {
                                xmlReader.Read();
                                openIdDic[id] = xmlReader.Value.Replace(".", ",");
                            }
                            xmlReader.Read();
                            xmlReader.Read();
                        } //end if
                    } // end while
                    graph.AddVertex(new MyNode(id, longitudeDic[id], longitudeDic[id], openIdDic[id]));
                } //end if
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "edge")
                {
                    if (graph.EdgeCount % 1000 == 0) Console.WriteLine(graph.EdgeCount);
                    startNode = Int32.Parse(xmlReader.GetAttribute("source").Substring(1));
                    endNode = Int32.Parse(xmlReader.GetAttribute("target").Substring(1));
                    while (xmlReader.NodeType != XmlNodeType.EndElement)
                    {
                        xmlReader.Read();
                        if (xmlReader.Name == "data")
                        {
                            if (xmlReader.GetAttribute("key") == "e_distance")
                            {
                                xmlReader.Read();
                                distance = double.Parse(xmlReader.Value.Replace(".", ","));

                            }
                            if (xmlReader.GetAttribute("key") == "e_lanes")
                            {
                                xmlReader.Read();
                                lanes = double.Parse(xmlReader.Value.Replace(".", ","));
                            }
                            if (xmlReader.GetAttribute("key") == "e_avgcard")
                            {
                                xmlReader.Read();
                                avgcard = double.Parse(xmlReader.Value.Replace(".", ","));
                            }
                            xmlReader.Read();
                            xmlReader.Read();
                        } //end if
                    } // end while
                    graph.AddEdge(new MyEdge(
                        graph.Vertices.ElementAt(startNode),
                        graph.Vertices.ElementAt(endNode),
                    distance, lanes, avgcard));
                } //end if
            } // end while
        }
    }
}
