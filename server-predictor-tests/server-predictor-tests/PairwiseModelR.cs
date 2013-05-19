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
            }
            Logger.Instance.log("Correctly initialized R.NET engine");
        }
        ArrayList pairwiseFactors;
        private string p;
        public void initPairwiseCalculation(string data_file_name)
        {
            //invoke R-script to load train data sample
            try
            {
            }
            catch (Exception e)
            {
            }
            
   
        }


        public double[] getPairwiseFactor(int i, int j, int step, int steps)
        {
            //invoke R-script to calculate pairwise factor for indices (i,j)
            try
            {
            }
            catch (Exception e)
            {
            }

            //TODO: fill
            double[] factor = new double[steps * steps];
            for (int h = 0; h < steps; ++h) for (int k = 0; k < steps; ++k)
            {
                factor[h * steps + k] = 2.0;///(double)(steps*steps);
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
