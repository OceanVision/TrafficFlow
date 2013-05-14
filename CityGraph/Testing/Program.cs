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
            CityGraph testGraph = new CityGraph("D:/Programowanie/ABBITChallenge/CityGraph/tsf_graph.csv");
            Console.ReadLine();
        }
    }
}
