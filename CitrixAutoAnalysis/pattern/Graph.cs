using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace CitrixAutoAnalysis.pattern
{
    public class Graph: AbstractNode
    {
        public Graph(Guid id, AbstractNode prnt, string name) : base(id, prnt, name, 0) { }

        public bool IsMatch(Graph instance)
        {
            bool MatchSeg = instance.ChildNodes.Count == this.ChildNodes.Count;
            bool MatchLog = instance.LogInCurrent().Count == this.LogInCurrent().Count;
            bool MatchContext = instance.ContextInCurrent().Count == this.ContextInCurrent().Count;// we may need to do some further validation regarding the context value
            bool AnyErrors = instance.LogInCurrent().Any(l => l.IsForDebug) || instance.LogInCurrent().Any(l => l.IsBreakPoint);

            return MatchSeg && MatchLog && MatchContext && !AnyErrors;
        }

        public override string ToXml()
        {
            string xmlContent = "<graph><segments>";
            foreach (Segment seg in this.ChildNodes)
            {
                xmlContent += seg.ToXml();
            }
            xmlContent += "</segments><log>";
            foreach (Log logItem in this.LogInCurrent())
            {
                xmlContent += logItem.ToXml();//here we need the log details;
            }
            xmlContent += "</log><context>";
            foreach (Context c in this.ContextInCurrent())
            {
                xmlContent += c.ToXml();
            }

            xmlContent += "</context></graph>";
            return xmlContent;
        }

        public static AbstractNode FromXml(Pattern pattern, XElement element) {//deserialize the xml format pattern into objects.

            List<XElement> segments = element.Descendants("segment").ToList();
            Graph graph = new Graph(Guid.NewGuid(), pattern, "default graph");

            segments.ForEach(seg => Segment.FromXml(seg, graph));

            List<XElement> log = element.Descendants("logitem").ToList();
            List<Log> parsedLog = new List<Log>();

            foreach(XElement elem in log)
            {
                parsedLog.Add((Log)Log.FromXml(null, elem));
            }

            List<XElement> context = element.Descendants("contextitem").ToList();
            List<Context> parsedContext = new List<Context>();

            foreach (XElement elem in context)
            {
                parsedContext.Add((Context)Context.FromXml(null, elem));
            }

            foreach (Log l in parsedLog)
            {
                List<AbstractNode> real = new List<AbstractNode>();
                foreach (Context con in l.ChildNodes)
                {
                    real.Add(parsedContext.First(p => p.NodeId == con.NodeId));
                }

                l.ChildNodes.Clear();

                foreach(Context  item in real)
                {
                    item.Parent = l;
                    l.ChildNodes.Add(item);
                }
            }

            foreach (Segment seg in graph.ChildNodes)
            {
                List<AbstractNode> real = new List<AbstractNode>();

                foreach(Log l in seg.ChildNodes)
                {
                    real.Add(parsedLog.First(p => p.NodeId == l.NodeId));
                }

                seg.ChildNodes.Clear();

                foreach(Log item in real)
                {
                    item.Parent = seg;
                    seg.ChildNodes.Add(item);
                }
            }

            return graph;
        }
 
        public override string ConstructSql()
        {
            //save the pattern itself into patterntable
            return "insert into SegmentTable values('" + this.NodeId + "','" + this.Parent.NodeId + "','" + this.NodeName + "',1, null)";
        }
    }
}
