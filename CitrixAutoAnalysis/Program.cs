using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern.generator;
using CitrixAutoAnalysis.pattern;
using CitrixAutoAnalysis.analysis.scheduler;
namespace CitrixAutoAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            //construct the xml file
            //new XMLGenerator().GenerateXML("c:\\CDF.txt", "C:\\cdfxml.xml");
            DataBaseHelper.DataBaseHelper2.Instance.StartTimerThread();
            //read pattern from xml file
            //CitrixAutoAnalysis.Pattern.Pattern.FromXml("C:\\cdfxml.xml");
            JobScheduler.ScheduleJobs();
        }
    }
}
