using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAutoAnalysis.analysis.engine
{
    class NoAppropriateTraecFoundException : Exception
    {
        private string message;
        public NoAppropriateTraecFoundException(string msg)
        {
            message = msg;
        }

        public string Message
        {
            get { return message; }
        }
    }
}
