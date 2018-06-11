using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.analysis.io;
using CitrixAutoAnalysis.analysis.tools;

using System.Data;

namespace CitrixAutoAnalysis.analysis.scheduler
{
    class Job
    {
        public uint JobId{get;set;}
        public uint OwnerId { get; set; }
        public string LCId {get;set;}
        public uint ProdId { get; set; }
        public uint VersionId { get; set; }
        public uint ComponentId { get; set; }
        public string Description { get; set; }
        public uint StatusId { get; set; }
        public uint IssueId { get; set; }
        public bool EmailNotified { get; set; }
        public Nullable<DateTime> ParseEndTime { get; set; }
        //here I am using the ParseEndTime as the indicator that job has not been processed, we need to change this in future
        public Nullable<DateTime> AnalysisBeginTime { get; set; }
        public Nullable<DateTime> AnalysisEndTime { get; set; }

        public static Job CreateNewJobViaDatBaseRow(DataRow row)
        {
            Job job = new Job();

            job.JobId = DBConverter.UintFromDBItem(row[0]);
            job.OwnerId = DBConverter.UintFromDBItem(row[1]); ;
            job.LCId = DBConverter.StringFromDBItem(row[2]);
            job.ProdId = DBConverter.UintFromDBItem(row[3]);
            job.VersionId = DBConverter.UintFromDBItem(row[4]);
            job.ComponentId = DBConverter.UintFromDBItem(row[5]);
            job.Description = DBConverter.StringFromDBItem(row[6]);
            job.StatusId = DBConverter.UintFromDBItem(row[7]);
            job.IssueId = DBConverter.UintFromDBItem(row[8]);
            job.EmailNotified = DBConverter.BoolFromDBItem(row[9]);
            job.ParseEndTime = DBConverter.DatetimeFromDBItem(row[13]);
            job.AnalysisBeginTime = DBConverter.DatetimeFromDBItem(row[14]);
            job.AnalysisEndTime = DBConverter.DatetimeFromDBItem(row[15]);

            return job;
        }

        

        public static List<Job> GetUnprocessJobs()
        {
            string sql = "Select * from CadJobs where statusId = " + JobStatus.JOB_STATUS_READY_FOR_ANSLYSIS;
            List<Job> jobs = new List<Job>();
            DataTable dt;

            using (DBHelper helper = new DBHelper())
            {

                dt = helper.FillDataTable(sql);

                if (dt.Rows.Count <= 0)
                {
                    return null;
                }

                foreach (DataRow row in dt.Rows)
                {
                    jobs.Add(Job.CreateNewJobViaDatBaseRow(row));
                }
            }

            return jobs;
        }

        public static Job GetTheMostUrgentJob()
        {
            string sql = "Select top 1 * from CadJobs where statusId = 6 order by ParseEndTime";
            List<Job> jobs = new List<Job>();
            DataTable dt;

            using(DBHelper helper = new DBHelper())
            { 
                dt = helper.FillDataTable(sql);

                if(dt.Rows.Count != 1)
                {
                  return null;
                }
             }

            return Job.CreateNewJobViaDatBaseRow(dt.Rows[0]);
        }

        public void UpdateJobStatus(JobStatus status)
        {
            string SqlString = "update CadJobs set StatusId = " + (int)status + " where id = " + this.JobId;

            using (DBHelper helper = new DBHelper())
            {
                helper.UpdateDB(SqlString);
            }
        }

        public void UpdateJobStartInfo()
        {
            string SqlString = "update CadJobs set StatusId = " + (int)JobStatus.JOB_STATUS_IN_ANALYZING + ",AnalyzeStartTime = SYSDATETIMEOFFSET() where id = " + this.JobId;
            
            using (DBHelper helper = new DBHelper())
            {
                helper.UpdateDB(SqlString);
            }
        }

        public void UpdateJobFailedInfo(string message)
        {
            string SqlString = "update CadJobs set StatusId = " + (int)JobStatus.JOB_STATUS_FAILED + ",AnalyzeEndTime = SYSDATETIMEOFFSET(), StatusMsg = '"+message+"' where id = " + this.JobId;

            using (DBHelper helper = new DBHelper())
            {
                helper.UpdateDB(SqlString);
            }
        }

        public void UpdateJobFinishInfo()
        {
            string SqlString = "update CadJobs set StatusId = " + (int)JobStatus.JOB_STATUS_FINISHED + ",AnalyzeEndTime = SYSDATETIMEOFFSET() where id = " + this.JobId;

            using (DBHelper helper = new DBHelper())
            {
                helper.UpdateDB(SqlString);
            }
        }

    }

    enum JobStatus {
        JOB_STATUS_UNKNOWN,
        JOB_STATUS_DRAFT,
        JOB_STATUS_SUBMITTED,
        JOB_STATUS_IN_PREPARATION,
        JOB_STATUS_READY_FOR_PARSING,
        JOB_STATUS_IN_PARSING,
        JOB_STATUS_READY_FOR_ANSLYSIS,
        JOB_STATUS_IN_ANALYZING,
        JOB_STATUS_FINISHED,
        JOB_STATUS_FAILED = 99
    }
}
