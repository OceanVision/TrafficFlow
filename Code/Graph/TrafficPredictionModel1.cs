using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDotNet;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;


namespace PredictionServer
{
    class TrafficPredictionModel1
    {

        InferenceEngine engine;
        //calculated basing on the model options
        Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[]> dbnCars;
        //Tuple<Variable<double>[], Variable<double>[], Variable<double>[]> dbnCarsCont;
        public int[] states_cars, states_velocity;


        public SingularModelR smrCars; //modelling cars
        public SingularModelR smrVelocity; //modelling velocity
        public PairwiseModelR pmrCars;
        public PairwiseModelR pmrVelocity;

        public double[][] normalizeFactors; //loads after training models to the memory (init phase and after every train phase)

        //loaded or set during the construction
        //Object[] abstract_edges_mapping; //mapping of the abstract edges onto the AbstractGraph

        public Dictionary<string, Object> modelOptions = new Dictionary<string, Object>();

        private DataSystem ds;




        public TrafficPredictionModel1(DataSystem ds)
        {
            this.ds = ds;

            //will be calculated in the init() phase//
            modelOptions["smrVelocity_states_count"] = -1.0;
            modelOptions["smrCars_states_count"] = -1.0;

            modelOptions["smrVelocity_step"] = 0.1;
            modelOptions["smrVelocity_max"] = 1.0;

            modelOptions["smrCars_step"] = 0.1;
            modelOptions["smrCars_max"] = 1.0;

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

            smrCars = new SingularModelR("model0b.r", "model0b", "smrCars");

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

            Logger.Instance.log(" ==== SingularModels init completed ==== ");

            int states_count_cars = (int)(getDblOpt("Cars", "max") / (float)(getDblOpt("Cars", "step"))) + 1;

            states_cars = new int[states_count_cars];
            for (int i = 0; i < states_count_cars; ++i)
                states_cars[i] = (getIntOpt("Cars", "step")) * i;

            modelOptions["smrCars_states_count"] = (double)states_count_cars;


            int states_count_velocity = (int)(getDblOpt("Velocity", "max") / (float)(getDblOpt("Velocity", "step"))) + 1;

            states_velocity = new int[states_count_cars];
            for (int i = 0; i < states_count_cars; ++i)
                states_velocity[i] = (getIntOpt("Velocity", "step")) * i;

            modelOptions["smrVelocity_states_count"] = (double)states_count_velocity;

           
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


        public Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[]> setupSubnetwork(string subnetwork_name)
        {
            // =======Prepare 2BN Slice of the subnetwork)================ //
            Variable<int>[] Y_before = new Variable<int>[getIntOpt(subnetwork_name, "link_count") + 1];
            Variable<int>[] Y_next = new Variable<int>[getIntOpt(subnetwork_name, "link_count") + 1];
            Variable<Vector>[] Y_next_prior = new Variable<Vector>[getIntOpt(subnetwork_name, "link_count") + 1];

            try
            {
                for (int i = 1; i <= getIntOpt(subnetwork_name, "link_count"); ++i)
                {
                    Y_before[i] = Variable.New<int>();
                    //will be observed
                    Y_next_prior[i] = Variable.New<Vector>();
                    Y_next[i] = Variable.Discrete(Y_next_prior[i]);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.log("Error initializing Y_before and Y_next variables for the sub_network " + subnetwork_name);
                Logger.Instance.log_warning(e.ToString());
            }
            Logger.Instance.log(" ==== Correctly initialized ariables for the sub_network " + subnetwork_name + " ==== ");


            // =======Initialize pairwise calculation (probably load into the memory a data sample)================ //
            if (subnetwork_name == "Cars")
                pmrCars.initPairwiseCalculation(ds.prepare_train_pairwise_cars(), getIntOpt(subnetwork_name, "link_count"), ds.get_train_pairwise_cars_lines(), getDblOpt(subnetwork_name, "step"));
            else if (subnetwork_name == "Velocity")
                pmrVelocity.initPairwiseCalculation(ds.prepare_train_pairwise_velocity(), getIntOpt(subnetwork_name, "link_count"), ds.get_train_pairwise_velocity(), getDblOpt(subnetwork_name, "step"));

            else
                throw (new Exception("Not supported subnetwork name"));


            double[] lista = pmrCars.getPairwiseFactorList(getIntOpt(subnetwork_name,"link_count"), getDblOpt(subnetwork_name, "step"));
            Logger.Instance.log_array(lista);

            //==========Calculate pairwise factors and represent in Infer.NET structure =====================//
            int state_count = getIntOpt(subnetwork_name, "states_count");
            int iter = getIntOpt(subnetwork_name, "states_count") * getIntOpt(subnetwork_name, "states_count");
            try
            {
                int i, j;
                for (int a = 1; a <= (int)(lista.Length/2); ++a)
                {
                        i = (int) lista[(a - 1) * 2];
                        j = (int) lista[(a - 1) * 2 + 1];

                        if (i >= j) continue;

                        double[] factor = new double[1000];
 
                        double sum = 0.0, max = 0.0;
                        for (int k = 1; k < factor.Length; ++k)
                        {
                            sum += factor[k];
                            max = Math.Max(max, factor[k]);
                        }
              
                        for (int k = 0; k < iter; ++k)
                        {
                            int state_a = (int)(k / (float)(state_count));
                            int state_b = k - (state_count) * (int)(k / (float)(state_count));

                            double prob_odds = Math.Max(0.5, 0.5 * (1.0 - factor[0]) + factor[0] * Math.Exp(-Math.Abs(state_a - state_b) / 3.0));

                            if (state_a > 6 && state_b > 6)
                            {
                             // Variable.ConstrainEqualRandom(
                             //   (Y_next[i] == state_a) &
                             //    (Y_next[((int)j)] == state_b),
                             //    new Bernoulli(prob_odds));
                            }
                        }
 
                }

            }
            catch (Exception e)
            {
                Logger.Instance.log("Error while calculating pairwise factors for " + subnetwork_name);
                throw (e);
            }
            Logger.Instance.log(" ====  Calculated pairwise factors for cars correctly  =====");



            // return results as a tuple //
            return new Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[]>(Y_before, Y_next, Y_next_prior);
        }
        public Tuple<Variable<double>[], Variable<double>[], Variable<double>[]> setupSubnetworkCont(string subnetwork_name)
        {
            // =======Prepare 2BN Slice of the subnetwork)================ //
            Variable<double>[] Y_before = new Variable<double>[getIntOpt(subnetwork_name, "link_count") + 1];
            Variable<double>[] Y_next = new Variable<double>[getIntOpt(subnetwork_name, "link_count") + 1];
            // Variable<IDistribution<int>>[] Y_next_prior = new Variable<IDistribution<int>>[(int)modelOptions["smr" + subnetwork_name + "_link_count"] + 1];

            Variable<double>[] Y_next_prior = new Variable<double>[getIntOpt(subnetwork_name, "link_count") + 1];

            try
            {

                for (int i = 1; i <= getIntOpt(subnetwork_name, "link_count"); ++i)
                {
                    //will be observed
                    Y_before[i] = Variable.New<double>();// Variable<int>.Discrete(); //Variable.Discrete<int>.Named(subnetwork_name + "Y_before" + (i + 1).ToString()); //observed

                    //  Y_next[i] = Variable<int>.Discrete(); ;// Variable<int>().Named(subnetwork_name + "Y_next" + (i + 1).ToString()); //observed

                    //will be observed
                    Y_next_prior[i] = Variable.New<double>();

                    Y_next[i] = Variable.GammaFromMeanAndVariance(Y_next_prior[i], 0.1);
                    //initialize using prior
                    //Y_next[i].InitialiseTo(Y_next_prior[i]);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.log_warning(e.ToString());
                Logger.Instance.log("Error initializing Y_before and Y_next variables for the sub_network " + subnetwork_name);
            }
            Logger.Instance.log(" ==== Correctly initialized variables for the sub_network " + subnetwork_name + "====");



            // =======Initialize pairwise calculation (probably load into the memory a data sample)================ //
            if (subnetwork_name == "Cars")
                pmrCars.initPairwiseCalculation(ds.prepare_train_pairwise_cars(), getIntOpt(subnetwork_name, "link_count"), ds.get_train_pairwise_cars_lines(), getDblOpt(subnetwork_name, "step"));
            else if (subnetwork_name == "Velocity")
                pmrVelocity.initPairwiseCalculation(ds.prepare_train_pairwise_velocity(), getIntOpt(subnetwork_name, "link_count"), ds.get_train_pairwise_velocity(), getDblOpt(subnetwork_name, "step"));

            else
                throw (new Exception("Not supported subnetwork name"));


            //==========Calculate pairwise factors and represent in Infer.NET structure =====================//
            try
            {


                for (int i = 1; i <= getIntOpt(subnetwork_name, "link_count"); ++i)
                {

                    for (int j = i + 1; j <= getIntOpt(subnetwork_name, "link_count"); ++j)
                    {
                        if (i >= j) continue;
                        double[] factor = pmrCars.getPairwiseFactor(i, j, getIntOpt(subnetwork_name, "link_count"), getDblOpt(subnetwork_name, "step"));
                        if (factor[0] < -50) continue; //not picked (TODO: improve skipping condition)
                        Logger.Instance.log(i.ToString() + " : " + j.ToString());
                        double sum = 0.0, max = 0.0;
                        for (int k = 1; k < factor.Length; ++k)
                        {
                            sum += factor[k];
                            max = Math.Max(max, factor[k]);
                        }

                        Variable.ConstrainEqualRandom(

                         Y_next[i] - Y_next[j],

                         new Gaussian(0.0, (1 - factor[0])));


                    }
                }

            }
            catch (Exception e)
            {
                Logger.Instance.log("Error while calculating pairwise factors for " + subnetwork_name);
                throw (e);
            }
            Logger.Instance.log("Calculated pairwise factors for cars correctly");



            // return results as a tuple //
            return new Tuple<Variable<double>[], Variable<double>[], Variable<double>[]>(Y_before, Y_next, Y_next_prior);
        }

        public void compileEngine()
        {
            dbnCars = this.setupSubnetwork("Cars");
            engine = new InferenceEngine(new GibbsSampling());
            engine.NumberOfIterations = 1000;
            engine.ShowProgress = false;
        }

        public double[,] makePrediction(double[] X) //(AbstractGraph to fill)
        {
            //========Calculate priors for variables in the network===========//
            List<List<double[]>> carsPriorFactors;
            try { carsPriorFactors = this.calculatePriorFactors("Cars", X); }
            catch (Exception e)
            {
                Logger.Instance.log_error("Error in calculating priors for cars " + e.ToString());
                throw (e);
            }
            Logger.Instance.log(" ==== Calculated correctly priors for cars ==== ");



            //========Setup priors for variables in the network===========//
            //=== returns double[] of predictions == //
  
            double[,] return_prediction = new double[getIntOpt("Cars", "ahead"), getIntOpt("Cars", "link_count")];
            for(int k=0;k<getIntOpt("Cars","ahead");++k){
                double[] res = _infer_time_slice(k, dbnCars, carsPriorFactors, this.engine);
                //Logger.Instance.log_array(res);
                for(int i=0;i<getIntOpt("Cars","link_count");++i){
                    return_prediction[k,i] = res[i];
                }
            }

            return return_prediction;

        }
        private double[] _infer_time_slice(int outer_time_slice,
            Tuple<Variable<int>[], Variable<int>[], Variable<Vector>[]> dbnCars,
             List<List<double[]>> carsPriorFactors,
            InferenceEngine ie
            )
        {
            for (int i = 1; i < dbnCars.Item2.Length; ++i)
            {
                dbnCars.Item3[i].ObservedValue = Vector.FromArray(carsPriorFactors[i][outer_time_slice]);
            }
          //  engine.OptimiseForVariables = dbnCars.Item2;
            Logger.Instance.log("Setup Y_next for time_slice 0 and Cars correctly");
            Console.WriteLine(carsPriorFactors[1][0].Length);


            double[] result = new double[getIntOpt("Cars", "link_count") + 1];
            int state_count = getIntOpt("Cars", "states_count");
            double step = getDblOpt("Cars", "step");
            for (int h = 1; h <= 20; ++h)// getIntOpt("Cars", "link_count"))
            {
                Discrete y_next_test1 = ie.Infer<Discrete>(dbnCars.Item2[h]);
               
                double max = 0.0;
                int max_id = 0;

                for (int i = 0; i < state_count ; ++i)
                {
                    if (y_next_test1[i] > max)
                    {
                        max = y_next_test1[i];
                        max_id = i;
                    }
                }

                result[h] = ((double)max_id + 0.5) * step * (1.0 / normalizeFactors[1][h - 1]);
               // Console.WriteLine(((double)max_id + 0.5) * getDblOpt("Cars", "step") * (1.0 / normalizeFactors[1][h - 1]));
            }
            return result;
        }


        private double getDblOpt(string subnetwork_name, string option)
        {
            double res;
            try { res = (double)modelOptions["smr" + subnetwork_name + "_" + option]; }
            catch (Exception e)
            {
                Logger.Instance.log_warning(e.ToString());
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
                Logger.Instance.log_warning(e.ToString());
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
            for (int i = 1; i <= getIntOpt(subnetwork_name, "link_count"); ++i)
            {
             //   Console.WriteLine(i.ToString());
                priorFactors.Add(new List<double[]>());

                //=====Iterate over ahead predictions (+5 min +10min etc, depends on the setup)========//
                for (int j = 0; j < ahead_tmp; ++j)
                {
                    priorFactors[i].Add(new double[(int)st_count_tmp]);
                    //==========Iterate over particular priors=============//

                    double sum = 0.0;
                    for (int k = 0; k < st_count_tmp; ++k)
                    {
                        //Console.WriteLine(String.Format("{0} {1} {2} prediction = {3} , current = {4}", i, j, k, prediction[j], getIntOpt(subnetwork_name, "step") * k));
                        //TODO: Pick constants and write as options
                        //TODO: Standarize indexing (normalizeFactors starts from 0)
                        priorFactors[i][j][k] = Math.Exp(-3 * Math.Abs((Math.Min(this.normalizeFactors[1][i - 1] * prediction[(i - 1) * ahead_tmp + j + 1], max_tmp) - step_tmp * ((double)k + 0.5))));
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

   
}
