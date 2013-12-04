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

            //Set server configuration, for demonstration purposes, server will be merged with the client//
            ServerCore.Instance.serverConfiguration = new ServerConfiguration { simulation_start_minute = 1 };
            ServerCore.Instance.getDaemon().Start();

            Logger.Instance.log("Waiting for data");
            Logger.Instance.log_2darray(ServerCore.Instance.fetchPrediction());
            Logger.Instance.log("Data available");
            while (true) ;
        }
    }
}
