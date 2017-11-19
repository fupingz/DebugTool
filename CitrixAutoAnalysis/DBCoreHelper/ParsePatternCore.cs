#define TRACE  
using System.Diagnostics;
using System;
using System.Collections.Generic;
using DataBaseHelper;
using CitrixAutoAnalysis.pattern;
using System.Data;
namespace CitrixAutoAnalysis.ParsePatern
{
    public class ParsePatternCore
    {
        //initilize the connection to DB
        private DataBaseHelper2 dbHelper = default(DataBaseHelper2);
        
        /// <summary>
        /// static class constructor to initialize all the static members to 
        /// make sure all the static members are initialized before being used.
        /// </summary>
        public ParsePatternCore()
        {
            dbHelper = DataBaseHelper2.Instance;
        }


        public void ParsePattern(Object pattern, bool isIssued = false)
        {
            Trace.WriteLine("ParsePattern start to tracing...");
            if(!dbHelper.DBOpen())
            {
                Trace.TraceError("cannot connect to Database！");
                return;
            }
            
            // begin  to parse pattern
            Pattern patternc = (Pattern)pattern;
            Trace.WriteLine("begin to write pattern into the DB");
            writePatternIntoDB(patternc, patternc.PatternName, isIssued);
            List<Segment> listSeg = patternc.Graph.Segments;
            List<List<Log>> nodeList = new List<List<Log>>();

            Trace.WriteLine("begin to write the segment into DB");
            ProcessSegList(listSeg, patternc.Graph.NodeId.ToString());
            dbHelper.DBClose();

        }
        private void ProcessSegList(List<Segment> segList, string parentId)
        {
            //退出条件
            if (segList.Count <= 0)
            {
                Trace.WriteLine("no more segment any more...");
                return;
            }
            int indexer = 1;// the order that this segment in the children segments.
            foreach (Segment seg in segList)
            {
                //递归的处理Segment的孩子
                Trace.WriteLine(String.Format("begin to process sub segment for this segment recursivly. and the segment id is {0}", seg.NodeId.ToString()));
                ProcessSegList(seg.SubSegment, seg.NodeId.ToString());
                //处理完成后把自己存入数据库
                Trace.WriteLine("wite the segment {0} into DB",seg.NodeId.ToString());
                writeSegIntoDB(seg, parentId, indexer++);
                // get and store all the node for segment
                int order = 1; // the order that this node in the children nodes.
                foreach (Log node in seg.Log)
                {
                    //store the log into database
                    Trace.WriteLine(String.Format("store the logs into db, and the log order is {0}", order));
                    writeLogIntoDB(node, seg.NodeId.ToString(), node.IndexInSeg);
                    if(node.PatternContext != null)
                    {
                        foreach (Context cont in node.PatternContext)
                        {
                            writeContextIntoDB(cont, node.NodeId.ToString());
                        }
                    }
                }
            }
        }
        // write the pattern into data base.
        private void writePatternIntoDB(Pattern pattern, string parent,bool isIssued)
        {
            dbHelper.DBAddLine(DataBaseHelper.DataBaseHelper2.PATTERNTABLENAME,null, 
                new NameAndValues("ID",(pattern.Graph.NodeId).ToString()),
                new NameAndValues("Name",pattern.PatternName),
                new NameAndValues("ProductName", (pattern.ProductVersion).ProductName),
                new NameAndValues("Version", pattern.ProductVersion.Version),
                new NameAndValues("HotfixLevel", pattern.ProductVersion.HotfixLevel),
                new NameAndValues("IsIssued",isIssued?"1":"0"));
                

        }
        // write the Segment into DB
        private void writeSegIntoDB(Segment seg, string parentId, int order)
        {
            //int parentId = dbHelper.getIdbyName(DataBaseHelper2.DataBaseHelper2.PATTERNTABLENAME, parent);
            //if(parentId == -1)
            //{
            //    Console.WriteLine("no parent id found" + "\n");
            //    return;
            //}
            //construct new line to add to the table.
            dbHelper.DBAddLine(DataBaseHelper.DataBaseHelper2.SEGMENTTABLENAME, null, 
                new NameAndValues("ID", seg.NodeId.ToString()), 
                new NameAndValues("ParentID", parentId),
                new NameAndValues("Name",seg.Name),
                new NameAndValues("IndexInPattern",order.ToString()),
                new NameAndValues("Collected", null));

        }
        //write the LogNode into DB
        private void writeLogIntoDB(Log log, string parentId,int order)
        {
            //int parentId = dbHelper.getIdbyName(DataBaseHelper2.DataBaseHelper2.SEGMENTTABLENAME, parent);
            //if (parentId == -1)
            //{
            //    Console.WriteLine("no parent id found" + "\n");
            //    return;
            //}
            //construct new line to add to the table.
            dbHelper.DBAddLine(DataBaseHelper.DataBaseHelper2.LOGTABLENAME, null, 
                new NameAndValues("ID", log.NodeId.ToString()),
                new NameAndValues("SegmentID", parentId),
                new NameAndValues("IndexInSegment",order.ToString()),
                new NameAndValues("Time",log.CapturedTime.ToString("yyyy-MM-dd HH:mm:ss")),// datatime store to DB, need to convert the format
                new NameAndValues("Source",log.Src),
                new NameAndValues("FunctionName",log.Func),
                new NameAndValues("LineNum",log.Line.ToString()),
                new NameAndValues("Module",log.Module),
                new NameAndValues("SessionID",log.SessionId.ToString()),
                new NameAndValues("ProcessID", log.ProcessId.ToString()),
                new NameAndValues("ThreadID", log.ThreadId.ToString()),
                new NameAndValues("RelationWithPrevious", null),// need to refine this
                new NameAndValues("Text", log.Text)
                //new NameAndValues("SessionID", log.SessionId.ToString()),
               );

        }
        private void writeContextIntoDB(Context cont, string parentId)
        {
            //int parentId = dbHelper.getIdbyName(DataBaseHelper2.DataBaseHelper2.PATTERNTABLENAME, parent);
            //if(parentId == -1)
            //{
            //    Console.WriteLine("no parent id found" + "\n");
            //    return;
            //}
            //construct new line to add to the table.
            dbHelper.DBAddLine(DataBaseHelper.DataBaseHelper2.CONTEXTTABLENAME, null,
                new NameAndValues("ID", cont.Id.ToString()),
                new NameAndValues("LogID", parentId),
                new NameAndValues("Name", cont.Name),
                new NameAndValues("Type", ContextTypeConverter.ContextTypeToString(cont.ContextType)),
                new NameAndValues("Value", cont.Value),
                new NameAndValues("ParamIndex",cont.Index.ToString()));

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Pattern ParsePatternFromDB(string name, string id =null)
        {
            if (!dbHelper.DBOpen())
            {
                Trace.TraceError("cannot connect to Database！");
                return null;
            }
            //PatternGraph pG = new PatternGraph(null,null);
            Pattern pattern = new Pattern(Guid.Empty, null, null);
            getPatternParams(pattern, name, id);
            List<Segment> segs = getSegmentFromDB(pattern.Graph.NodeId.ToString());
            pattern.Graph.Segments = segs;
            return pattern;
        }
        private void getPatternParams(Pattern pg, string name,string id =null)
        {
            List<NameAndValues> listNv = dbHelper.getTableItemsbyNameOrId(DataBaseHelper.DataBaseHelper2.PATTERNTABLENAME,name,id);
            ProductVersion pv = new ProductVersion(null, null, null);
            foreach(NameAndValues nv in listNv)
            {
                switch(nv.name)
                {
                   case "ID":
                                pg.NodeId = new Guid(nv.value);
                                pg.Graph.NodeId = new Guid(nv.value);
                                break;
                   case "Name" :
                        pg.PatternName= nv.value;
                                break;
                   case "Version":
                        //pg.SetVersion(nv.value);
                        pv.Version = nv.value;
                                break;
                   case "ProductName":
                        pv.ProductName = nv.value;
                        break;
                   case "HotfixLevel":
                        pv.HotfixLevel = nv.value;
                                break;
                }
            }
            pg.ProductVersion= pv;
        }
        private List<Segment> getSegmentFromDB(string parentId)
        {
            List<Segment> listSeg = new List<Segment>();
            //在数据库中查找所有指向parentId的行，并根据ID创建segment
            List<DataRow> listNv = dbHelper.getTableRowbyNameOrId(DataBaseHelper.DataBaseHelper2.SEGMENTTABLENAME,null, parentId);
            //遍历所有的节点，如果ID相同，则为一个新的segment
            foreach(DataRow row in listNv)
            {
                Segment segNode = new Segment(null);
                if (row["ID"] != DBNull.Value)
                    segNode.NodeId = new Guid(row["ID"].ToString());
                if (row["Name"] != DBNull.Value)
                    segNode.Name = row["Name"].ToString();
                //if (row["ParentID"] != DBNull.Value)
                //    segNode.NodeId = new Guid(row["ID"].ToString());
                //if (row["ID"] != DBNull.Value)
                //    segNode.NodeId = new Guid(row["ID"].ToString());
                //if (row["ID"] != DBNull.Value)
                //    segNode.NodeId = new Guid(row["ID"].ToString());
                //递归的访问所有的subsegment
                List<Segment> segs = getSegmentFromDB(segNode.NodeId.ToString());
                HashSet<Log> logs = getLogsFromDB(segNode.NodeId.ToString());
                segNode.SubSegment = segs;
                segNode.Log = logs;
                listSeg.Add(segNode);
            }

            return listSeg;
        }
        private HashSet<Log> getLogsFromDB(string segId)
        {
            HashSet<Log> listNodes = new HashSet<Log>();
            //在数据库中查找所有指向parentId的行，并根据ID创建Node
            List<DataRow> listDr = dbHelper.getTableRowbyNameOrId(DataBaseHelper.DataBaseHelper2.LOGTABLENAME, null, segId);
            foreach(DataRow dr in listDr)
            {
                Log node = new Log();
                node.NodeId = new Guid(dr["ID"].ToString());
                node.CapturedTime = (DateTime)(dr["Time"]);
                node.Src = dr["Source"].ToString();
                node.Func = dr["FunctionName"].ToString();
                node.Line = Int32.Parse(dr["LineNum"].ToString());
                node.SessionId = Int32.Parse(dr["SessionID"].ToString());
                node.ProcessId = Int32.Parse(dr["ProcessID"].ToString());
                node.ThreadId = Int32.Parse(dr["ThreadID"].ToString());
                node.Text = dr["Text"].ToString();
                node.IndexInSeg = Int32.Parse(dr["IndexInSegment"].ToString());
                //node.RelationWithPrevious = dr["RelationWithPrevious"];
                List<Context> conts = getContextFromDB(node.NodeId.ToString());
                node.PatternContext = conts;
                listNodes.Add(node);



            }
            return listNodes;
        }
        private List<Context> getContextFromDB(string nodeId)
        {
            List<Context> conts = new List<Context>();

            //在数据库中查找所有指向parentId的行，并根据ID创建Node
            List<DataRow> listDr = dbHelper.getTableRowbyNameOrId(DataBaseHelper.DataBaseHelper2.CONTEXTTABLENAME, null, nodeId);
            foreach (DataRow dr in listDr)
            {
                Context cnt = new Context();
                cnt.Id = new Guid(dr["ID"].ToString());
                cnt.Name = dr["Name"].ToString();
                cnt.ContextType = ContextTypeConverter.StringToContextType(dr["Type"].ToString());
                cnt.Value = dr["Value"].ToString();
                cnt.Index = Int32.Parse(dr["ParamIndex"].ToString());

                conts.Add(cnt);

            }

            return conts;
        }
    }
}
