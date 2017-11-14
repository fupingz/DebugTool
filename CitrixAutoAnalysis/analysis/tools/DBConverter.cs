using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAutoAnalysis.analysis.tools
{
    class DBConverter
    {
        public static uint UintFromDBItem(Object obj)
        {
            if (obj.Equals(null) || obj.ToString().Length == 0)
            {
                return 0;
            }
            else
            {
                return uint.Parse(obj.ToString());
            }
        }

        public static int IntFromDBItem(Object obj)
        {
            if (obj.Equals(null) || obj.ToString().Length == 0)
            {
                return 0;
            }
            else
            {
                return int.Parse(obj.ToString());
            }
        }

        public static Nullable<DateTime> DatetimeFromDBItem(Object obj)
        {
            if (obj.Equals(null) || obj.ToString().Length == 0)
            {
                return null;
            }
            else
            {
                return DateTime.Parse(obj.ToString());
            }
        }

        public static string StringFromDBItem(Object obj)
        {
            return obj.Equals(null) ? null : obj.ToString();
        }

        public static bool BoolFromDBItem(Object obj)
        {
            return (obj.Equals(null) || obj.ToString().Length == 0) ? false : bool.Parse(obj.ToString());
        }
    }
}
