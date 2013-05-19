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
        // AdjecencyGraph from QuickGraph library
        private AdjacencyGraph<MyNode, MyEdge> graph;
        
        // dictionary with which you can get node id based on node openid
        private Dictionary<string, int> idDictionary = new Dictionary<string, int>();
        
        // dictionary of middle points belonging to links
        Dictionary<int, List<int>> linksDictionary = new Dictionary<int, List<int>>();

        public CityGraph(String graphPath, String linksPath)
        {
            createGraphFromXML(graphPath);
            createLinksDictionary(linksPath);
        }

        // creating graph from XML file
        public void createGraphFromXML(String graphPath)
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

            XmlTextReader xmlReader = new XmlTextReader(graphPath);

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
                                latitudeDic[id] = double.Parse(xmlReader.Value.Replace(".", ","));
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
                    graph.AddVertex(new MyNode(id, latitudeDic[id], longitudeDic[id], openIdDic[id]));
                    idDictionary.Add(openIdDic[id], id);
                } //end if
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "edge")
                {
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
                        } // end if
                    } // end while
                    graph.AddEdge(new MyEdge(
                        graph.Vertices.ElementAt(startNode),
                        graph.Vertices.ElementAt(endNode),
                    distance, lanes, avgcard));
                } // end if
            } // end while
        } // end function

        // creating dictionary of middle points on links
        public void createLinksDictionary(String sourcePath)
        {
            List<int> links = new List<int>();  // list of nodes from one link

            string line;                        // one line of rebuild file
            string[] elements;                  // elements of that line
            int actualIndex = 1;                // index of actual link from file           
            int actualTime = 1;                 // time of actualIndex

            FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader linksReader = new StreamReader(sourceStream);

            // first line of description
            linksReader.ReadLine();

            // read file to it's end
            while ((line = linksReader.ReadLine()) != null)
            {
                elements = line.Split(' ');

                // if we are on the next link
                if (Int32.Parse(elements[1]) != actualIndex)
                {
                    // add link to dictionary
                    linksDictionary.Add(actualIndex, links);

                    // if we have added all links
                    if (actualTime != Int32.Parse(elements[0]))
                        break; ;

                    // clear list
                    links.Clear();
                    // add first element of new link
                    links.Add(Int32.Parse(elements[2]));

                    // update parameters
                    actualIndex = Int32.Parse(elements[1]);
                    actualTime = Int32.Parse(elements[0]);
                }

                // searching edge from file in the graph
                foreach(MyEdge e in graph.OutEdges(graph.Vertices.ElementAt(idDictionary[elements[2]])))
                {
                    if(e.endNode.openid == elements[3]) 
                    {
                        // add new node to our list
                        links.Add(Int32.Parse(elements[3]));
                        break;
                    }
                }
            } // end while
            
            // last link
            if (line == null)
                linksDictionary.Add(actualIndex, links);

            linksReader.Close();
            sourceStream.Close();
        } // end function

        public void rebuildAvgVelocity(String toRebuild, String outPath)
        {
            string line;                        // one line of rebuild file
            string[] elements;                  // elements of that line
            int actualIndex = 1;                // index of actual link from file           
            int actualTime = 60;                 // time of actualIndex
            double congestion = 0;              // summary congestion at link
            double velocity = 0;                // summary velocity at link
            double weight = 0;                  // weight(sum of distances) of edges from link

            // creating out file
            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
            FileStream tmp = File.Create(outPath);
            tmp.Close();

            FileStream sourceStream = new FileStream(toRebuild, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader linksReader = new StreamReader(sourceStream);

            FileStream outStream = new FileStream(outPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter linksWriter = new StreamWriter(outStream);

            // first line of description
            linksReader.ReadLine();
            linksWriter.WriteLine("Time Name Velocity NrOfCars");

            // read file to it's end
            while ((line = linksReader.ReadLine()) != null)
            {
                elements = line.Split(' ');
                
                // if we are on the next link
                if (Int32.Parse(elements[1]) != actualIndex)
                {
                    linksWriter.WriteLine(actualTime + " " + actualIndex + " " + Math.Round(velocity / weight, 3) + " " +
                        Math.Round(congestion / weight, 3));
                    congestion = velocity = weight = 0;

                    actualIndex = Int32.Parse(elements[1]);
                    actualTime = Int32.Parse(elements[0]);
                }

                // searching edge from file in the graph
                foreach(MyEdge e in graph.OutEdges(graph.Vertices.ElementAt(idDictionary[elements[2]])))
                {
                    if(e.endNode.openid == elements[3]) 
                    {
                        // update parameters
                        congestion += e.distance * Double.Parse(elements[5]);
                        velocity += e.distance * Double.Parse(elements[4]);
                        weight += e.distance;
                        break;
                    }
                }
            } // end while

            // last link
            linksWriter.WriteLine(actualTime + " " + actualIndex + " " + Math.Round(velocity / weight, 3) + " " +
                Math.Round(congestion / weight, 3));

            linksReader.Close();
            sourceStream.Close();
            
            linksWriter.Close();
            outStream.Close();
        } // end function

        public void rebuildCongestion(String toRebuild, String outPath)
        {
            string line;            // one line of rebuild file
            string[] elements;      // elements of that line
            int actualIndex = 1;    // index of actual link from file           
            int actualTime = 1;     // time of actualIndex
            double congestion = 0;  // summary congestion at link
            double weight = 0;      // weight(sum of distances) of edges from link

            // creating out file
            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
            FileStream tmp = File.Create(outPath);
            tmp.Close();

            FileStream sourceStream = new FileStream(toRebuild, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader linksReader = new StreamReader(sourceStream);

            FileStream outStream = new FileStream(outPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter linksWriter = new StreamWriter(outStream);

            // first line of description
            linksReader.ReadLine();
            linksWriter.WriteLine("Time(minutes) Name NrOfCars");

            // read file to it's end
            while ((line = linksReader.ReadLine()) != null)
            {
                elements = line.Split(' ');
                
                // if we are on the next link
                if (Int32.Parse(elements[1]) != actualIndex)
                {
                    linksWriter.WriteLine(actualTime + " " + actualIndex + " " + Math.Round(congestion / weight, 3));
                    congestion = weight = 0;

                    actualIndex = Int32.Parse(elements[1]);
                    actualTime = Int32.Parse(elements[0]);
                }

                // searching edge from file in the graph
                foreach(MyEdge e in graph.OutEdges(graph.Vertices.ElementAt(idDictionary[elements[2]])))
                {
                    if(e.endNode.openid == elements[3]) 
                    {
                        // update parameters
                        congestion += e.distance * Double.Parse(elements[4]);
                        weight += e.distance;
                        break;
                    }
                }
            } // end while

            // last link
            linksWriter.WriteLine(actualTime + " " + actualIndex + " " + Math.Round(congestion / weight, 3));

            linksReader.Close();
            sourceStream.Close();
            
            linksWriter.Close();
            outStream.Close();
        } // end function
    } // end class
} // end namespace
