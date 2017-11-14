using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitrixAutoAnalysis.pattern;

using CitrixAutoAnalysis.ParsePatern;

namespace CitrixAutoAnalysis.analysis.engine
{
    class CDFFilter
    {
        private CDFCondition condition;
        private string value;
        private static string ParamMagic = @"*#_PARAM_INDEX_";

        public CDFFilter(CDFCondition con, string val)
        {
            this.condition = con;
            this.value = val;
        }
        public bool IsMatch(Log log) {
            switch (condition)
            { 
                case CDFCondition.CDF_MODULE:
                    return value == log.Module;
                case CDFCondition.CDF_SRC:
                    return value == log.Src;
                case CDFCondition.CDF_FUNC:
                    return value == log.Func;
                case CDFCondition.CDF_LINE:
                    return value == log.Line.ToString();
                case CDFCondition.CDF_SESSIONID:
                    return value == log.SessionId.ToString();
                case CDFCondition.CDF_PROCESSID:
                    return value == log.ProcessId.ToString();
                case CDFCondition.CDF_THREADID:
                    return value == log.ThreadId.ToString();
                case CDFCondition.CDF_TEXT:
                    return MatchCDFText(log.Text);
                case CDFCondition.CDF_FILTER:
                    return MatchFilter(log);
            }

            return false;
        }

        private bool MatchCDFText(string text){
            string tmpText = text;
            string tmpValue = value;
            int index = 1;

            while (tmpValue != null && tmpValue.Length > 0)
            {
                int offset = tmpValue.IndexOf(ParamMagic + index);

                if (offset < 0)
                {
                    if (tmpValue.Length == 0 || tmpText.EndsWith(tmpValue))
                    {
                        return true;//all parameters match.
                    }

                    return false;// comes to the last lirteral part
                }

                string part = tmpValue.Substring(0, offset);

                if (tmpText.IndexOf(part) < 0)
                {
                    return false;//any literal that not matches
                }
                    
                tmpValue = tmpValue.Substring(part.Length + (ParamMagic + index).Length);//have to add the magic because next time we will need the real literal
                tmpText = tmpText.Substring(tmpText.IndexOf(part)+part.Length);//it's fine to keep the parameter value because we just find the existance of literal
                
                index++;
            }

            return true;
        }

        private bool MatchFilter(Log log) 
        {
            string[] filterProperties = this.value.Split(':');

            string filterValue= filterProperties[1];

            if (log.Text.IndexOf(filterValue) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}
