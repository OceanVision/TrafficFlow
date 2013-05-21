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
using PredictionServer;

namespace Graph
{
    /// <summary>
    /// 
    /// </summary>
    public class CityGraph
    {
        /// <summary>
        /// AdjecencyGraph from QuickGraph library
        /// </summary>
        private AdjacencyGraph<MyNode, MyEdge> graph;

        /// <summary>
        /// Dictionary with which you can get node id based on node openid
        /// </summary>
        private Dictionary<string, int> idDictionary = new Dictionary<string, int>();

        /// <summary>
        /// Dictionary of middle points belonging to links
        /// </summary>
        private Dictionary<int, List<int>> linksDictionary = new Dictionary<int, List<int>>();

        /// <summary>
        /// Dictionary of edges and their weights based on current data and predictions
        /// </summary>
        private Dictionary<MyEdge, double> weightsWithPredictions = new Dictionary<MyEdge, double>();

        /// <summary>
        /// Streams to read file with simulation data
        /// </summary>
        private double[][] simulationData;

        /// <summary>
        /// Contructor - creating graph and links dictionary
        /// </summary>
        /// <param name="graphPath">(.xml) graph file path</param>
        /// <param name="linksPath">(.csv) links file path</param>
        public CityGraph(String graphPath, String linksPath)
        {
            createGraphFromXML(graphPath);
            createLinksDictionary(linksPath);
        }

        /// <summary>
        /// Getter for graph.Edges
        /// </summary>
        /// <returns>graph.Edges</returns>
        public IEnumerable<MyEdge> getEdges()
        {
            return graph.Edges;
        }

        /// <summary>
        /// Creating graph
        /// </summary>
        /// <param name="graphPath">Path of file containing graph</param>
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

            try
            {
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
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        } // end function

        /// <summary>
        /// Creating dictionary of middle points in links
        /// Link is the set of edges on which we got data from TSF
        /// </summary>
        /// <param name="sourcePath">Path of file containing links</param>
        public void createLinksDictionary(String sourcePath)
        {
            List<int> links = new List<int>();  // list of nodes from one link

            string line;                        // one line of rebuild file
            string[] elements;                  // elements of that line
            int actualIndex = 1;                // index of actual link from file           
            int actualTime = 1;                 // time of actualIndex
            try
            {
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
                    foreach (MyEdge e in graph.OutEdges(graph.Vertices.ElementAt(idDictionary[elements[2]])))
                    {
                        if (e.endNode.openid == elements[3])
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
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        } // end function

        /// <summary>
        /// Gets data from simulation data file, start simulation 
        /// </summary>
        /// <param name="path">Simulation data file path</param>
        public void startSimulation(String path)
        {
            // fields used to read simulation data file
            String line;
            String[] elements;
            int actualTime;

            // structures to hold simulation data
            List<List<double>> listOfLists = new List<List<double>>();
            List<double> dataList = new List<double>();

            try
            {
                // default values for edges without predictions
                foreach (MyEdge e in graph.Edges)
                    weightsWithPredictions.Add(e, 40);

                // simulation data streams
                FileStream simulationDataStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader simulationDataReader = new StreamReader(path);

                // first line of description
                simulationDataReader.ReadLine();
                // reading first time mark
                line = simulationDataReader.ReadLine();
                elements = line.Split(' ');
                actualTime = Int32.Parse(elements[0]);

                do
                {
                    elements = line.Split(' ');

                    // if we are on the next time moment
                    if (Int32.Parse(elements[0]) != actualTime)
                    {
                        listOfLists.Add(dataList);
                        dataList.Clear();
                        actualTime = Int32.Parse(elements[0]);
                    }

                    // add new velovity to list
                    dataList.Add(Double.Parse(elements[2]));

                } while ((line = simulationDataReader.ReadLine()) != null);

                if (dataList.Count > 0)
                    listOfLists.Add(dataList);

                // create double[][] from List<List<int>>
                List<double[]> outList = new List<double[]>();

                foreach (List<double> list in listOfLists)
                    outList.Add(list.ToArray());

                simulationData = outList.ToArray();

                ServerCore.Instance.serverConfiguration = new ServerConfiguration { simulation_start_minute = 1 };
                ServerCore.Instance.getDaemon().Start();
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        }

        /// <summary>
        /// Fills in weightsWithPredictions dictionary with data from simulation data file 
        /// </summary>
        public void updateSimulationData()
        {
            // value for edge based on simulation data and AI predictions
            double value;
            // actual time of simulation
            int time;

            try
            {
                // get predictions from AI
                double[,] prediction = ServerCore.Instance.fetchPrediction();
                time = ServerCore.Instance.fetchCurrentTimeStamp();

                // for every link in simulation data
                for (int i = 0; i < simulationData[time].Length; i++)
                {
                    // base value from simulation data
                    value = simulationData[(time + 5) / 10][i];

                    // if there are predictions for this link
                    if (i < prediction.GetLength(1))
                    {
                        // get average value
                        int j;
                        for (j = 0; j < prediction.GetLength(0); j++)
                            value += prediction[j, i];
                        value /= (j + 2);
                    }

                    // based on links dictionary, set right values to weightsWithPredictions
                    for (int j = 0; j < linksDictionary[i].Count - 1; j++)
                    {
                        foreach (MyEdge e in graph.OutEdges(
                            graph.Vertices.ElementAt (idDictionary [linksDictionary [i].ElementAt (j).ToString ()] ) ) )
                        {
                            if (e.endNode.GetHashCode() == idDictionary[linksDictionary[i].ElementAt(j + 1).ToString()])
                            {
                                // value is based on velocity and edge distance
                                weightsWithPredictions[e] = value / e.distance;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        }

        /// <summary>
        /// Getter for weightsWithPredictions
        /// </summary>
        /// <returns>edge values</returns>
        public Dictionary<MyEdge, double> getEdgeValues()
        {
            return weightsWithPredictions;
        }

        /// <summary>
        /// Shortest path with all checkpoints
        /// </summary>
        /// <param name="checkPoints">Points on map which driver wants to visit</param>
        /// <returns>List of edges (shortest path) containing all check points</returns>
        public LinkedList<MyEdge> shortestPath(int[] checkPoints)
        {
            // to less points to find path, return empty list
            if (checkPoints.Length < 2)
                return (new LinkedList<MyEdge>());

            double minPathLength = Double.MaxValue;        // length of the best path
            double actualPathLength = 0;                   // length of present path
            List<MyEdge> minPath = new List<MyEdge>();     // the best path
            List<MyEdge> actualPath = new List<MyEdge>();  // present path

            try
            {
                // checkpoints without first and last (start and target are constant)
                int[] insidePoints = new int[checkPoints.Length - 2];
                Array.Copy(checkPoints, 1, insidePoints, 0, checkPoints.Length - 2);

                // getting all possible permutations and finding which is the best
                int[][] permutations = getPermutations(insidePoints);
                foreach (int[] perm in permutations)
                {
                    actualPath.Clear();
                    actualPathLength = 0;
                    // for each 2 points in present permutation, find shortest path
                    Array.Copy(perm, 0, checkPoints, 1, perm.Length);
                    for (int i = 0; i < checkPoints.Length - 1; i++)
                    {
                        // new part of path
                        MyEdge[] pathPart = shortestPathBetween(graph.Vertices.ElementAt(checkPoints[i]),
                                            graph.Vertices.ElementAt(checkPoints[i + 1]),
                                            weightsWithPredictions);

                        // add new part to existing path
                        foreach (MyEdge e in pathPart)
                        {
                            actualPath.Add(e);
                            actualPathLength += weightsWithPredictions[e];
                        }
                    }

                    // check if new path is better
                    if (actualPathLength < minPathLength)
                    {
                        minPathLength = actualPathLength;
                        minPath = new List<MyEdge>(actualPath);
                    }
                }

                // return the best path
                return new LinkedList<MyEdge>(minPath);
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        }

        /// <summary>
        /// Create all possible permutations from an array
        /// </summary>
        /// <param name="array">Argument array</param>
        /// <returns>Array of permutations arrays</returns>
        private int[][] getPermutations(int[] array)
        {
            try
            {
                // get permutations into permList
                List<List<int>> permList = new List<List<int>>();
                permRecursion(array.ToList(), new List<int>(), permList);

                // create int[][] from List<List<int>>
                List<int[]> outList = new List<int[]>();

                foreach (List<int> list in permList)
                    outList.Add(list.ToArray());

                return outList.ToArray();
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        }

        /// <summary>
        /// Recursion for permutations 
        /// </summary>
        /// <param name="toAdd">Elements waiting to be add to permutations</param>
        /// <param name="actualPerm">Actual existing permutation</param>
        /// <param name="permutations">List of permutations made so far</param>
        private void permRecursion(List<int> toAdd, List<int> actualPerm, List<List<int>> permutations)
        {
            try
            {
                // if we created permutation
                if (toAdd.Count() == 0)
                    // add it to the permutations list
                    permutations.Add(actualPerm);
                else
                {
                    // new elements of recursion
                    List<int> copyToAdd;
                    List<int> copyActualPerm;

                    // add new element and continue recursion
                    foreach (int v in toAdd)
                    {
                        copyToAdd = new List<int>(toAdd);
                        copyActualPerm = new List<int>(actualPerm);

                        copyActualPerm.Add(v);
                        copyToAdd.Remove(v);

                        permRecursion(copyToAdd, copyActualPerm, permutations);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        }

        /// <summary>
        /// Shortest path between 2 points 
        /// </summary>
        /// <param name="source">Paths start</param>
        /// <param name="target">Paths target</param>
        /// <param name="weights">Weights of edges</param>
        /// <returns>The edges of the shortest path</returns>
        private MyEdge[] shortestPathBetween(MyNode source, MyNode target, Dictionary<MyEdge, double> weights)
        {
            try
            {
                Func<MyEdge, double> edgeCost = AlgorithmExtensions.GetIndexer(weights);
                // compute shortest paths
                TryFunc<MyNode, IEnumerable<MyEdge>> tryGetPaths = graph.ShortestPathsDijkstra(edgeCost, source);

                // query path for given vertices
                List<MyEdge> outPath = new List<MyEdge>();
                IEnumerable<MyEdge> path = null;

                tryGetPaths(target, out path);

                foreach (var edge in path)
                    outPath.Add(edge);

                return outPath.ToArray();
            }
            catch (Exception e)
            {
                Logger.Instance.log(e.ToString());
                throw (e);
            }
        }

        public void rebuildAvgVelocity(String toRebuild, String outPath)
        {
            string line;                        // one line of rebuild file
            string[] elements;                  // elements of that line
            int actualIndex = 1;                // index of actual link from file           
            int actualTime = 60;                // time of actualIndex
            double congestion = 0;              // summary congestion at link
            double velocity = 0;                // summary velocity at link
            double weight = 0;                  // weight(sum of distances) of edges from link

            // creating out files
            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
            FileStream tmp1 = File.Create(outPath);
            tmp1.Close();

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
                foreach (MyEdge e in graph.OutEdges(graph.Vertices.ElementAt(idDictionary[elements[2]])))
                {
                    if (e.endNode.openid == elements[3])
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
                foreach (MyEdge e in graph.OutEdges(graph.Vertices.ElementAt(idDictionary[elements[2]])))
                {
                    if (e.endNode.openid == elements[3])
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

        public void matrixData(string dataPath, string outPath)
        {
            string line;            // one line of rebuild file
            string[] elements;      // elements of that line         
            int actualTime;         // time of actualIndex

            // creating out file
            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
            FileStream tmp = File.Create(outPath);
            tmp.Close();

            FileStream sourceStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader dataReader = new StreamReader(sourceStream);

            FileStream outStream = new FileStream(outPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter matrixWriter = new StreamWriter(outStream);

            // first line of description
            dataReader.ReadLine();

            line = dataReader.ReadLine();
            elements = line.Split(' ');
            actualTime = Int32.Parse(elements[0]);

            // read file to it's end
            do
            {
                elements = line.Split(' ');

                // if we are on the next time mark
                if (Int32.Parse(elements[0]) != actualTime)
                {
                    matrixWriter.WriteLine();
                    actualTime = Int32.Parse(elements[0]);
                }

                // write value
                matrixWriter.Write(elements[2] + " ");

            } while ((line = dataReader.ReadLine()) != null);

            dataReader.Close();
            sourceStream.Close();

            matrixWriter.Close();
            outStream.Close();
        }

    } // end class
} // end namespace
