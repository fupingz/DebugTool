using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern.generator;
using CitrixAutoAnalysis.pattern;
using CitrixAutoAnalysis.analysis.scheduler;

using System.Threading;

namespace CitrixAutoAnalysis
{
    class Program
    {
        public static void Start()
        {
            try
            {
                Thread resultAnalyzerThread = new Thread(new ThreadStart(ResultAnalzerThread));
                resultAnalyzerThread.Start();

                Thread traceAnalyzerThread = new Thread(new ThreadStart(TraceAnalyzerThread));
                traceAnalyzerThread.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
 
        }

        static private void ResultAnalzerThread()
        {
            //construct the xml file
            //new XMLGenerator().GenerateXML("c:\\CDF.txt", "C:\\cdfxml.xml");
            DataBaseHelper.DataBaseHelper2.Instance.DBOpen();
            DataBaseHelper.DataBaseHelper2.Instance.StartTimerThread();
        }

        static private void TraceAnalyzerThread()
        {
            //read pattern from xml file
            //CitrixAutoAnalysis.Pattern.Pattern.FromXml("C:\\cdfxml.xml");
            JobScheduler.ScheduleJobs();
        }
    }
}
