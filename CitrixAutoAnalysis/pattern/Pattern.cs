using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace CitrixAutoAnalysis.pattern
{
   public class Pattern : AbstractNode
    {
        private static string XMLHeader = @"<?xml version='1.0' encoding='utf-8' ?><pattern>";
        private HashSet<Log> log = new HashSet<Log>();
        private string patternName;
        private Graph graph;
        private ProductVersion version;

        public Pattern(Guid id, string name, ProductVersion productVersion) : base(id)
        {
            this.patternName = name;
            this.version = productVersion;
        }

        public static AbstractNode FromXml(string xmlPath) 
        {
            XElement doc = XElement.Load(xmlPath);

            string PatternId = doc.Descendants("id").First().Value;
            string PatternName = doc.Descendants("name").First().Value;
            string ProductName = doc.Descendants("productName").First().Value;
            string ProductVersion = doc.Descendants("productVersion").First().Value;
            string HotfixLevel = doc.Descendants("hotfixLevel").First().Value;

            Pattern ptn = new Pattern(Guid.Parse(PatternId), PatternName, new ProductVersion(ProductName, ProductVersion, HotfixLevel));

            ptn.graph = (Graph)Graph.FromXml(ptn, doc.Descendants("graph").First());//leave the graph node to Graph class to handle

            return ptn;
        }

        public override string ToXml()
        {
            string xmlContent = XMLHeader;
            xmlContent += "<id>" + this.NodeId + "</id>";
            xmlContent += "<name>" + this.patternName + "</name>";

            xmlContent += graph.ToXml();

            xmlContent += "</pattern>";
            return xmlContent;
        }

        public HashSet<string> GetAllModules()
        {
            HashSet<string> modules = new HashSet<string>();

            foreach (Segment seg in graph.Segments)
            {
                foreach (Log log in seg.Log)
                {
                    if (!modules.Contains(log.Module))
                        modules.Add(log.Module);
                }
            }

            return modules;
        }

        public override bool IsMatch(AbstractNode node)
        {
            throw new NotImplementedException();
        }

        public Graph Graph{
            get { return graph; }
            set { graph = value; }
        }

        public void AddLog(Log logNode)
        {
            this.log.Add(logNode);
        }

        public HashSet<Log> Log
        {
            get { return log; }
            set { log = value; }
        }

        public ProductVersion ProductVersion
        {
            get { return version; }
            set { version = value; }
        }

        public string PatternName {
            get { return patternName; }
            set { patternName = value; }
        }
    }
}
