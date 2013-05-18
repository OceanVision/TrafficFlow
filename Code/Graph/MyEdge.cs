using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;

namespace Graph
{
    public class MyEdge : IEdge<MyNode>
    {
        private MyNode startNode { get; set; }
        private MyNode endNode { get; set; }
        private double distance { get; set; }
        private double lanes { get; set; }
        private double avgcard { get; set; }

        public MyEdge(MyNode start, MyNode end, double dist, double l, double ac)
        {
            startNode = start;
            endNode = end;
            distance = dist;
            lanes = l;
            avgcard = ac;
        }

        MyNode IEdge<MyNode>.Source
        {
            get { return startNode; }
        }

        MyNode IEdge<MyNode>.Target
        {
            get { return endNode; }
        }
    }
}
