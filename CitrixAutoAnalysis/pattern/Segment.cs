using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CitrixAutoAnalysis.pattern
{
    class Segment : AbstractNode
    {
        private Dictionary<Segment, SegmentRelation> connectedSegments = new Dictionary<Segment,SegmentRelation>();
        private HashSet<Log> log = new HashSet<Log>();
        private Graph parent;
        private string segName;
        private int indexInPattern;

        public Segment(Guid SegId, string SegName, int index, Graph parent)
            : base(SegId)
        {
            this.segName = SegName;
            this.indexInPattern = index;
            this.parent = parent;
        }

        public Segment(string name)
        {
            this.segName = name;
        }

        public void AddConnectedSegment(Segment node, SegmentRelation relation)
        {
            this.connectedSegments.Add(node, relation);
        }

        public void AddLog(Log logNode)
        {
            this.log.Add(logNode);
        }

        public HashSet<Log> Log {
            get { return log; }
            set { log = value; }
        }

        public override string ToXml() {
            string xmlContent = "<segment>";
            xmlContent += "<id>"+this.NodeId+"</id>";
            xmlContent += "<name>" + this.segName + "</name>";

            xmlContent += "<log>";
            foreach (Log logItem in log)
            {
                xmlContent += "<id>"+logItem.NodeId+"</id>";
            }
            xmlContent += "</log><connectedSegments>";
            foreach (KeyValuePair<Segment, SegmentRelation> connected in connectedSegments) {
                xmlContent += "<item><id>" + connected.Key.NodeId + "</id><relation>" + connected.Value.ToString() + "</relation>";
            }
            xmlContent += "</connectedSegments></segment>";
            return xmlContent;
        }

        public static AbstractNode FromXml(XElement element, Graph parent)
        {
            string segId = element.Descendants("id").First().Value;
            string segName = element.Descendants("name").First().Value;
            int index = int.Parse(element.Descendants("index").First().Value);
            Segment seg = new Segment(Guid.Parse(segId), segName, index, parent);

            List<XElement> logs = element.Descendants("log").Descendants("id").ToList();
            List<XElement> connSegs = element.Descendants("connectedSegments").Descendants("id").ToList();

            foreach (XElement ele in logs)
            {
                seg.AddLog(parent.GetPatternLogById(Guid.Parse(ele.Value)));
            }

            foreach (XElement ele in connSegs)
            {
                //ignoring it for now
                //seg.AddConnectedSegment(parent.GetPatternSegById(Guid.Parse(ele.Value)), SegmentRelation.Unknown);
            }

            return seg;
        }

        public bool BelongsTo(Graph graph)
        {
            // we need to analyze the context there, so decide if a segment belongs to the graph
            return true;
        }

        public override bool IsMatch(AbstractNode node)
        {
            return false;
        }

        public string Name {
            get { return segName; }
            set { segName = value; }
        }

        public int IndexInPattern {
            get { return indexInPattern; }
            set { indexInPattern = value; }
        }

        public List<Segment> SubSegment
        {
            get;
            set;
        }
    }

    public enum SegmentRelation{
        Unknown,
        BinaryReference,
        FunctionPointer,
        NamePipe,
        WindowMessage,
    }
}
