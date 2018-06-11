using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CitrixAutoAnalysis.pattern
{
    public class Segment : AbstractNode
    {
        private Dictionary<Segment, SegmentRelation> connectedSegments = new Dictionary<Segment,SegmentRelation>();

        public Segment(Guid SegId, AbstractNode prnt, string name, int index)
            : base(SegId, prnt, name, index)
        {

        }

        public void AddConnectedSegment(Segment node, SegmentRelation relation)
        {
            this.connectedSegments.Add(node, relation);
        }

        public override string ToXml() {
            string xmlContent = "<segment>";
            xmlContent += "<id>"+this.NodeId+"</id>";
            xmlContent += "<name>" + this.NodeName + "</name>";

            xmlContent += "<log>";
            foreach (Log logItem in this.LogInCurrent())
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
            Segment seg = new Segment(Guid.Parse(segId), parent, segName, index);

            parent.AddChildNode(seg);

            List<XElement> logs = element.Descendants("log").Descendants("id").ToList();
            List<XElement> connSegs = element.Descendants("connectedSegments").Descendants("id").ToList();

            foreach (XElement ele in logs)
            {
                //we are adding a fake log item here, will fill in the info when log item is parsed from xml
                seg.AddChildNode(new Log(Guid.Parse(ele.Value), seg, "","","",0,"",0,0,0,DateTime.Now,index, 0, 0, RelationWithPrevious.Unknown));
            }

            foreach (XElement ele in connSegs)
            {
                //ignoring it for now
                //seg.AddConnectedSegment(parent.GetPatternSegById(Guid.Parse(ele.Value)), SegmentRelation.Unknown);
            }

            return seg;
        }

        public override string ConstructSql()
        {
            //save the pattern itself into patterntable
            return "insert into SegmentTable values('" + this.NodeId + "','" + this.Root.NodeId + "','" + this.NodeName + "'," + this.IndexInParent + ",null)";
        }

        public bool BelongsTo(Graph graph)
        {
            // we don't need 2 duplicate segments in a same pattern
            if (graph.ChildNodes.Any(c => c.IndexInParent == this.IndexInParent))
            {
                return false;
            }

            foreach (Context cs in this.ContextInCurrent())
            {
                foreach (Context cg in graph.ContextInCurrent())
                {
                    if (cs.NodeName.Equals(cg.NodeName))//same name, while different value, seperate the segments into different sequences
                    {
                        if (String.IsNullOrEmpty(cs.ContextValue))
                        {
                            if(!graph.LogInCurrent().Any(l =>l.IsBreakPoint))
                            {
                                ((Log)cs.Parent).IsBreakPoint = true;
                            }
                        }
                        else if (String.IsNullOrEmpty(cg.ContextValue))
                        {
                            ((Log)cg.Parent).IsBreakPoint = true;
                        }
                        else if(!cs.ContextValue.Equals(cg.ContextValue)){
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool IsMatch(Segment node)
        {
            return false;
        }

        public List<AbstractNode> SubSegment
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
