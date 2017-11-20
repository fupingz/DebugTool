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
        private static int LONG_SLEEP_INTERVAL = 15 * 2 * 1000;
        public static void ScheduleJobs()
        { 
            Job job;
            while (true)
            {

                while ((job = Job.GetTheMostUrgentJob()) != null)
                {
                    Console.WriteLine("obtained job(id ="+job.JobId+") to process");
                    try
                    {
                        job.UpdateJobStartInfo();
                        CDFHelper helper = new CDFHelper(job);

                        helper.ProcessJob(job);

                        job.UpdateJobFinishInfo();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Job(id=" + job.JobId + ") : exception happened."+ex.ToString());
                        job.UpdateJobFailedInfo();
                        //do nothing
                    }

                    // don't let the scheduler eats all CPU 
                    System.Threading.Thread.Sleep(SLEEP_INTERVAL);
                }
   
                Console.WriteLine("No jobs to process, so sleep in the next 30 secs");
                System.Threading.Thread.Sleep(LONG_SLEEP_INTERVAL);
            }
        }

    }
}
