using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace CitrixAutoAnalysis.pattern
{
    class Log : AbstractNode
    {
        private string module;
        private string src;
        private string func;
        private int line;
        private string text;

        private int sessionId;
        private int processId;
        private int threadId;
        private int indexInSeg; // the index of the log in the segment, so we can know the orders

        private DateTime capturedTime;

        private HashSet<Log> equivalents; //  a set of equivalent logs cross different version, they may have different line number, but they are the same

        private RelationWithPrevious rwp;

        //in the segment, we may provide another completely different log item in the place where an expected log should locate,
        //this is used for the situation that falls back to error finding, when the expected item cannot be found
        //e.g. when we are trying to find the item "this_is_the_end_of_app_startup", if we don't find it, we find another most relevant error "application_startup_failed",
        //to fill in the same location where above expected log should be, all later issue summarization will be based on this.
        private bool isForDebug = false;

        //mark the log item as the location that most close to break
        private bool isBreakPoint = false;

        private static string ParamMagic = @"*#_PARAM_INDEX_";

        public Log(string Module, string Src, string Func, int Line, string Text, int SessionId, int ProcessId, int ThreadId, DateTime CapturedTime) {
            this.module = Module;
            this.src    = Src;
            this.func   = Func;
            this.line   = Line;
            this.text   = Text;

            this.sessionId = SessionId;
            this.processId = ProcessId;
            this.threadId = ThreadId;

            this.capturedTime = CapturedTime;
        }


        public Log(Guid id, string Module, string Src, string Func, int Line, string Text, int SessionId, int ProcessId, int ThreadId, DateTime CapturedTime, int index, RelationWithPrevious Rwp)
            : base(id)
        {
            this.module = Module;
            this.src = Src;
            this.func = Func;
            this.line = Line;
            this.text = Text;

            this.sessionId = SessionId;
            this.processId = ProcessId;
            this.threadId = ThreadId;

            this.capturedTime = CapturedTime;

            this.indexInSeg = index;

            this.rwp = Rwp;
        }

        public override bool IsMatch(AbstractNode instance) {

            var log = instance as Log;

            foreach(Log item in equivalents){

                if (this.Mode == CDFLogMode.Unmanaged)
                {
                    if (Regex.IsMatch(log.text, item.text))
                    {
                        //for unmanged log, e.g. brokeragent, we will need to check if the log text matches the pattern
                        return true;
                    }
                }
                else
                {
                    if (log.line == item.line)
                    {
                        return true; // for managed code, log from the same file(tmfId) with same line# would be good engough.
                    }
                }
            }

            return false;
        }

        public override string ToXml() {
            string xmlContent = "<item>";

            xmlContent += "<id>" + this.NodeId + "</id>";
            xmlContent += "<time>" + this.CapturedTime + "</time>";
            xmlContent += "<src>" + this.Src + "</src>";
            xmlContent += "<func>" + this.Func + "</func>";
            xmlContent += "<line>" + this.Line + "</line>";
            xmlContent += "<module>" + this.Module + "</module>";
            xmlContent += "<sessionId>" + this.SessionId + "</sessionId>";
            xmlContent += "<processId>" + this.ProcessId + "</processId>";
            xmlContent += "<threadId>" + this.ThreadId + "</threadId>";
            xmlContent += "<text>" + this.Text + "</text>";

            xmlContent += "<context>";
            foreach (Context c in this.PatternContext)
            {
                xmlContent += "<id>"+c.Id+"</id>";//here we just need the Id to reference to the context
            }
            xmlContent += "</context>";

            xmlContent += "</item>";

            return xmlContent;
        }

        public static AbstractNode FromXml(Pattern pattern, XElement element)
        {
            string id = element.Descendants("id").First().Value;
            string index = element.Descendants("index").First().Value;
            string time = element.Descendants("time").First().Value;
            string src = element.Descendants("src").First().Value;
            string func = element.Descendants("func").First().Value;
            string line = element.Descendants("line").First().Value;
            string module = element.Descendants("module").First().Value;
            string sessionId = element.Descendants("sessionId").First().Value;
            string processId = element.Descendants("processId").First().Value;
            string threadId = element.Descendants("threadId").First().Value;
            string rwp = element.Descendants("RelationWithPrevious").First().Value;
            string text = element.Descendants("text").First().Value;

            List<Guid> contextIds = new List<Guid>();
            element.Descendants("context").Descendants("id").ToList().ForEach(e => contextIds.Add(Guid.Parse(e.Value)));

            Log log = new Log(Guid.Parse(id), module, src, func, Convert.ToInt32(line), text, 
                              Convert.ToInt32(sessionId), Convert.ToInt32(processId), Convert.ToInt32(threadId), 
                              DateTime.Parse(time), Convert.ToInt32(index),LogRelationConverter.StringToLogRelation(rwp));

            log.PatternContext = pattern.PatternContext.Where(e => contextIds.Any(c =>e.Id == c)).ToList();

            return log;
        }

        public void ExtractContextViaPattern(Log pattern)
        {
            foreach (Context con in pattern.PatternContext)
            {
                Context context = new Context(Guid.NewGuid(), con.Name, con.ParamIndex, this.NodeId, con.ContextType);
                context.Value = FillInParamValue(pattern, context.ParamIndex);

                this.AddContext(context);
            }
        }

        private string FillInParamValue(Log pattern, int expectedIndex)
        {
            string ptnText = pattern.Text;
            string instText = this.Text;
            string paramVal = null;
            for (int i = 1; i <= expectedIndex+1; i++)
            {
                int beginIndex = ptnText.IndexOf(ParamMagic + i);
                if (i == 1)
                {
                    ptnText = ptnText.Substring(beginIndex);
                    instText = instText.Substring(beginIndex);
                }
                else
                {
                    if (beginIndex == -1)
                    {
                        string finalLiteral = ptnText.Substring((ParamMagic + (i - 1)).Length).Trim();
                        if (finalLiteral.Length > 0)
                        {
                            return instText.Substring(0, instText.IndexOf(finalLiteral));
                        }
                        else {
                            return instText;
                        }
                    }
                    string constLiteral = ptnText.Substring(0, beginIndex).Substring((ParamMagic + (i-1)).Length);
                    paramVal = instText.Substring(0,instText.IndexOf(constLiteral));

                    ptnText = ptnText.Substring(beginIndex);
                    instText = instText.Substring((constLiteral + paramVal).Length);
                }
            }

            return paramVal;
        }

        public CDFLogMode Mode {
            get {
                if (this.src == "_#dotNet#_")
                {
                    return CDFLogMode.Unmanaged;
                }
                else {
                    return CDFLogMode.Managed;
                }
            }
        }

        public string Module {
            get { return module; }
        }

        public string Src {
            get { return src; }
        }

        public string Func { 
            get{ return func; }
        }

        public int Line {
            get { return line; }
        }

        public string Text {
            get { return text; }
        }

        public int SessionId
        {
            get { return sessionId; }
        }

        public int ProcessId
        {
            get { return processId; }
        }

        public int ThreadId
        {
            get { return threadId; }
        }

        public DateTime CapturedTime {
            get { return capturedTime; }
        }

        // we are reusing this property,
        // for pattern logs, it''s the index in the pattern
        // for real CDF lofs, it's the index in the log file
        public int IndexInSeg
        {
            get { return indexInSeg; }
            set { indexInSeg = value; }
        }

        public RelationWithPrevious RWP {
            get { return rwp; }
            set { rwp = value; }
        }
        public bool IsForDebug
        {
            get { return isForDebug; }
            set { isForDebug = value; }
        }

        public bool IsBreakPoint
        {
            get { return isBreakPoint; }
            set { isBreakPoint = value; }
        
        }
    }
    public enum CDFLogMode
    {
        Unmanaged,
        Managed
    }

    public enum RelationWithPrevious{
        Unknown,
        First,
        SameSession,
        SameProcess,
        SameThread
    }

    public class LogRelationConverter
    {
        public static string LogRelationToString(RelationWithPrevious relation)
        {
            switch (relation)
            {
                case RelationWithPrevious.First:
                    return "First";
                case RelationWithPrevious.SameSession:
                    return "SameSession";
                case RelationWithPrevious.SameProcess:
                    return "SameProcess";
                case RelationWithPrevious.SameThread:
                    return "SameThread";
            }

            return "Unknown";
        }

        public static RelationWithPrevious StringToLogRelation(string relation)
        {
            if (relation == "First")
            {
                return RelationWithPrevious.First;
            }
            else if (relation == "SameSession")
            {
                return RelationWithPrevious.SameSession;
            }
            else if (relation == "SameProcess")
            {
                return RelationWithPrevious.SameProcess;
            }
            else if (relation == ("SameThread"))
            {
                return RelationWithPrevious.SameThread;
            }

            return RelationWithPrevious.Unknown;
        }
    }
}
