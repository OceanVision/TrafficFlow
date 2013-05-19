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

namespace server_predictor_tests
{
    class DataSystem
    {

        public string prepare_train_cars()
        {
            //constant in the demonstration version
            return "data/model0b.data.RDa";
        }
        public string prepare_train_velocity()
        {
            //constant in the demonstration version
            return "data/model0b.data.RDa";
        }
        public string prepare_train_pairwise_cars()
        {
            return "data/model0b.data.RDa";
        }
        public string prepare_train_pairwise_velocity()
        {
            return "data/model0b.data.RDa";
        }

        public double[][] readMatrixCsv(string file_name, bool header, int nrow, int ncol, string sep = " ")
        {
            double[][] matrix = new double[nrow][]; for (int i = 0; i < nrow; ++i) matrix[i] = new double[ncol];

            var reader = new StreamReader(File.OpenRead(file_name));
            if(header) reader.ReadLine();
            int row=0,col=0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(sep.ToCharArray());

                if (line == "") continue; //skip blanks

                try{
                     col=0;
                    foreach(string val in values) {
                        matrix[row][col++] = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    
                    Logger.Instance.log_ifn(ncol == col, "wrong column number specified!"); //warning message
                    ++row;
                }
                catch(Exception e){
                    //skip this line
                    
                }

                if (row == nrow) break;
            }
            return matrix;
        }

    }
    class TrafficPredictionModel1
    {
       
        //calculated basing on the model options
        
        public int[] states_cars, states_velocity;


        public SingularModelR smrCars; //modelling cars
        public SingularModelR smrVelocity; //modelling velocity
        public PairwiseModelR pmrCars;
        public PairwiseModelR pmrVelocity;

        public double[][] normalizeFactors; //loads after training models to the memory (init phase and after every train phase)

        //loaded or set during the construction
        Object[] abstract_edges_mapping; //mapping of the abstract edges onto the AbstractGraph
        
        public Dictionary<string, Object> modelOptions  = new Dictionary<string, Object>();

        private DataSystem ds;




        public TrafficPredictionModel1(DataSystem ds)
        {
            this.ds = ds;

            //will be calculated in the init() phase//
            modelOptions["Velocity_states_count"] = -1.0;
            modelOptions["Cars_states_count"] = -1.0;

            modelOptions["Velocity_step"] = 0.1;
            modelOptions["Velocity_max"] = 1.0;

            modelOptions["Cars_step"] = 0.1;
            modelOptions["Cars_max"] = 1.0;

            modelOptions["step_time"] = 5.0; //5 min prediction step

            //setup options, probably will remain static throught program//

            modelOptions["smrCars_ahead_data"] = 10.0;//
            modelOptions["smrCars_ahead"] = 2.0; //time that is to be predicted ahead (and returned by predict)
            modelOptions["smrCars_link_count"] = 20.0;
            modelOptions["smrCars_look_behind_prediction"] = 10.0;
            modelOptions["smrCars_look_behind"] = 30.0;
            modelOptions["smrCars_look_behind_prediction_self"] = 30.0;

            //~!-------------------------TODO: REPAIR MODEL0B.R----------------------!//
            modelOptions["smrCars_ahead_ret"] = 1.0;


            modelOptions["smrVelocity_ahead_data"] = 10.0;
            modelOptions["smrVelocity_ahead"] = 2.0;
            modelOptions["smrVelocity_link_count"] = 20.0;
            modelOptions["smrVelocity_look_behind_prediction"] = 10.0;
            modelOptions["smrVelocity_look_behind"] = 30.0;
            modelOptions["smrVelocity_look_behind_prediction_self"] = 30.0;           
        }

        //Initialize
        public void init()
        {  
            // step/max - responsible for setting discretization steps //
            smrVelocity = new SingularModelR("model0b.r", "model0b", "smrVelocity");
        
            //For the demo version: only one value prediction//
           // smrVelocity.init(
           //    getIntOpt("Velocity","look_behind") ,
           //    getIntOpt("Velocity","link_count") ,
          //     getIntOpt("Velocity","look_behind_prediction_self") ,
           //    getIntOpt("Velocity","look_behind_prediction") ,
           //    getIntOpt("Velocity","ahead") 
           // );


            smrCars = new SingularModelR("model0b.r","model0b","smrCars");

            smrCars.init(
               getIntOpt("Cars", "look_behind"),
               getIntOpt("Cars", "link_count"),
               getIntOpt("Cars", "look_behind_prediction_self"),
               getIntOpt("Cars", "look_behind_prediction"),
               getIntOpt("Cars", "ahead")
            );


            pmrCars = new PairwiseModelR("pmrCars");
            pmrVelocity = new PairwiseModelR("pmrVelocity");

            PairwiseModelR.engine = SingularModelR.engine; //R.NET library limitations

            pmrCars.init();
            pmrVelocity.init();

            Logger.Instance.log("SingularModels init completed");

            int states_count_cars = (int)(getDblOpt("Cars", "max") / (float)(getDblOpt("Cars","step"))) + 1;
           
            states_cars = new int[states_count_cars];
            for (int i = 0; i < states_count_cars; ++i)
                states_cars[i] = (getIntOpt("Cars","step")) * i;

            modelOptions["Cars_states_count"] = (double)states_count_cars;


            int states_count_velocity = (int)(getDblOpt("Velocity", "max") / (float)(getDblOpt("Velocity", "step"))) + 1;

            states_velocity = new int[states_count_cars];
            for (int i = 0; i < states_count_cars; ++i)
                states_velocity[i] = (getIntOpt("Velocity", "step")) * i;

            modelOptions["Velocity_states_count"] = (double)states_count_velocity;

            //!----- 2->4 -----!//
            try
            {
                this.normalizeFactors = ds.readMatrixCsv("data/general.normalize_factors.txt", false, 2, getIntOpt("Cars", "link_count"));
            }
            catch (Exception e)
            {
                Logger.Instance.log_error("Error reading normalize factors " + e.ToString());
                throw (e);
            }
            Console.WriteLine(this.normalizeFactors[1][1]);

        }

        //in the demonstration version files in .RDa
        public void train(string train_file_cars, string train_file_velocity)
        {
            smrCars.train_model(train_file_cars); //zapisze sobie do odpowiedniego pliku i mnie nie obchodzi jakiego 
            smrVelocity.train_model(train_file_velocity); //narazie wylaczone
        }

        //setup_graph is extracted, because some usage may involve multiple infering
        //subnetwork_name = 'Cars' or 'Velocity'
        public Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[] > setupSubnetwork(string subnetwork_name) 
        {
            // =======Prepare 2BN Slice of the subnetwork)================ //
            Variable<int>[] Y_before = new Variable<int>[getIntOpt(subnetwork_name,"link_count") + 1];
            Variable<int>[] Y_next = new Variable<int>[getIntOpt(subnetwork_name, "link_count") + 1];
           // Variable<IDistribution<int>>[] Y_next_prior = new Variable<IDistribution<int>>[(int)modelOptions["smr" + subnetwork_name + "_link_count"] + 1];



            Variable<Vector>[] Y_next_prior = new Variable<Vector>[getIntOpt(subnetwork_name,"link_count")  + 1];
            
            try
            {

                for (int i = 1; i <= getIntOpt(subnetwork_name, "link_count"); ++i)
                {
                    //will be observed
                    Y_before[i] = Variable.New<int>();// Variable<int>.Discrete(); //Variable.Discrete<int>.Named(subnetwork_name + "Y_before" + (i + 1).ToString()); //observed

                  //  Y_next[i] = Variable<int>.Discrete(); ;// Variable<int>().Named(subnetwork_name + "Y_next" + (i + 1).ToString()); //observed
                    
                    //will be observed
                    Y_next_prior[i] = Variable.New<Vector>();

                    Y_next[i] = Variable.Discrete(Y_next_prior[i]);
                    //initialize using prior
                    //Y_next[i].InitialiseTo(Y_next_prior[i]);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.log("Error initializing Y_before and Y_next variables for the sub_network "+subnetwork_name);
            }
            Logger.Instance.log("Correctly initialized Y_before and Y_next variables for the sub_network " + subnetwork_name);



            // =======Initialize pairwise calculation (probably load into the memory a data sample)================ //
            if(subnetwork_name == "Cars")
                pmrCars.initPairwiseCalculation(ds.prepare_train_pairwise_cars());
            else if(subnetwork_name == "Velocity")
                pmrVelocity.initPairwiseCalculation(ds.prepare_train_pairwise_cars());
            else
                throw(new Exception("Not supported subnetwork name"));


            //==========Calculate pairwise factors and represent in Infer.NET structure =====================//
            try
            {
                /*
                for (int i = 1; i <= getIntOpt(subnetwork_name,"link_count"); ++i)
                {
                    Console.WriteLine("");
                    //TODO: transport to another module this functionality
                   // double[] neigh = subnetwork_name == "Cars" ? smrCars.getMostImportantNeigh(i) : smrVelocity.getMostImportantNeigh(i);

                    //Logger.Instance.log_array(neigh);

                    

                    for (int j = i+1; j <= getIntOpt(subnetwork_name,"link_count"); ++j)
                    {
                        if (i >= neigh[j]) continue;
                        double[] factor = pmrCars.getPairwiseFactor(i, (int)neigh[j], (int)modelOptions[subnetwork_name+"_step"], ((int)modelOptions[subnetwork_name+"_states_count"]));

                
                        //write constraints
                        for (int k = 0; k < factor.Length; ++k)
                        {

                            Console.WriteLine(String.Format("Constrain {0}:{1} with {2}:{3} prob {4}",
                                    i,
                                     (int)(k / (float)((int)modelOptions[subnetwork_name + "_states_count"])),
                                     ((int)neigh[j]),
                                     k - ((int)modelOptions[subnetwork_name + "_states_count"]) * (int)(k / (float)((int)modelOptions[subnetwork_name + "_states_count"]))
                                   , factor[k]

                                     ));



                            Variable.ConstrainEqualRandom(

                             (Y_next[i] == (int)(k / (float)((int)modelOptions[subnetwork_name + "_states_count"]))) &

                             (Y_next[((int)neigh[j])] == k - ((int)modelOptions[subnetwork_name + "_states_count"]) * (int)(k / (float)((int)modelOptions[subnetwork_name + "_states_count"]))),

                             Bernoulli.FromLogOdds(Math.Log(factor[k])));


                        }
                    }
                }
                 */
            }
            catch (Exception e)
            {
                Logger.Instance.log("Error while calculating pairwise factors for "+subnetwork_name);
                throw (e);
            }
            Logger.Instance.log("Calculated pairwise factors for cars correctly");



            // return results as a tuple //
            return new Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[]>(Y_before, Y_next, Y_next_prior);
        }



        public void makePrediction() //(AbstractGraph to fill)
        {
            Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[]> dbnCars = this.setupSubnetwork("Cars");


             //!-------REMOVETHISLINE------!//
             //artificially get data from TSF test set//
             double[][] X_transposed = ds.readMatrixCsv("data/traffic_test.txt", false, getIntOpt("Cars", "look_behind"), getIntOpt("Cars", "link_count"));
             double[] X = new double[X_transposed.Length * X_transposed[0].Length];
             int cnt = 0;
             for (int i = 0; i < X_transposed[0].Length; ++i)
             {
                 for (int j = 0; j < X_transposed.Length; ++j)
                 {
                     X[cnt++] = X_transposed[j][i];
                 }
             }
             //!---------------------------!//



             //========Calculate priors for variables in the network===========//
             List<List<double[]>> carsPriorFactors;
             try { carsPriorFactors = this.calculatePriorFactors("Cars", X); }
             catch (Exception e)
             {
                 Logger.Instance.log_error("Error in calculating priors for cars " + e.ToString());
                 throw (e);
             }
             Logger.Instance.log("Calculated correctly priors for cars");




            InferenceEngine engine = new InferenceEngine(new GibbsSampling());


            //========Setup priors for variables in the network===========//
            //=== returns double[] of predictions == //
            int outer_time_slice = 0; //as function
            for (int i = 1; i < dbnCars.Item2.Length; ++i)
            {
                Console.WriteLine("assigned " + i);

               // dbnCars.Item1[i].ObservedValue = 1;
               // dbnCars.Item2[i] = Variable.D

                dbnCars.Item3[i].ObservedValue = Vector.FromArray(carsPriorFactors[i][outer_time_slice]);
                //dbnCars.Item2[i].InitialiseT
                //     Distribution<double>.Array(Variable<IDistribution<int>>(carsPriorFactors[i][outer_time_slice]))
                // );
            }
            Logger.Instance.log("Setup Y_next for time_slice 0 and Cars correctly");
            Console.WriteLine(carsPriorFactors[1][0].Length);


            engine.NumberOfIterations = 10000;
            Discrete y_next_test1 = engine.Infer<Discrete>(dbnCars.Item2[1]);
            Console.WriteLine(y_next_test1.ToString());

            double max = 0.0;
            int max_id = 0;


            for (int i = 0; i < getIntOpt("Cars", "states_count"); ++i)
            {
                if (y_next_test1[i] > max)
                {
                    max = y_next_test1[i];
                    max_id = i;
                }
            }
           

            Logger.Instance.log_array(carsPriorFactors[1][0]);
            Console.WriteLine((double)max_id * getDblOpt("Cars", "step") * (1.0/normalizeFactors[1][0]));

            
        }


        private double getDblOpt(string subnetwork_name, string option)
        {
            double res;
            try { res = (double)modelOptions["smr" + subnetwork_name + "_" + option]; }
            catch (Exception e)
            {
                try { res = (double)modelOptions[subnetwork_name + "_" + option]; }
                catch (Exception r)
                {
                    Logger.Instance.log_error("not existing option for " + subnetwork_name + " " + option);
                    throw r; //system will crash anyway
                }
            }
            return res;

        }
        private int getIntOpt(string subnetwork_name, string option)
        {
            int res;
            try { res = (int)((double)modelOptions["smr" + subnetwork_name + "_" + option]); }
            catch (Exception e)
            {
                try { res = (int)((double)modelOptions[subnetwork_name + "_" + option]); }
                catch (Exception r)
                {
                    Logger.Instance.log_error("not existing option for " + subnetwork_name + " " + option);
                    throw r; //system will crash anyway
                }
            }
            return res;

        }
        public List<List<double[]>> calculatePriorFactors(string subnetwork_name, double[] X)
        {
            Logger.Instance.log("Run prediction");    //X - conformant do model_options[subnetwork_name_link_count] and model_options[subnetwork_name_predict_behind] and
            //model_options[subnetwork_name_ahead]. 1D array
                //i.e (predict_behind * link_count)+(ahead*link_count) size
            //Format priorFactors[i][j][k] --- priorFactor of i'th link of j'th predicition (+1, +2, +3) of k'th probability discrete (0,1,....,subnetwork_name_states_count)

            Logger.Instance.log_assert(X != null, "null historic_data");
            Logger.Instance.log_ifn((X).GetLength(0) == getIntOpt(subnetwork_name, "link_count") * (getIntOpt(subnetwork_name, "look_behind")), "wrong historic_data[i] size");

            
            //TODO: add using brackets//
            List<List<double[]>> priorFactors = new List<List<double[]>>();

            double[] prediction = subnetwork_name == "Cars" ? smrCars.predict(X) : smrVelocity.predict(X);
            Logger.Instance.log_array(prediction);
            Logger.Instance.log_array(X);
            double step_tmp = getDblOpt(subnetwork_name, "step");
            double st_count_tmp = getDblOpt(subnetwork_name, "states_count");
            double max_tmp = getDblOpt(subnetwork_name, "max");
            int ahead_tmp = getIntOpt(subnetwork_name, "ahead");

            //=====Iterate over every link that is measured in the network=========//
            priorFactors.Add(new List<double[]>()); //dummy
            for (int i = 1; i<= getIntOpt(subnetwork_name,"link_count"); ++i)
            {
                Console.WriteLine(i.ToString());
                priorFactors.Add( new List<double[]>() );

                //=====Iterate over ahead predictions (+5 min +10min etc, depends on the setup)========//
                for (int j = 0; j <ahead_tmp; ++j)
                {
                    priorFactors[i].Add( new double[getIntOpt(subnetwork_name, "states_count")] );
                    //==========Iterate over particular priors=============//

                    double sum = 0.0;
                    for (int k = 0; k < st_count_tmp; ++k)
                    {
                        //Console.WriteLine(String.Format("{0} {1} {2} prediction = {3} , current = {4}", i, j, k, prediction[j], getIntOpt(subnetwork_name, "step") * k));
                        //TODO: Pick constants and write as options
                        //TODO: Standarize indexing (normalizeFactors starts from 0)
                        priorFactors[i][j][k] = 3*Math.Exp(-Math.Abs((Math.Min( this.normalizeFactors[1][i-1]*prediction[(i-1) * ahead_tmp + j + 1], max_tmp) - step_tmp * k)));
                        sum += priorFactors[i][j][k];
                    }
                    //normalization
                    for (int k = 0; k < st_count_tmp; ++k)
                    {
                        //Console.WriteLine(String.Format("{0} {1} {2} prediction = {3} , current = {4}", i, j, k, prediction[j], getIntOpt(subnetwork_name, "step") * k));
                        priorFactors[i][j][k] /= sum;
                    }

                }


            }
            return priorFactors;
        }

     
        
    }
    
 /*  [Serializable]
   public  class MyNode
    {
        int m_id;
        public MyNode() { }
        public MyNode(int id) { m_id = id; }

        [System.Xml.Serialization.XmlAttribute("v_latitude")]
        public string v_latitude { get; set; }

        [System.Xml.Serialization.XmlAttribute("v_longitude")]
        public string v_longitude { get; set; }

        [System.Xml.Serialization.XmlAttribute("v_openid")]
        public string v_openid { get; set; }
    }*/


   
    class Program
    {
        static void Main(string[] args)
        {


          /*  var g = new AdjacencyGraph<MyNode , Edge<MyNode>>();
            using (var xreader = XmlReader.Create("tmp.xml"))
            {
                g.DeserializeFromGraphML(xreader,
                    id => new MyNode(int.Parse(id.Substring(1))),
                    (source, target, id)  => new Edge<MyNode>(source,target)
               );
           }*/

          //  Logger.Instance.log("Graph loaded successfully from .xml");



            //========setup_network()========//

            //model options//

            DataSystem ds = new DataSystem();
            TrafficPredictionModel1 tpm1 = new TrafficPredictionModel1(ds);
            tpm1.init();

            //REMOVE THIS LINE//
            tpm1.smrCars.isTrained = true;
            tpm1.smrVelocity.isTrained = true;
            tpm1.makePrediction();
      
            
          
           

           

    /*
           Variable<int> tmp = Variable.Discrete(new double[] { 0.4, 0.4, 0.09, 0.01 });

           Variable<int> tmp2 = Variable.Discrete(new double[] { 0.05, 0.05, 0.1, 0.8 });

            


           Variable<int> Y1 = Variable.Discrete(new double[]{0.5,0.5});
           Variable<int> Y2 = Variable.Discrete(new double[]{0.5,0.5});
           Variable<int> Y3 = Variable.Discrete(new double[] {0.5,0.5 });


           
           Variable.ConstrainEqualRandom((Y1 == 0) & (Y2 == 0), Bernoulli.FromLogOdds(Math.Log(5.0)));

           Variable.ConstrainEqualRandom((Y1 == 0) & (Y2 == 1), Bernoulli.FromLogOdds(Math.Log(3.0)));

          
           Variable.ConstrainEqualRandom((Y1 == 1) & (Y2 == 0), Bernoulli.FromLogOdds(Math.Log(1.0)));

    */
/*
            InferenceEngine engine = new InferenceEngine(new GibbsSampling());
            engine.NumberOfIterations = 10000;
            Discrete y_next_test1 = engine.Infer<Discrete>(Y_next[1]);
            Discrete y_next_test2 = engine.Infer<Discrete>(Y_next[2]);
            Discrete y_next_test3 = engine.Infer<Discrete>(Y_next[3]);
            Console.WriteLine(y_next_test1.ToString());
            Console.WriteLine(y_next_test2.ToString());
            Console.WriteLine(y_next_test3.ToString());
            //R-function zwraca correlated indexes*/

           // for (int i = 0; i < link_count_var; ++i) for (int j = i + 1; j < link_count_var; ++j)
            //    {

              //      y_agree[i, j] = Variable.Discrete();

             //   }
            



            
            
            try {
                //tpm1.init();
                //tpm1.train(ds.prepare_train_cars(), ds.prepare_train_velocity());
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
