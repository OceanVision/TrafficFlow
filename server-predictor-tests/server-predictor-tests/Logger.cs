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

        public void log(string message)
        {
            StackFrame callstack = new StackFrame(1, true);
            string logMessage = string.Format("{0} {1}:{2}\t{3}", DateTime.Now.ToString() , callstack.GetFileLineNumber(), callstack.GetMethod(), message);
            Console.WriteLine(logMessage);        
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
