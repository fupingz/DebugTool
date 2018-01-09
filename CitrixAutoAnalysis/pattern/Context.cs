using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CitrixAutoAnalysis.pattern 
{
    public class Context : AbstractNode
    {
        private ContextType conType;
        private string conValue;
        private int paramIndex;
        private string assertion;

        public Context(Guid conId, Log prnt, string conName, string conVal, int ParamIndex, ContextType ConType)
            :   base(conId, prnt, conName, 0)//we don't really care the order of all context data in a log, so initilize it as 0
        {
            this.conValue = conVal;
            this.paramIndex = ParamIndex;
            this.conType = ConType;
        }

        public Context(Guid conId, Log prnt, string conName, string conVal, int ParamIndex, ContextType ConType, string Assert)
            : base(conId, prnt, conName, 0)//we don't really care the order of all context data in a log, so initilize it as 0
        {
            this.conValue = conVal;
            this.paramIndex = ParamIndex;
            this.conType = ConType;
            this.assertion = Assert;
        }

        public string ContextValue
        {
            get { return conValue; }
            set { this.conValue = value; }
        }

        public int ParamIndex
        {
            get { return paramIndex; }
            set { paramIndex = value; }
        }

        public ContextType ContextType
        {
            get { return conType; }
            set { conType = value; }
        }

        public string Assertion
        {
            get { return assertion; }
            set { assertion = value; }
        }

        public override string ToXml() {
            string xmlContent = "<item>";

            xmlContent += "<id>"+this.NodeId+"</id>";
            xmlContent += "<name>" + this.NodeName + "</name>";
            xmlContent += "<log>" + this.Parent.NodeId+ "</log>";
            xmlContent += "<paraIndex>" + this.ParamIndex+ "</paraIndex>";

            xmlContent += "</item>";

            return xmlContent;
        }

        public static AbstractNode FromXml(AbstractNode parent, XElement elem)
        {
            string id = elem.Descendants("id").First().Value;
            string logId = elem.Descendants("logId").First().Value;
            string conName = elem.Descendants("name").First().Value;
            string tmpType = elem.Descendants("type").First().Value;
            string index = elem.Descendants("paraIndex").First().Value;
            string conValue = "";
            
            ContextType type = ContextTypeConverter.StringToContextType(tmpType);
            if(type == ContextType.ContextAssertion || type == ContextType.ContextFilter)
            {
                //we need the value for these 2 kinds
                string assert = tmpType.Split(':')[1];
                return new Context(Guid.Parse(id), (Log)parent,conName, conValue, Convert.ToInt32(index), type, assert);
            }
            
            return new Context(Guid.Parse(id), (Log)parent, conName, conValue, Convert.ToInt32(index), type);
        }

        public override string ConstructSql()
        {
            return "insert into ContextTable values('" + this.NodeId + "','" + this.Parent.NodeId + "','" + this.NodeName + "','" + ContextType.ToString() + "','" + this.ContextValue + "'," + this.ParamIndex + ")";
        }

        public bool Assert()
        { 
            //this works only for assertion
            if (this.ContextType == ContextType.ContextAssertion)
            {
                return string.Equals(ContextValue, Assertion, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
    }

    public enum ContextType{
        Unknown,
        ContextInteger,
        ContextBool,
        ContextString,
        ContextGuid,
        ContextAssertion,// to evaluate if the log instance meet some criteria, e.g. the result of procedure execution is successful.
        ContextFilter    // to filter the log as a filter, e.g. the log that meet this condition is the one we expected.
    }

    public class ContextTypeConverter
    {
        public static string ContextTypeToString(ContextType type)
        {
            switch(type)
            {
                case ContextType.ContextBool:
                    return "Bool";
                case ContextType.ContextInteger:
                    return "Integer";
                case ContextType.ContextString:
                    return "String";
                case ContextType.ContextGuid:
                    return "Guid";
                case ContextType.ContextAssertion:
                    return "Assertion";
                case ContextType.ContextFilter:
                    return "Filter";
            }

            return "Unknown";
        }

        public static ContextType StringToContextType(string type)
        {
            if(type == "Bool")
            {
                return ContextType.ContextBool;
            }
            else if(type == "Integer")
            {
                return ContextType.ContextInteger;
            }
            else if(type == "String")
            {
                return ContextType.ContextString;
            }
            else if (type == "Guid")
            {
                return ContextType.ContextGuid;
            }
            else if (type.StartsWith("Assertion"))
            {
                return ContextType.ContextAssertion;
            }
            else if (type.StartsWith("Filter"))
            {
                return ContextType.ContextFilter;
            }

            return ContextType.Unknown;
        }
    }
}
