using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CitrixAutoAnalysis.pattern
{
    public class Context
    {
        private string name;
        private int index; // the index of the context from the parameter list
        private ContextType type;
        private string value;
        private Guid id;
        private Guid logId;


        public Context() { }

        public Context(Guid conId, string conName, string conVal, int ParamIndex, Guid LogId, ContextType ConType)
        {
            this.id = conId;
            this.name = conName;
            this.value = conVal;
            this.type = ConType;
            this.logId = LogId;
            this.index = ParamIndex;
        }

        public Context(Guid conId, string conName, int ParamIndex, Guid LogId, ContextType ConType)
        {
            this.id = conId;
            this.name = conName;
            this.type = ConType;
            this.logId = LogId;
            this.index = ParamIndex;
        }

        public Guid Id {
            get { return this.id; }
            set { id = value; }
        }

        public int Index {
            get { return this.index; }
            set { index = value; }
        }

        public string Name 
        {
            get { return name; }
            set { name = value; }
        }

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public int ParamIndex
        {
            get { return index; }
            set { index = value;}
        }

        public ContextType ContextType
        {
            get { return type; }
            set { type = value; }
        }

        public string ToXml() {
            string xmlContent = "<item>";

            xmlContent += "<id>"+this.Id+"</id>";
            xmlContent += "<name>" + this.Id + "</name>";
            xmlContent += "<log>" + this.logId+ "</log>";
            xmlContent += "<paraIndex>" + this.index+ "</paraIndex>";

            xmlContent += "</item>";

            return xmlContent;
        }

        public static Context FromXml(XElement context)
        {
            string id = context.Descendants("id").First().Value;
            string logId = context.Descendants("logId").First().Value;
            string conName = context.Descendants("name").First().Value;
            string tmpType = context.Descendants("type").First().Value;
            string index = context.Descendants("paraIndex").First().Value;
            string conValue = "";
            
            ContextType type = ContextTypeConverter.StringToContextType(tmpType);
            if(type == ContextType.ContextAssertion || type == ContextType.ContextFilter)
            {
                //we need the value for these 2 kinds
                conValue = tmpType;
            }
            
            return new Context(Guid.Parse(id), conName, conValue, Convert.ToInt32(index), Guid.Parse(logId), type);
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
