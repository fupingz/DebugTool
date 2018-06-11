using CitrixAutoAnalysis.analysis.engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAutoAnalysis.analysis.analyzers
{
    //this analyzer tries to check all the trace lines which potentially reports an "ERROR" or "Exception", so as to locate the root cause
    class ErrorAndExceptionAnalyzer
    {
        private CDFHelper _helper;

        public ErrorAndExceptionAnalyzer(CDFHelper helper)
        {
            this._helper = helper;
        }



    }
}
