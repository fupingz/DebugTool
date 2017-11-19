using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern;
using CitrixAutoAnalysis.analysis.io;
using CitrixAutoAnalysis.analysis.tools;

using CitrixAutoAnalysis.analysis.scheduler;

using CitrixAutoAnalysis.ParsePatern;

namespace CitrixAutoAnalysis.analysis.engine
{
    class CDFHelper
    {
        // we may have difffernt helpers for different node in the pattern, 
        // however we hope they can use the same CDF log set, and differentiate each other with the range
        private List<Log> allLog; 
        private CDFRange range;
        private Pattern pattern;
        private Job job;

        public CDFHelper(Job job)
        {
            this.job = job;
            Initialize();
        }

        private void Initialize() 
        {
            this.pattern = FindProperPattern("","",""/*job.ProdId, job.VersionId, job.hotfix*/);
            this.allLog = ReadLogByPattern();

            if (allLog == null)
            {
                range = null;
                return;
            }
            range = new CDFRange(0, allLog.Count - 1);
        }

        private Pattern FindProperPattern(string Product, string Version, string hotfix)
        {
            //here we need to find the proper patterns per the input params.
            string path = Environment.CurrentDirectory;
            path = path.Substring(0, path.IndexOf("CitrixAutoAnalysis")) + "CitrixAutoAnalysis\\CitrixAutoAnalysis\\pattern\\patterns\\cdfxml.xml";
            return (Pattern)Pattern.FromXml(path);
        }
        private string ReadLogTableNameByJobId(uint jobId)
        {
            string sql = "select ResultTable from RawTraceFiles where JobID = "+ jobId;
            string logTableName = "";
            using (DBHelper helper = new DBHelper())
            { 
                logTableName = helper.RetriveStringFromDB(sql);
            }
            return logTableName;

        }
        private List<Log> ReadLogByPattern()
        {
            List<Log> log = new List<Log>();
            string logTable = ReadLogTableNameByJobId(job.JobId);

            if (logTable.Length == 0)
            {
                //I probably need to elegantly stop processing
                return null;
            }

            string sql = "Select * from " + logTable + " where moduleName in (";
            HashSet<string> modules = pattern.GetAllModules();
            int index = 1;

            foreach (string module in modules)
            {
                sql += "'" + module + "'";
                if (index++ == modules.Count)
                {
                    sql += ") order by ID";
                }
                else { 
                    sql += ',';
                }
            }

            DataTable dt;

            using (DBHelper helper = new DBHelper())
            {
                dt = helper.FillDataTable(sql);
            }

            return ParseDBDataIntoLog(dt);
        }

        private List<Log> ParseDBDataIntoLog(DataTable dt)
        {
            List<Log> log = new List<Log>();
            if (dt == null || dt.Rows.Count == 0)
            {
                return null;
            }

            foreach (DataRow row in dt.Rows)
            {
                string module = DBConverter.StringFromDBItem(row[8]);
                string src = DBConverter.StringFromDBItem(row[9]);
                string func = DBConverter.StringFromDBItem(row[11]);
                int line = DBConverter.IntFromDBItem(row[10]);
                string text = DBConverter.StringFromDBItem(row[14]);
                int sessionId = DBConverter.IntFromDBItem(row[7]);
                int processId = DBConverter.IntFromDBItem(row[5]);
                int threadId = DBConverter.IntFromDBItem(row[3]);
                int index = DBConverter.IntFromDBItem(row[0]);// here the ID looks the index, needs to confirm
                DateTime time = DBConverter.DatetimeFromDBItem(row[2]) ?? DateTime.Now;
                log.Add( new Log(Guid.NewGuid(), module, src, func, line, text, sessionId, processId, threadId,time, index,RelationWithPrevious.Unknown));
            }

            return log;
        }


        public Log GetLogByIndex(int index)
        {
            CheckRange(index);

            return allLog[index];
        }

        public void ProcessJob(Job job)
        {
            if (this.range == null)
            { 
                //log here is something wrong reading the log
                return;
            }
            //here a job handling begins.
            try
            {
                job.UpdateJobStartInfo();

                TopDownEngine engine = new TopDownEngine(this, pattern.Graph);
                List<Graph> instances = engine.ExtractFromCDF();

                List<Pattern> result = FillInPatternInfo(instances);

                // here we are processing the result
                SummarizeAndOutputToPersistance(result);
            }
            catch (Exception ex)
            {
                job.UpdateJobFailedInfo();
            }

            job.UpdateJobFinishInfo();
        }

        public Log GetNextbyFiltersFromIndex(HashSet<CDFFilter> filters, int index)
        {
            CheckRange(index);

            for (int i = index; i < allLog.Count; i++)
            {
                bool IsMatch = true;
                foreach (CDFFilter f in filters)
                {
                    if (!f.IsMatch(allLog[i]))
                    {
                        IsMatch = false;
                        break;
                    }
                }

                if (IsMatch)
                {
                    return allLog[i];
                }
            }
            
            return null;// cannot find one item meeting the requirement
        }

        public int GetLogIndexByFiltersFromIndex(HashSet<CDFFilter> filters, int index)
        {
            CheckRange(index);

            for (int i = index; i < allLog.Count; i++)
            {
                bool IsMatch = true;
                foreach (CDFFilter f in filters)
                {
                    if (f == null)
                    {
                        continue;
                    }
                    if (!f.IsMatch(allLog[i]))
                    {
                        IsMatch = false;
                        break;
                    }
                }

                if (IsMatch)
                {
                    return i;
                }
            }

            return -1;// cannot find one item meeting the requirement
        }

        public List<Log> GetAllByFiltersInRange(HashSet<CDFFilter> filters)
        {
            int index = range.Begin;
            List<Log> logs = new List<Log>();

            while ((index = GetLogIndexByFiltersFromIndex(filters, index+1)) > 0)
            {
                logs.Add(allLog[index]);
            }

            return logs;
        }

        public int GetLogIndex(Log log) { //this is not the index in the segment, instead it's the index in the real-time captured log
            for (int index = this.range.Begin; index < this.range.End; index++)
            {
                if (log == allLog[index])
                    return index;
            }

            return -1;//not found
        }

        private void CheckRange(int index)
        {
            if (index < range.Begin || index > range.End)
            {
                throw new System.ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// the engine can help us constructing the graph, but still a bit further work(fill in the pattern info) left to do
        /// </summary>
        /// <param name="graphs"> the graphs extracted out of the log per our analysis</param>
        /// <returns>the final result</returns>
        private List<Pattern> FillInPatternInfo(List<Graph> graphs)
        {
            List<Pattern> patterns = new List<Pattern>();
            foreach (Graph g in graphs)
            {
                Pattern newPattern = new Pattern(Guid.NewGuid(), pattern.PatternName, pattern.ProductVersion);
                newPattern.Graph = g;
                newPattern.Log = g.Log;
                newPattern.PatternContext = g.PatternContext;
                patterns.Add(newPattern);
            }

            return patterns; 
        }

        private void SummarizeAndOutputToPersistance(List<Pattern> result)
        {
            Summarize(result);
            OutputToPersistance(result);
        }

        private void Summarize(List<Pattern> result)
        {
            foreach (Pattern ptn in result)
            {
                Log logErr = ptn.Log.First(l => l.IsForDebug);
                Log logBreakPoint = ptn.Log.First(l => l.IsBreakPoint);

                if (logErr == null && logBreakPoint == null)
                {
                    //probably there is not anything wrong
                    continue;
                }

                new IssueSummary(logBreakPoint, logErr, ptn.NodeId, (int)this.job.JobId,job.LCId, "automated analysis result", null).OutputIssueToDB();
            }
        }

        private void OutputToPersistance(List<Pattern> result)
        { 
            // output to database
            ParsePatternCore PPCore = new ParsePatternCore();
            foreach (Pattern pt in result)
                PPCore.ParsePattern(pt);
        }
    }


    class CDFRange
    {
        private int begin;
        private int end;
        public CDFRange(int begin, int end)
        {
            this.begin = begin;
            this.end = end;
        }
        public int Begin {
            get { return begin; }
        }
        public int End {
            get { return end; }
        }
    }
}
