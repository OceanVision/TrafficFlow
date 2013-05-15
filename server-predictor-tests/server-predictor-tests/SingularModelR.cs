using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDotNet;
using System.Collections;
using System.IO;

namespace server_predictor_tests
{

    //Pariwise factor predictor only based on local history used in Bayes Network
    //Stays the same regardless of the underlying model assuming it satisfies given interface
    class SingularModelR
    {
        static REngine engine; //engine to R
        private string m_trained_model_file; //saved trained model
        private string m_source_file;
        private string m_alg_name;
        private int m_ahead;
        private string m_predict_var_name;
        private string m_name;

        //init - initialize engine and setup predict_options
        public SingularModelR(string source_file = "model0b.r", string alg_name = "model0b", string name = "SingularModelR")
        {
            m_name = name;
            m_source_file = source_file; m_alg_name = alg_name;
            m_trained_model_file = "data/" + m_name + ".trained_model.RDa"; //setting up used file paths
            m_predict_var_name = "predict_options_" + m_name;
        }
        //transfer part of construction to the init function (can be time consuming)
        public void init(int look_behind = 30, int link_count = 20, int look_behind_prediction_self = 30, int look_behind_prediction = 10, int ahead = 10)
        {
            m_ahead = ahead;

            if (SingularModelR.engine == null)
            {
                try
                {
                    var envPath = Environment.GetEnvironmentVariable("PATH");
                    var rBinPath = @"c:\Program Files\R\R-2.15.2\bin\i386";
                    Environment.SetEnvironmentVariable("PATH", envPath + Path.PathSeparator + rBinPath);
                    REngine.CreateInstance("RDotNet" + m_name);

                    SingularModelR.engine = REngine.GetInstanceFromID("RDotNet" + m_name);
                    SingularModelR.engine.Initialize();
                }
                catch (Exception e)
                {
                    Logger.Instance.log("Error initializing R.NET engine");
                    throw (e);
                }
            }
            Logger.Instance.log("Correctly initialized R.NET engine");
            
            try
            {
    
                string cur_dir = Directory.GetCurrentDirectory();

                SingularModelR.engine.Evaluate("setwd(" + cur_dir + ") ");
     
                //load scripts into the memory
                SingularModelR.engine.Evaluate("source(\"" + m_source_file + "\")");
               
                Logger.Instance.log("Source code loaded correctly");

                SingularModelR.engine.Evaluate(m_predict_var_name + "= " + m_alg_name + ".getDefaultPredictOptions()");

                SingularModelR.engine.Evaluate(m_predict_var_name+"= " + look_behind.ToString());
                SingularModelR.engine.Evaluate(m_predict_var_name+"$link_count= " + link_count.ToString());
                SingularModelR.engine.Evaluate(m_predict_var_name + "$look_behind_prediction= " + look_behind_prediction.ToString());
                SingularModelR.engine.Evaluate(m_predict_var_name + "$look_behind_prediction_self= " + look_behind_prediction_self.ToString());
                SingularModelR.engine.Evaluate(m_predict_var_name + "$ahead= " + ahead.ToString());
                Logger.Instance.log("predict_options set correctly");
          
            }
            catch (Exception e)
            {
                Logger.Instance.log("Error during the initialization of the model");
                throw (e);
            }
            

        }
 
        //TODO: make it accept raw .RDa
        public void train_model(string data_file) //data assumed to be an matrix X..Y
        {
            try
            {
                SingularModelR.engine.Evaluate(m_alg_name + ".train('" + data_file + "',"+m_predict_var_name+", '" + m_trained_model_file + "')");
            }
            catch (Exception e)
            {
                Logger.Instance.log("Error in training SingularModelR");
                Logger.Instance.log(e.ToString());
                return;
            }
            Logger.Instance.log("Training SingularModelR was successful");
            SingularModelR.engine.Evaluate("load('" + m_trained_model_file + "')");
        }
        public double[] predict(double[] X_input)
        {
            SingularModelR.engine.Evaluate("load('" + m_trained_model_file + "')");
            var x = engine.CreateNumericVector(X_input);
            engine.SetSymbol("x", x);
            NumericVector Y;
            try
            {
                Y = engine.Evaluate(m_alg_name + ".makePrediction(" + m_alg_name + ".model, x, "+m_predict_var_name+")").AsNumeric();

            }
            catch (Exception e)
            {
                Logger.Instance.log("Error while predicting " + e.ToString());
                throw (e);
            }
            double[] Y_output = new double[this.m_ahead];
            Y.AsNumeric().CopyTo(Y_output, this.m_ahead);
            //Console.WriteLine(Y.First().ToString());
            return Y_output;

        }
        public void use_case1()
        {
            SingularModelR smr = new SingularModelR();

            smr.init();
            smr.train_model("data/model0b.data.RDa");
            double[] X = { 1, 2, 20, 20, 30, 1, 8, 10, 19, 29, 1, 2, 20, 20, 30, 1, 8, 10, 19, 29,
                              1, 2, 20, 20, 30, 1, 8, 10, 19, 29, 1, 2, 20, 20, 30, 1, 8, 10, 19, 29,
                              1, 2, 20, 20, 30, 1, 8, 10, 19, 29, 1, 2, 20, 20, 30, 1, 8, 10, 19, 29,
                              1, 2, 20, 20, 30, 1, 8, 10, 19, 29, 1, 2, 20, 20, 30, 1, 8, 10, 19, 29,
                              1, 2, 20, 20, 30, 1, 8, 10, 19, 29, 1, 2, 20, 20, 30, 1, 8, 10, 19, 29};
            double[] Y = smr.predict(X);
            foreach (var v in Y) Console.Write(v);
        }

    }
}
