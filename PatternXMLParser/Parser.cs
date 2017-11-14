using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PatternXMLParser
{
    class Parser
    {
        public Parser(string file)
        {
            try
            {
                StreamReader reader = new StreamReader(file);

                reader.ReadLine();
            }
            catch (Exception ex) {
                Console.WriteLine("exception happened while processing file "+file+"/n"+ex.ToString());
            }


        }
    }
}
