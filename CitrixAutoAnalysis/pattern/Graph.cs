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
        private List<Segment> segments = new List<Segment>();
        private HashSet<Log> log = new HashSet<Log>();

        public override bool IsMatch(AbstractNode instance)
        {
            return false;
        }

        public override string ToXml()
        {
            string xmlContent = "<graph><segments>";
            foreach (Segment seg in segments)
            {
                xmlContent += seg.ToXml();
            }
            xmlContent += "</segments><log>";
            foreach (Log logItem in log)
            {
                xmlContent += logItem.ToXml();//here we need the log details;
            }
            xmlContent += "</log><context>";
            foreach (Context c in this.PatternContext)
            {
                xmlContent += c.ToXml();
            }

            xmlContent += "</context></graph>";
            return xmlContent;
        }

        public static AbstractNode FromXml(Pattern pattern, XElement element) {//deserialize the xml format pattern into objects.

            List<XElement> segments = element.Descendants("segment").ToList();
            List<XElement> log = element.Descendants("logitem").ToList();
            List<XElement> context = element.Descendants("contextitem").ToList();
            Graph graph = new Graph();

            context.ForEach(e => pattern.AddContext(Context.FromXml(e)));
            log.ForEach(e => pattern.AddLog((Log)(CitrixAutoAnalysis.pattern.Log.FromXml(pattern, e))));

            graph.PatternContext = pattern.PatternContext;
            graph.Log = pattern.Log;

            segments.ForEach(e => graph.AddSegment((Segment)(Segment.FromXml(e, graph))));

            return graph;
        }

        public void AddSegment(Segment segment) {
            this.segments.Add(segment);
        }

        public void AddLog(Log logNode)
        {
            this.log.Add(logNode);
        }

        public Log GetPatternLogById(Guid guid)
        {
            return log.First(l => l.NodeId.Equals(guid));
        }

        public Segment GetPatternSegById(Guid guid)
        {
            return segments.First(s => s.NodeId.Equals(guid));
        }

        public List<Segment> Segments
        {
            get { return segments; }
            set { segments = value; }
        }

        public HashSet<Log> Log
        {
            get { return log; }
            set { log = value; }
        }
    }
}
