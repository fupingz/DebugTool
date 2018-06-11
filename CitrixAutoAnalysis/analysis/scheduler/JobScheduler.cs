using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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
        private static bool stopService = false;

        public static void ScheduleJobs()
        { 
            Job job;
            while (true)
            {

                while ((job = Job.GetTheMostUrgentJob()) != null)
                {
                    System.Diagnostics.Trace.WriteLine("obtained job(id ="+job.JobId+") to process");
                    System.Console.WriteLine("obtained job(id ="+job.JobId+") to process");
                    try
                    {
                        job.UpdateJobStartInfo();

                        List<Pattern> patterns = FindPatternsPerJobID(job.JobId);

                        if (patterns.Count == 0)
                        {
                            patterns.Add(Pattern.FromXml(@"C:\CAD\Patterns\VDA Connection.xml") as Pattern);
                        }

                        foreach (Pattern pattern in patterns)
                        {
                            CDFHelper helper = new CDFHelper(job, pattern);
                            helper.ProcessJob();
                        }

                        job.UpdateJobFinishInfo();

                    }
                    catch (NoAppropriateTraecFoundException nate)
                    {
                        job.UpdateJobFailedInfo(nate.Message);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("Job(id=" + job.JobId + ") : exception happened." + ex.ToString());
                        job.UpdateJobFailedInfo("Unexpected Exception");
                        //do nothing
                    }

                    if (stopService)// so service can be stopped
                    {
                        return;
                    }

                    // don't let the scheduler eats all CPU 
                    System.Threading.Thread.Sleep(SLEEP_INTERVAL);
                }

                System.Diagnostics.Trace.WriteLine("No jobs to process, so sleep in the next 30 secs");
                System.Threading.Thread.Sleep(LONG_SLEEP_INTERVAL);
            }
        }

        public static void StopService()
        {
            stopService = true;
        }

        private static List<Pattern> FindPatternsPerJobID(uint jobId)
        {
            List<Pattern> resultPatterns = new List<Pattern>();

            DataTable dt;
            string sql = "select P.filePath from JobPattern j inner join PatternTable P on j.PatternId=p.ID where j.JobId = " + jobId;

            using (DBHelper helper = new DBHelper())
            {
                dt = helper.FillDataTable(sql);
            }

            foreach (DataRow dr in dt.Rows)
            {
                resultPatterns.Add(Pattern.FromXml(dr[0].ToString()) as Pattern);
            }

            return resultPatterns;
        }
    }
}
