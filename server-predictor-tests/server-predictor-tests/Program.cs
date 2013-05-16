using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDotNet;
using System.Collections;
using System.IO;
using QuickGraph;
using QuickGraph.Serialization;
using System.Xml;
namespace server_predictor_tests
{

    class TrafficPredictionModel1
    {
        private int step, max, states_count;
        private int[] states;
        public TrafficPredictionModel1() { }

        Object[] abstract_edges_mapping; //mapping of the abstract edges onto the AbstractGraph

        private SingularModelR smrCars; //modelling cars
        private SingularModelR smrVelocity; //modelling velocity

        //TODO: add parameter setting
        private Dictionary<string, Object> modelOptions;

        //Initialize
        public void init(int step = 10, int max = 100){  
            // step/max - responsible for setting discretization steps //
            smrVelocity = new SingularModelR("model0b.r", "model0b", "smrVelocity");
            smrVelocity.init();
            smrCars = new SingularModelR("model0b.r","model0b","smrCars");
            smrCars.init(); //static problem - hopefully will be resolved in the next version of R.NET library

            this.step = step; 
            this.max = max;
            
            states_count = (int)(max / (float)step)+1;
            states = new int[states_count];
            for (int i = 0; i < states_count; ++i)
                states[i] = step * i;

        }

        //TODO: Change to send 2D Matrix instead of file name - just for prototyping
        public void train(string train_file_cars, string train_file_velocity)
        {
            smrCars.train_model(train_file_cars); //zapisze sobie do odpowiedniego pliku i mnie nie obchodzi jakiego 
            smrVelocity.train_model(train_file_velocity); //narazie wylaczone
        }




        
    }

    class Program
    {
        static void Main(string[] args)
        {
            //var g = new AdjacencyGraph<Tuple<int,float> , Edge<Tuple<int,float>>>();
            //using (var xreader = XmlReader.Create("tmp.xml"))
            //{
            //    g.DeserializeFromGraphML(xreader,
            //        (id, latitude) => new Tuple<int, float>(int.Parse(id.Substring(1, 1)), float.Parse(latitude.Substring(1, 1))),
            //        (source, target, id)  => new Edge<Tuple<int, float>>(10,10)
            //   );
           // }

          //  Logger.Instance.log("Graph loaded successfully from .xml");




            TrafficPredictionModel1 tpm1 = new TrafficPredictionModel1();
            
            try {
                tpm1.init();
                tpm1.train("data/model0b.data.RDa", "data/model0b.data.RDa");
            }
            catch(Exception e){
                Logger.Instance.log(e.ToString());
            }
            //engine.Evaluate("predict_options_tmp = model0b.getDefaultPredictOptions()");

            Console.ReadLine();



            while (true) ;
        }
    }
}
