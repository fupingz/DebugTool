using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern;

namespace CitrixAutoAnalysis.analysis.engine
{
    class MileStoneEngine
    {
        private Segment segment;
        private CDFHelper helper;

        public MileStoneEngine(Segment seg, CDFHelper cdf)
        {
            segment = seg;
            helper = cdf;
        }
        public List<AbstractNode> ExtractFromCDF()
        {
            List<Log> segLogs = segment.Log.OrderBy(i => i.IndexInSeg).ToList();
            List<AbstractNode> nodes = new List<AbstractNode>();

            //all the logs that matches the first item in the segment
            List<Log> first = GetAllMatchingLog(segLogs[0], helper);

            foreach (Log log in first)
            {
                nodes.Add(ExtractInstanceFromCDF(log));
            }

            return nodes;
        }

        public AbstractNode ExtractInstanceFromCDF(Log log)
        {
            Segment segInstance = new Segment(Guid.NewGuid(), segment.Name, segment.IndexInPattern, null);
            List<Log> segLogs = segment.Log.OrderBy(i => i.IndexInSeg).ToList();
            int temp = helper.GetLogIndex(log);// points to the log that's being processed for the segment

            /*ignore the 1st since it has defintiely matched with the parameter log*/
            for (int index = 0; index < segLogs.Count; index++)
            { 
                //try to match each log
                Log curLog = helper.GetLogByIndex(temp);

                if (index < segLogs.Count - 1)// the last item don't need to find the next
                {
                    HashSet<CDFFilter> filters = new HashSet<CDFFilter>();

                    CDFFilter filter = ConstructFilterPerLogRelation(segLogs[index + 1], curLog);
                    if (filter != null)
                    {
                        filters.Add(filter);
                    }

                    filters.Add(new CDFFilter(CDFCondition.CDF_TEXT, segLogs[index + 1].Text));

                    //we want to use the "filters" to get the exact item we need, because some item may be decided in runtime.
                    segLogs[index + 1].PatternContext.Where(con => con.ContextType == ContextType.ContextFilter).ToList().ForEach(con => filters.Add(new CDFFilter(CDFCondition.CDF_FILTER, con.Value)));

                    temp = helper.GetLogIndexByFiltersFromIndex(filters, temp);
                }

                if (temp == -1)
                {
                    return segInstance;// we may still need to do some further things
                }

                curLog.IndexInSeg = segLogs[index].IndexInSeg;
                curLog.ExtractContextViaPattern(segLogs[index]);//extract all the configured context
                segInstance.AddLog(curLog);
                curLog.PatternContext.ForEach(con => segInstance.AddContext(con));
            }

            segInstance.IndexInPattern = segment.IndexInPattern;

            return segInstance;

        }

        public List<Log> GetAllMatchingLog(Segment node, CDFHelper helper)
        {
            List<Log> result = new List<Log>();

            foreach(Log log in node.Log)
            {
                result.AddRange(GetAllMatchingLog(log, helper));

            }

            return result.OrderBy( log => log.IndexInSeg).ToList();
        }

        public List<Log> GetAllMatchingLog(Log log, CDFHelper helper)
        {
            CDFFilter filter = new CDFFilter(CDFCondition.CDF_TEXT, log.Text);
            HashSet<CDFFilter> filters = new HashSet<CDFFilter>();

            //we want to use the "filters" to get the exact item we need, because some item may be decided in runtime.
            log.PatternContext.Where(con => con.ContextType == ContextType.ContextFilter).ToList().ForEach(con => filters.Add(new CDFFilter(CDFCondition.CDF_FILTER, con.Value)));

            filters.Add(filter);

            return helper.GetAllByFiltersInRange(filters);
        }

        private CDFFilter ConstructFilterPerLogRelation(Log log, Log prev)
        {
            CDFCondition condition;
            string value;
            switch (log.RWP)
            { 
                case RelationWithPrevious.First:
                case RelationWithPrevious.Unknown:
                case RelationWithPrevious.SameSession:
                //ignore same-session-relation for now since it has very little value and CDFControl does not report the info correctly
                    return null;
                case RelationWithPrevious.SameProcess:
                    condition = CDFCondition.CDF_PROCESSID;
                    value = prev.ProcessId.ToString();
                    break;
                case RelationWithPrevious.SameThread:
                    condition = CDFCondition.CDF_THREADID;
                    value = prev.ThreadId.ToString();
                    break;
                default:
                    return null;
            }

            return new CDFFilter(condition, value);
        }

        public void SummarizeIssues()
        {
            //summary the issues and store data into DB
        }
    }
}
