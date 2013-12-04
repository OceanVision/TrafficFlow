using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
namespace PredictionServer
{
    class ServerConfiguration{
        public int
            simulation_start_minute { get; set; }
        public string Rpath { get; set; }
        public int max_progress{ get; set; }
    }

    ///<summary>
    /// Class biding all submodules belonging to the server
    ///</summary>
    class ServerCore
    {
        public int progress { get; set; }

        private Stopwatch stopWatch;
        private static ServerCore m_instance;
        private static readonly ReaderWriterLockSlim singleton_lock = new ReaderWriterLockSlim();
        private static readonly ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();
        private double elapsed_minutes_start_backing;
        /// <summary> 
        /// current time_stamp  </summary>
        public double elapsed_minutes_start { 
             get{ slimLock.EnterReadLock(); var x = elapsed_minutes_start_backing; slimLock.ExitReadLock(); return x;} 
             set{ slimLock.EnterWriteLock(); elapsed_minutes_start_backing=value; slimLock.ExitWriteLock();}  
        }
        public int current_time_stamp { get { return (int)(elapsed_minutes_start + serverConfiguration.simulation_start_minute); } }
        public double[,] current_prediction { get { return PredictionDaemon.Instance.fetchPrediction(); } }

        /// <summary>
        /// Wrapper for current_prediction (for server)
        /// </summary>
        /// <returns> Matrix of prediction (using ordering of links specified in matching file) </returns>
        public double[,] fetchPrediction() { return this.current_prediction; }
        /// <summary>
        /// Wrapper current_time_stamp (for server)
        /// </summary>
        /// <returns> Int time stamp (minute) </returns>
        public int fetchCurrentTimeStamp() { return this.current_time_stamp; }
        private ServerCore()
        {
            this.progress = 0;
            
            this.elapsed_minutes_start = 0.0;
            this.stopWatch = new Stopwatch();
            this.stopWatch.Start();
        }

        public ServerConfiguration serverConfiguration { get; set; }
        
        public static ServerCore Instance
        {
            get
            {
                ServerCore.singleton_lock.EnterWriteLock();
                if (m_instance == null)
                {
                    m_instance = new ServerCore();
                }
                ServerCore.singleton_lock.ExitWriteLock();
                return m_instance;
            }
        }

        private void runServerDaemons()
        {
            PredictionDaemon.Instance.getDaemon().Start();
        }

        private void run()
        {
            //configure//
            this.serverConfiguration.max_progress = 20;

            this.runServerDaemons();
            while (true)
            {
                Thread.Sleep(1000);
                this.elapsed_minutes_start = (this.stopWatch.Elapsed.Minutes + this.stopWatch.Elapsed.Seconds / 60.0); //update current time_stamp
            }
        }

        public Thread getDaemon()
        {
            return new Thread(new ThreadStart(run));
        }

    }
}
