using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PredictionServer
{

        //implementation of prediction daemon taht will be runned on the server, for demonstration purposes merged with client//
        class PredictionDaemon
        {
            static ReaderWriterLockSlim singleton_lock = new ReaderWriterLockSlim();
            ReaderWriterLockSlim current_prediction_lock = new ReaderWriterLockSlim(); //read write lock for reading current prediction
            DataSystem ds; //data system reference - for demonstration purposes simplifed
            TrafficPredictionModel1 tpm1; //traffic prediction object
            private double[,] current_prediction;
            private static PredictionDaemon m_instance;
            private PredictionDaemon() { ds = new DataSystem(); tpm1 = new TrafficPredictionModel1(ds); }
            private volatile int last_stamp_prediction_backing = -1;
            private int last_stamp_prediction { get { return last_stamp_prediction_backing; } set { last_stamp_prediction_backing = value; } }


            //=== public methods ====//
            public Thread getDaemon()
            {
                return new Thread(new ThreadStart(run));
            }

            public static PredictionDaemon Instance
            {
                get
                {
                    singleton_lock.EnterWriteLock();
                    if (m_instance == null)
                    {
                        m_instance = new PredictionDaemon();
                    }
                    singleton_lock.ExitWriteLock();
                    return m_instance;
                }
            }

            //for demonstration purposes merged with client//
            public void makePrediction()
            {
                current_prediction_lock.EnterWriteLock();
                Logger.Instance.log(" ==== Making prediction for "+ ServerCore.Instance.current_time_stamp.ToString() +" ==== ");
                current_prediction = this.tpm1.makePrediction(ds.get_current_X_TPM1(ServerCore.Instance.current_time_stamp));
                current_prediction_lock.ExitWriteLock();
            }

            /// <summary>
            /// Used by ServerCore , waits for the first prediction
            /// </summary>
            /// <returns> Matrix of prediction </returns>
            public double[,]  fetchPrediction()
            {
                
                while (this.last_stamp_prediction == -1) Thread.Sleep(2000); //wait for the first predicition
                current_prediction_lock.EnterReadLock();
                double [,] prediction_clone = (double[,])this.current_prediction.Clone();
                current_prediction_lock.ExitReadLock();
                return prediction_clone;
            }

            public void run()
            {
                Logger.Instance.log(" ==== Executed prediction service ==== ");
                //=====Initialize prediction systems====//
                try
                {
                    tpm1.init(); //for demonstration purposes
                }
                catch (Exception e)
                {
                    Logger.Instance.log_error("Failed to init prediction submodule: " + e.ToString());
                    throw (e);
                }
                ServerCore.Instance.progress += 3;
                Logger.Instance.log("==== Initiliazed predcition systems successfully =====");
                tpm1.smrCars.isTrained = true;
                tpm1.smrVelocity.isTrained = true;


                Logger.Instance.log("===== Compiling inference engine =======");
                try
                {
                    tpm1.compileEngine(); //compile Infer.NET engine
                }
                catch (Exception e)
                {
                    Logger.Instance.log_error("Failed to compile prediction infering algorithm: " + e.ToString());
                    throw (e);
                }
                ServerCore.Instance.progress += 3;
                Logger.Instance.log("===== Compiled infering engine successfully =======");

               
                while (true)
                {
                    // Check if the current time stamp has changed //
            
                    if (ServerCore.Instance.current_time_stamp != last_stamp_prediction)
                    {
   
                        makePrediction();  //make prediction every 1 min
                       
                        if (last_stamp_prediction == -1)
                        {
                            ServerCore.Instance.progress += 14;
                            Logger.Instance.log(" ==== First prediction and compilation was successful ==== ");
                        }
                        last_stamp_prediction = ServerCore.Instance.current_time_stamp;
                    }
                    Thread.Sleep(1000);
                }
            }

  
    }
}
