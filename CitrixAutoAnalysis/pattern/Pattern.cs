using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

using CitrixAutoAnalysis.analysis.io;

namespace CitrixAutoAnalysis.pattern
{
    public class Pattern : AbstractNode
    {
        private static string XMLHeader = @"<?xml version='1.0' encoding='utf-8' ?><pattern>";
        private ProductVersion version;
        private bool isIssuePattern = false;

        public Pattern(Guid id, string name, ProductVersion productVersion, bool IsIssue) 
                : base(id, null, name, 0)
                // setting the index in parent to 0, since this node is the root
        {
            this.version = productVersion;
            this.isIssuePattern = IsIssue;
            this.root = this;
        }

        public static AbstractNode FromXml(string xmlPath) 
        {

            try {
                XElement doc = XElement.Load(xmlPath);

                string PatternId = doc.Descendants("id").First().Value;
                string PatternName = doc.Descendants("name").First().Value;
                string ProductName = doc.Descendants("productName").First().Value;
                string ProductVersion = doc.Descendants("productVersion").First().Value;
                string HotfixLevel = doc.Descendants("hotfixLevel").First().Value;

                Pattern ptn = new Pattern(Guid.Parse(PatternId), PatternName, new ProductVersion(ProductName, ProductVersion, HotfixLevel), false);

                ptn.AddChildNode((Graph)Graph.FromXml(ptn, doc.Descendants("graph").First()));//leave the graph node to Graph class to handle

                return ptn;
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed to read the pattern(from "+xmlPath+" : "+ex.ToString());
            }

            return null;
        }

        public override string ToXml()
        {
            string xmlContent = XMLHeader;
            xmlContent += "<id>" + this.NodeId + "</id>";
            xmlContent += "<name>" + this.NodeName + "</name>";

            Graph graph = (Graph)this.ChildNodes.First();
            xmlContent += graph.ToXml();

            xmlContent += "</pattern>";
            return xmlContent;
        }

        public override string ConstructSql()
        {
            return "insert into PatternTable values('"+this.NodeId+"','"+this.NodeName+"','"+this.ProductVersion.ProductName+"','"+this.ProductVersion.Version+"','"+this.ProductVersion.HotfixLevel+"',"+(this.IsIssuePattern ? 1:0)+")";
        }

        public HashSet<string> GetAllModules()
        {
            HashSet<string> modules = new HashSet<string>();

            foreach (Log log in this.LogInCurrent())
            {
                if (!modules.Contains(log.Module))
                        modules.Add(log.Module);
            }

            return modules;
        }

        public bool IsMatch(Pattern node)
        {
            Graph graph = (Graph)this.ChildNodes.First();
            
            //check if the info in the pattern node matches here.

            return graph.IsMatch((Graph)node.ChildNodes.First());
        }

        //anyway we calculate a breakpoint for the analyzed issue
        public Log GetBreakPoint()
        {
            Log log = null;
            Segment seg = null;

            if (this.LogInCurrent().Any(l => l.IsBreakPoint))
            {
                return this.LogInCurrent().First(l => l.IsBreakPoint);
            }

            this.SegInCurrent().ForEach(s => {
                                                    if(seg == null || seg.IndexInParent < s.IndexInParent) 
                                                        seg = (Segment)s;
                                                  }
                                           );

            if (seg != null)
            {
                seg.LogInCurrent().ToList().ForEach(l =>{
                                                   if (log == null || log.IndexInParent < l.IndexInParent)
                                                       log = l;
                                             }
                                        );
            }

            return log;
        }

        public Log GetDebugPoint()
        {
            return this.LogInCurrent().Any(l => l.IsForDebug) ? this.LogInCurrent().First(l => l.IsForDebug) : null;
        }

        public ProductVersion ProductVersion
        {
            get { return version; }
            set { version = value; }
        }

        public Boolean IsIssuePattern
        {
            get { return isIssuePattern; }
            set { isIssuePattern = value; }
        }

        public Context GetContextById(Guid guid)
        {
            List<Context> context = this.ContextInCurrent();

            if (context.Any(con => con.NodeId.Equals(guid)))
            { 
                return context.First(con => con.NodeId.Equals(guid));
            }

            return null;
        }

        public Log GetLogById(Guid guid)
        {
            List<Log> log = this.LogInCurrent();

            if(log.Any(l => l.NodeId.Equals(guid)))
            {
                return log.First(l => l.NodeId.Equals(guid));
            }

            return null;
        }

        public Graph Graph
        {
            get { return (Graph)this.ChildNodes.First(); }
        }

        public Segment GetSegById(Guid guid)
        {
            foreach(Graph graph in this.ChildNodes)
            {
                foreach(Segment seg in graph.ChildNodes)
                {
                    if(seg.NodeId.Equals(guid))
                        return seg;
                }
            }

            return null;
        }
    }
}
