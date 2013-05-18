using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace server_predictor_tests
{
    //Prosta klasa do obslugi logow - do rozwiniecia//
    class Logger
    {
        private static Logger instance;

        private Logger() {}

        //TODO: fill in dump function and add delegate (or stream?)
        private void dump(string message)
        {

        }

        public void log(string message, bool new_line = true)
        {
            StackFrame callstack = new StackFrame(1, true);
            string logMessage = string.Format("{0} {1}:{2}\t{3}", DateTime.Now.ToString() , callstack.GetFileLineNumber(), callstack.GetMethod(), message);
            if (new_line) Console.WriteLine(logMessage);
            else Console.Write(logMessage);
        }

        public void log_warning(string message)
        {
            this.log("WARNING:" + message);
        }
        public void log_array<T>(T[] array){
            this.log("(", false);
            foreach (var t in array) Console.Write(t + ",");
            Console.Write(")\n");
        }

        public void log_error(string message)
        {
            this.log("ERROR:" + message);
        }
        public void log_ifn(bool condition, string message)
        {
           if(!condition) this.log_warning("WARNING:"+message);
        }
        public void log_assert(bool condition, string message)
        {

            if (!condition)
            {
                this.log_error(message);
                throw new Exception(message);
            }
        }
        public static Logger Instance{
            get
            {
                if (instance == null)
                {
                    instance = new Logger();
                }
                return instance;
            }
        }
    }
}
