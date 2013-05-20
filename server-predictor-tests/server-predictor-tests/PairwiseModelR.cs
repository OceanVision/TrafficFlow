using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDotNet;
using System.IO;
using System.Reflection;
using System.Collections;

namespace server_predictor_tests
{
    //Model for calculating pairwise factors. For now is trivial //
    class PairwiseModelR
    {
        public static REngine engine
        {
            get;
            set;
        }//engine to R
        string m_name = "PairwiseModelR";

       public PairwiseModelR(string name)
        {
            m_name = name;
            pairwiseFactors = new ArrayList();
        }

        

        // data_file_name - train
        public void init()
        {
            // parameters - setting up predictor's options - for instance ahead : how many VALUES to predict ahead
            if (PairwiseModelR.engine == null)
            {
                try
                {
                    var envPath = Environment.GetEnvironmentVariable("PATH");
                    var rBinPath = @"c:\Program Files\R\R-2.15.2\bin\i386";
                    Environment.SetEnvironmentVariable("PATH", envPath + Path.PathSeparator + rBinPath);
                    REngine.CreateInstance("RDotNet" + m_name);

                    PairwiseModelR.engine = REngine.GetInstanceFromID("RDotNet" + m_name);
                    PairwiseModelR.engine.Initialize();
                }
                catch (Exception e)
                {
                    Logger.Instance.log("Error initializing R.NET engine");
                    throw (e);
                }
                Logger.Instance.log("Correctly initialized R.NET engine");
            }
            engine.Evaluate("source('general.r')");

        }
        ArrayList pairwiseFactors;
        private string p;
        public void initPairwiseCalculation(string data_file_name, int link_count, int data_sample_file_lines, double step_size)
        {
            //invoke R-script to load train data sample
            try
            {
                engine.Evaluate("general.initPairwiseCalculation(data_sample_file=\"" + data_file_name + "\", link_count = " +
                            link_count.ToString() + ",data_sample_file_lines = " + 
                               data_sample_file_lines.ToString() + ", step_size = " + step_size.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
            }
            catch (Exception e)
            {
                Logger.Instance.log_error(e.ToString());
                throw(e);
            }
        }

        public double[] getPairwiseFactorList(int link_count, double step)
        {
            NumericVector Y;
            try
            {
                Y = engine.Evaluate("general.getDifferencePariwiseFactorList(" +
       
                     link_count.ToString() + "," +
                     step.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")").AsNumeric();
                double[] Y_output = new double[Y.Length];
                Y.AsNumeric().CopyTo(Y_output, Y.Length, 0, 0);
                return Y_output;
            }
            catch (Exception e)
            {
                Logger.Instance.log_error("Error while getting pairwise factor list " + e.ToString());
                throw (e);
            }

        }
        public double[] getPairwiseFactor(int i, int j, int link_count, double step)
        {
            //invoke R-script to calculate pairwise factor for indices (i,j)
            NumericVector Y;
            try
            {
               Y = engine.Evaluate("general.getDifferencePairwiseFactor(" +
                    i.ToString() + "," +
                    j.ToString() + "," + 
                    link_count.ToString() + "," + 
                    step.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")").AsNumeric();
                double[] Y_output = new double[Y.Length];
                Y.AsNumeric().CopyTo(Y_output, Y.Length, 0, 0);
                return Y_output;
            }
            catch (Exception e)
            {
                Logger.Instance.log_warning("Error while getting pairwise factor " + e.ToString());
            }

            int steps = 10;

            double[] factor = new double[steps * steps];
            for (int h = 0; h < steps; ++h) for (int k = 0; k < steps; ++k)
            {
                factor[h * steps + k] = 0.5;
            }
            return factor;
        }

        public void finishPairwiseCalculation()
        {


            //invoke R-script to delete train data sample
            try
            {
            }
            catch (Exception e)
            {
            }

        }

    }
}
