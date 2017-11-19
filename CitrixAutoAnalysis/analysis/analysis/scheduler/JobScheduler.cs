using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern;
using CitrixAutoAnalysis.analysis.io;
using CitrixAutoAnalysis.analysis.engine;

namespace CitrixAutoAnalysis.analysis.scheduler
{
    class JobScheduler
    {
        private static int SLEEP_INTERVAL = 2 * 1000;
        public static void ScheduleJobs()
        { 
            Job job;
            while (true)
            {
                try
                {
                    while ((job = Job.GetTheMostUrgentJob()) != null)
                    {
                        CDFHelper helper = new CDFHelper(job);

                        helper.ProcessJob(job);

                        // don't let the scheduler eats all CPU 
                        System.Threading.Thread.Sleep(SLEEP_INTERVAL);
                    }
                }
                catch (Exception ex)
                { 
                    //do nothing
                }
            }
        }

    }
}
