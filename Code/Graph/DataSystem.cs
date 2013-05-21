using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.IO;
using System.Xml.Schema;


namespace PredictionServer
{
    class DataSystem
    {
        // for demonstration version, we are reading the observed data from .csv files and formatting it for TPM1 //
        public int time_start = 0;
        public string get_simulation_data_file()
        {
            return "data/traffic_test.txt";
        }
        public double[] get_current_X_TPM1(int current_time, int look_behind = 30, int link_count = 20)
        {

            double[][] X_transposed = this.readMatrixCsv(this.get_simulation_data_file(), false, look_behind, link_count, " ",current_time);
            double[] X = new double[X_transposed.Length * X_transposed[0].Length];
            int cnt = 0;
            for (int i = 0; i < X_transposed[0].Length; ++i)
            {
                for (int j = 0; j < X_transposed.Length; ++j)
                {
                    X[cnt++] = X_transposed[j][i];
                }
            }
            return X;
        }

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
            //can be easily calculated using function from general.r file//
            return "data/model0b.smrCars.observationalSample.txt";
        }
        public int get_train_pairwise_cars_lines()
        {
            //constant int he demonstration version
            return 1000;
        }
        public int get_train_pairwise_velocity()
        {
            //constant in the demonstartion version
            return 1000;
        }
        public string prepare_train_pairwise_velocity()
        {
            return "data/model0b.data.RDa";
        }

        public double[][] readMatrixCsv(string file_name, bool header, int nrow, int ncol, string sep = " ", int skip = 0)
        {
            double[][] matrix = new double[nrow][]; for (int i = 0; i < nrow; ++i) matrix[i] = new double[ncol];

            var reader = new StreamReader(File.OpenRead(file_name));
            if (header) reader.ReadLine();
            int row = 0, col = 0;
            int skipped = 0;
            while (!reader.EndOfStream)
            {


                //TODO: replace with smarter code
                ++skipped;
                if (skipped <= skip) continue;

               
                var line = reader.ReadLine();
                var values = line.Split(sep.ToCharArray());

                if (line == "") continue; //skip blanks

                try
                {
                    col = 0;
                    foreach (string val in values)
                    {
                        matrix[row][col++] = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    Logger.Instance.log_ifn(ncol == col, "wrong column number specified!"); //warning message
                    ++row;
                }
                catch (Exception e)
                {
                    //skip this line
                    Logger.Instance.log_warning(e.ToString());
                }

                if (row == nrow) break;
            }
            return matrix;
        }

    }
}
