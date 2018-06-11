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
            List<Log> segLogs = segment.LogInCurrent().OrderBy(i => i.IndexInParent).ToList();
            List<AbstractNode> nodes = new List<AbstractNode>();

            for (int index = 0; index < segLogs.Count && index < 3; index++)
            {
                //all the logs that matches the first item in the segment
                List<Log> first = GetAllMatchingLog(segLogs[index], helper);

                if (first.Count > 0)
                {
                    foreach (Log log in first)
                    {
                        log.IndexInParent = index+1;
                        nodes.Add(ExtractInstanceFromCDF(log));
                    }
                    break;
                }
            }



            return nodes;
        }

        public AbstractNode ExtractInstanceFromCDF(Log log)
        {
            Segment segInstance = new Segment(Guid.NewGuid(),null, segment.NodeName, segment.IndexInParent);
            List<Log> segLogs = segment.LogInCurrent().OrderBy(i => i.IndexInParent).ToList();
            int temp = helper.GetLogIndex(log);// points to the log that's being processed for the segment
            bool QuitingDueToErrOrExcep = false;

            /*ignore the 1st since it has defintiely matched with the parameter log*/
            for (int index = log.IndexInParent - 1; index < segLogs.Count; index++)
            { 
                //try to match each log
                if (temp == -1)
                {
                    break;
                }

                Log curLog = helper.GetLogByIndex(temp);
                curLog.IndexInParent = segLogs[index].IndexInParent;
                curLog.IsUsed = true; // so no else segment will find me
                curLog.Parent = segInstance;
                segInstance.AddChildNode(curLog);
                
                if(QuitingDueToErrOrExcep)
                {
                    //this is an expected error log
                    curLog.IsForDebug = true;
                    break;
                }
                curLog.RWP = segLogs[index].RWP;
                curLog.ExtractContextViaPattern(segLogs[index]);//extract all the configured context

                if (!curLog.EvaluateContext())/*the log itself indicates something wrong via the param value, e.g. the validation result*/
                {
                    //some assertion fails
                    curLog.IsBreakPoint = true;
                    break;
                }

                if (index < segLogs.Count - 1)// the last item don't need to find the next
                {
                    HashSet<CDFFilter> filters = new HashSet<CDFFilter>();

                    filters.Add(new CDFFilter(CDFCondition.CDF_TEXT, segLogs[index + 1].Text));

                    //we want to use the "filters" to get the exact item we need, because some item may be decided in runtime.
                    segLogs[index + 1].ContextInCurrent().Where(con => con.ContextType == ContextType.ContextFilter).ToList().ForEach(con => filters.Add(new CDFFilter(CDFCondition.CDF_FILTER, con.Assertion)));

                    var tempIndex = -1;

                    if (segLogs[index + 1].RWP == RelationWithPrevious.Parallel)
                    {
                        //this indicates the expected log may not be later than current one, so we just search from the beginning of current segment
                        tempIndex = helper.GetLogIndexByFiltersFromIndex(filters, helper.GetLogIndex((Log)segInstance.ChildNodes[0]));
                    }
                    else 
                    {
                        CDFFilter filter = ConstructFilterPerLogRelation(segLogs[index + 1], curLog);
                        if (filter != null)
                        {
                            filters.Add(filter);
                        }

                        tempIndex = helper.GetLogIndexByFiltersFromIndex(filters, temp);
                    }

                    if (tempIndex == -1)
                    {
                        curLog.IsBreakPoint = true;// we may still need to do some further things

                        //something bad happens, for now we just define this as an error,
                        //we need to be more robust in future realizing that missing one item does not necessarily mean something wrong((because CDFControl sometimes loses items)

                        HashSet<CDFFilter> errFilters = new HashSet<CDFFilter>();
                        errFilters.Add(new CDFFilter(CDFCondition.CDF_ERROR_EXCEPTION, ""));
                        errFilters.Add(new CDFFilter(CDFCondition.CDF_PROCESSID, curLog.ProcessId.ToString()));//we let it to be in the same process

                        tempIndex = helper.GetLogIndexByFiltersFromIndex(errFilters, temp);
                        QuitingDueToErrOrExcep = true;
                    }

                    temp = tempIndex;
                    
                }
            }

            return segInstance;

        }

        public List<Log> GetAllMatchingLog(Segment node, CDFHelper helper)
        {
            List<Log> result = new List<Log>();

            foreach(Log log in node.LogInCurrent())
            {
                result.AddRange(GetAllMatchingLog(log, helper));

            }

            return result.OrderBy(log => log.IndexInParent).ToList();
        }

        public List<Log> GetAllMatchingLog(Log log, CDFHelper helper)
        {
            CDFFilter filter = new CDFFilter(CDFCondition.CDF_TEXT, log.Text);
            HashSet<CDFFilter> filters = new HashSet<CDFFilter>();

            //we want to use the "filters" to get the exact item we need, because some item may be decided in runtime.
            log.ContextInCurrent().Where(con => con.ContextType == ContextType.ContextFilter).ToList().ForEach(con => filters.Add(new CDFFilter(CDFCondition.CDF_FILTER, con.Assertion)));

            filters.Add(filter);

            if (log.Func != null && log.Func.Length > 0)
            {
                filters.Add(new CDFFilter(CDFCondition.CDF_FUNC, log.Func));
            }

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
    }
}
