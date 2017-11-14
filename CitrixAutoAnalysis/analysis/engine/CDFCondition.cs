using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAutoAnalysis.analysis.engine
{
    enum CDFCondition
    {
        CDF_MODULE,
        CDF_SRC,
        CDF_FUNC,
        CDF_LINE,
        CDF_SESSIONID,
        CDF_PROCESSID,
        CDF_THREADID,
        CDF_TEXT,
        CDF_FILTER// to use the contexttype.contextfilter to filter out logs
    }
}
