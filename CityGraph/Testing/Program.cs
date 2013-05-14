using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graph;

namespace Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            // File path: .../ABBITChallenge/CityGraph/tsf_graph.csv
            KnowledgeGraph knowldgGraph = new KnowledgeGraph("D:/Programowanie/ABBITChallenge/CityGraph/tsf_graph.csv");
            // File path: .../ABBITChallenge/CityGraph/abstract_links.txt
            AbstractGraph abstrctGraph = new AbstractGraph("D:/Programowanie/ABBITChallenge/CityGraph/abstract_links.txt");
            Console.ReadLine();
        }
    }
}
