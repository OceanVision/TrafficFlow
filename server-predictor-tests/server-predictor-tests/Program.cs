using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDotNet;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using QuickGraph;
using QuickGraph.Serialization;
using System.Xml;
using System.Xml.Schema;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;

using System.Diagnostics.Contracts;
using System.Threading;
namespace server_predictor_tests
{
   

    class Program
    {
        static void Main(string[] args)
        {

           Thread th =  PredictionDaemon.Instance.getDaemon();
           th.Priority = ThreadPriority.Highest;
           th.Start();


            while (true) ;
        }
    }
}
