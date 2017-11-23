using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CitrixAutoAnalysis.pattern.generator
{
    class XMLGenerator
    {
        public void GenerateXML(string CDFPath, string xmlPath)
        {
            StreamReader reader = null;
            Graph pattern = null;
            try
            {
                reader = new StreamReader(CDFPath);

                pattern = processPattern(reader, xmlPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception happened while processing file " + CDFPath + "/n" + ex.ToString());
            }
            finally {
                reader.Close();
            }

            if(pattern != null)
            {
                try
                {
                    File.WriteAllText(xmlPath, pattern.ToXml());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot save the XML content to " + xmlPath + "/n" + ex.ToString());
                }
            }
        }

        public Graph processPattern(StreamReader reader, string xmlFileName)
        {
            ProductVersion version = new ProductVersion("XenApp", "7.6.300", "CU2");
            string line;

            string patternName = reader.ReadLine();//the first line is the name

            Pattern ptn = new Pattern(Guid.NewGuid(), patternName, version, false);
            Graph graph = new Graph(Guid.NewGuid(), ptn, "default graph");

            //2 empty line between each segments to make this work
            for (line = reader.ReadLine(); line != null && line.Length == 0; line = reader.ReadLine())
            {
                Segment segment = processSegment(reader);
                graph.AddChildNode(segment);//generate the xml content for each segment
            }
            return graph;
        }

        //process a single pattern segement
        public Segment processSegment(StreamReader reader)
        {
            string segName = reader.ReadLine(); // the first line is the seg name
            Segment segment = new Segment(Guid.NewGuid(), null, segName, 0);

            ProcessLogItems(segment,reader);

            return segment;
        }

        //process a single line CDF log
        public void ProcessLogItems(Segment segment, StreamReader reader)
        {
            for (string line = reader.ReadLine(); line != null && line.Length != 0; line = reader.ReadLine())
            {
                string[] elements = line.Split('	');//elements ares split via 'tab' key
                
                Log item = new Log(Guid.NewGuid(), null, 
                                           elements[6],                      //module
                                           elements[7],                      //src
                                           elements[7] == "_#dotNet#_" ? "" : elements[9],        //func                   
                                           Convert.ToInt32(elements[8]),     //line
                                           elements[7] == "_#dotNet#_" ? elements[11] : elements[12],                     //text
                                           Convert.ToInt32(elements[5]),     //sessionId
                                           Convert.ToInt32(elements[4]),     //processId
                                           Convert.ToInt32(elements[3]),     //threadId
                                           Convert.ToDateTime(elements[2].Substring(0, elements[2].LastIndexOf(":"))), //capturedTime
                                           0, 0, RelationWithPrevious.Unknown); //index in trace

                segment.AddChildNode(item);
            }
        }
    }
}
