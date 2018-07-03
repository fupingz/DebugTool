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

        public CDFHelper(Job job, Pattern ptn)
        {
            this.job = job;
            this.pattern = ptn;
            Initialize();
        }

        private void Initialize() 
        {
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
            string path = Environment.CurrentDirectory+"\\cdfxml.xml";
            //if (!System.IO.File.Exists(path))
            //{
            //    path = path.Substring(0, path.IndexOf("CitrixAutoAnalysis")) + "CitrixAutoAnalysis\\pattern\\patterns\\cdfxml.xml";
            //}
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
                    sql += ") order by LogTime";
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
            int indexInResult = 0;
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
                log.Add( new Log(Guid.NewGuid(), null, module, src, func, line, text, sessionId, processId, threadId,time, 0, index, indexInResult++, RelationWithPrevious.Unknown));
            }

            return log;
        }


        public Log GetLogByIndex(int index)
        {
            CheckRange(index);

            return allLog[index];
        }

        public void ProcessJob()
        {
            //here a job handling begins.
            if (this.range == null)
            {
                //log here is something wrong reading the log
                System.Diagnostics.Trace.WriteLine("Job(id="+job.JobId+") ： The uploaded log does not contain any useful data against the components for your selected pattern, so returning");
                throw new NoAppropriateTraecFoundException("The uploaded trace does not contain any useful dataagainst the components for your selected pattern("+pattern.NodeName+"), please check if necessary moduels were included at trace colelction.");
            }

            TopDownEngine engine = new TopDownEngine(this, pattern.Graph);
            List<Graph> instances = engine.ExtractFromCDF();

            System.Diagnostics.Trace.WriteLine("Job(id=" + job.JobId + ") ：--- SHOWING THE RESULT ---");
            foreach (Graph g in instances)
            {
                System.Diagnostics.Trace.WriteLine("Job(id=" + job.JobId + ") ：instances("+g.NodeId+")");
                System.Diagnostics.Trace.WriteLine("Job(id=" + job.JobId + ") ：Seg("+g.ChildNodes.Count+") , Log (" + g.LogInCurrent().Count+ ") , Context("+g.ContextInCurrent().Count+")");

                //break;
            }
            System.Diagnostics.Trace.WriteLine("Job(id=" + job.JobId + ") ：--- SHOWING THE RESULT ---");
            List<Pattern> result = FillInPatternInfo(instances);

            // here we are processing the result

            SummarizeAndOutputToPersistance(result);

            System.Diagnostics.Trace.WriteLine("Job(id=" + job.JobId + ") ：processsing done");
        }

        public Log GetNextbyFiltersFromIndex(HashSet<CDFFilter> filters, int index)
        {
            CheckRange(index);

            for (int i = index; i < allLog.Count; i++)
            {
                if (allLog[i].IsUsed)
                {
                    continue;
                }

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
                    return allLog[i];
                }
            }
            
            return null;// cannot find one item meeting the requirement
        }

        public int GetLogIndexByFiltersFromIndex(HashSet<CDFFilter> filters, int index)
        {
            if(!CheckRange(index))
                return -1;


            for (int i = index; i < allLog.Count; i++)
            {
                if (allLog[i].IsUsed)
                {
                    continue;
                }

                if (allLog[i].Text.IndexOf("IsUserAllowedToLogon") >=0)
                {
                    var me = 1209;
                }

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
            return log.IndexInResult;
        }

        private bool CheckRange(int index)
        {
            if (index < range.Begin || index > range.End)
            {
                //throw new System.ArgumentOutOfRangeException();
                return false;
            }

            return true;
        }

        public List<Log> GetErrorAndExpcetionsFromCurrentRange()
        {
           return this.allLog.Where(l => l.Text.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   l.Text.IndexOf("err", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   l.Text.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0
                             ).ToList();
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
                Pattern newPattern = new Pattern(Guid.NewGuid(), pattern.NodeName, pattern.ProductVersion, true);
                g.Parent = newPattern;
                newPattern.AddChildNode(g);

                patterns.Add(newPattern);

                List<Log> allBreak = newPattern.LogInCurrent().FindAll(l => l.IsBreakPoint);

                if (allBreak.Count > 1)//if we have multiple, then we remain the last one
                {
                    Log lastBreak = newPattern.LogInCurrent().Last(l => l.IsBreakPoint);

                    foreach (Log l in allBreak)
                    {
                        if (l != lastBreak)
                        {
                            l.IsBreakPoint = false;
                        }
                    }
                }

            }

            return patterns; 
        }

        private void SummarizeAndOutputToPersistance(List<Pattern> result)
        {
            Summarize(result);
            //we may still change some data(e.g. the breakpoint) when summarizing the issue, so persisting later
            OutputToPersistance(result);
        }

        private void Summarize(List<Pattern> result)
        {
            if (result.Count == 0)
            { 
                //nothing found, probably there is something wrong with the log
                string RootCause = "No analyzable trace was found from the uploaded log. please check following items: " +
                                   "<br/>1 : following modules are included when capturing the CDF log:" +
                                   "    <br/>BrokerAgent, TDICA, RPM, Seamless" +
                                   "<br/>2 : you don't have private binaries applied when capturing the log, else you need to provide the tmf files for analysis";

                new IssueSummary(RootCause, Guid.NewGuid(), (int)this.job.JobId, job.LCId, "automated analysis result", new HashSet<string>{"No","Analyzable","Trace","Found"}).OutputIssueToDB();
            }

            if(result.Any(ptn =>ptn.IsMatch(pattern))&& result.All(ptn =>ptn.IsMatch(pattern)))
            {
                //all good results, so at least show one
                new IssueSummary("the connection completes perfectly", result.First().NodeId, (int)this.job.JobId,job.LCId, "automated analysis result", new HashSet<string>(){"succeeded"}).OutputIssueToDB();
            }

            foreach (Pattern ptn in result)
            {
                if (ptn.IsMatch(pattern))
                {
                    //probably there is not anything wrong
                    continue;
                }

                //if (ptn.LogInCurrent().Count < ((pattern.LogInCurrent().Count) / 5)) //too few items found, so ignore it
                //{
                //    continue;
                //}

                if (!ptn.SegInCurrent().Any(seg => seg.IndexInParent == 1))
                {
                    continue;
                }

                // we did not find the last segment, so the final one we can find is a break point
                if(!ptn.SegInCurrent().Exists( Segment => Segment.IndexInParent == pattern.SegInCurrent().Count))
                {
                    for(int index = pattern.SegInCurrent().Count; index > 0 ; index --)
                    {
                        //find the final one
                        if(ptn.SegInCurrent().Exists( seg => seg.IndexInParent == index))
                        {
                            Segment tempSeg =  ptn.SegInCurrent().First(seg => seg.IndexInParent == index) as Segment;
                            Segment templateSeg = pattern.SegInCurrent().First(seg => seg.IndexInParent == index) as Segment;
                            //the final one looks good, so created a fake seg for the result
                            if(tempSeg.LogInCurrent().Count == templateSeg.LogInCurrent().Count)
                            {
                                Log templateLog = tempSeg.LogInCurrent().First(l => l.IndexInParent == tempSeg.LogInCurrent().Count);
                                templateLog.IsBreakPoint = true;

                                Segment templateSeg2 = pattern.SegInCurrent().First(seg => seg.IndexInParent == index+1) as Segment;
                                Log tempplateLog2 = templateSeg2.ChildNodes[0] as Log;
                                Segment fakeSeg = new Segment(Guid.NewGuid(), ptn.Graph, templateSeg2.NodeName, tempSeg.IndexInParent + 1);
                                Log tempLog2 = new Log(Guid.NewGuid(), fakeSeg, tempplateLog2.Module, tempplateLog2.Src, tempplateLog2.Func, tempplateLog2.Line, "MISSED TO LOCATE ( " + tempplateLog2.Text+" )", 0, 0, 0, tempplateLog2.CapturedTime, 1, 1, ptn.LogInCurrent().Count + 1, RelationWithPrevious.Unknown);
                                tempLog2.IsForDebug = true;

                                foreach(Context c in tempplateLog2.ContextInCurrent())
                                {
                                    if (c.ContextType == ContextType.ContextAssertion || c.ContextType == ContextType.ContextFilter)
                                    {
                                        tempLog2.Text = tempLog2.Text.Replace(Log.ParamMagic+c.ParamIndex, c.Assertion);
                                    }
                                }

                                fakeSeg.AddChildNode(tempLog2);

                                ptn.Graph.AddChildNode(fakeSeg);

                                break;
                            }
                        }
                    }
                }

                HashSet<string> Keywords = new HashSet<string>();
                string RootCause = "remain unknown";
                Log logErr = ptn.GetDebugPoint();
                Log logBreakPoint = ptn.GetBreakPoint();

                if (logBreakPoint == null)
                {

                    ptn.LogInCurrent().ForEach(l =>
                                               {
                                                   if (logBreakPoint == null || (logBreakPoint.Parent.IndexInParent <= l.Parent.IndexInParent && logBreakPoint.IndexInParent < l.IndexInParent))
                                                   {
                                                       logBreakPoint = l;
                                                   }
                                               });
                }
                logBreakPoint.IsBreakPoint = true;

                //always calculate a breakpoint since we know there is something wrong
                Keywords.Add(logBreakPoint.Module);

                if (logBreakPoint.Func == null || logBreakPoint.Func.Length == 0)
                {
                    RootCause = "The connection sequence breaks around printing Log : " + logBreakPoint.Text;
                }
                else
                {
                    Keywords.Add(logBreakPoint.Func);
                    RootCause = "The connection sequence breaks around calling <b>" + logBreakPoint.Func + "</b> and printing Log : <br/><b> " + logBreakPoint.Text + "</b>";
                }

                if (logErr != null)
                {
                    if (logErr.Func != null && logErr.Func.Length != 0)
                    {
                        Keywords.Add(logErr.Func);
                    }
                    RootCause += "<br/>And a potentially responsible error recorded : <b>" + logErr.Text + "</b>";
                }

                new IssueSummary(RootCause, ptn.NodeId, (int)this.job.JobId, job.LCId, "automated analysis result", Keywords).OutputIssueToDB();
            }
        }

        private void OutputToPersistance(List<Pattern> result)
        { 
            // output to database
            ParsePatternCore PPCore = new ParsePatternCore();
            foreach (Pattern pt in result)
                PPCore.ParsePattern(pt, true);
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
