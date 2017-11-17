#define TRACE  
using System.Diagnostics;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Data.SqlTypes;
namespace DataBaseHelper
{ 
  
   public class NameAndValues
    {
        public string name;
        public string value;
        public NameAndValues(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
        public NameAndValues()
        {
            this.name = null;
            this.value = null;
        }
        public override string ToString()
        {
            return "name is " + name + " value is " + value + "\n";
        }
    }
    /// <summary>
    /// Singleton class to keep only one db connection is created everytime we use the DBHelper.
    /// </summary>
    public class DataBaseHelper2
    {
        //表名
        public const string PATTERNTABLENAME = "PatternTable";
        public const string SEGMENTTABLENAME = "SegmentTable";
        public const string LOGTABLENAME = "LogTable";
        public const string CONTEXTTABLENAME = "ContextTable";
        // dongsheng add 
        public const string CADTABLENAME = "CadJobs";
        public const string CADISSUESTABLENAME = "CadIssues";
        // dongsheng end
      
        // 连接字符串
        //public const string ConnectionString = "Data Source=10.150.143.83;Initial Catalog=cse_auto_debugger;Integrated Security=False;uid=cad_admin;pwd=njlcm@2017";
        public const string ConnectionString = "Data Source=10.150.143.83;Initial Catalog=cse_auto_debugger;Integrated Security=False;MultipleActiveResultSets=True;uid=cad_admin;pwd=njlcm@2017";
        //public const string ConnectionString = "Data Source=DUKE-PC\\SQLEXPRESS;Initial Catalog=ParsePattern;Integrated Security=False;uid=sa;pwd=7ujm*IK<";
        /// <summary>
        /// instance of the helper
        /// </summary>
        private static DataBaseHelper2 _instance = default(DataBaseHelper2);
        /// <summary>
        /// Lock object.
        /// </summary>
        private static readonly object _lock = new object();
        private SqlConnection conn;
        private DataBaseHelper2()
        {
            // 创建SqlConnection实例：
            conn = new SqlConnection(ConnectionString);
        }

        public static DataBaseHelper2 Instance
        {
            get 
            {
                if(_instance == null)
                {
                    lock(_lock)
                    {
                        if (_instance == null)
                            _instance = new DataBaseHelper2();
                    }
                }
                return _instance;
            }
        }
        /// <summary>
        /// release the singleton instance
        /// </summary>
        /// <returns></returns>
        public static void Release()
        {
            lock(_lock)
            {
                if(_instance!=null)
                {
                    _instance = default(DataBaseHelper2);
                }
            }
        }

        /// <summary>
        /// get the db connection status 
        /// </summary>
        /// <returns></returns>
        public Boolean IsDBConnected()
        {
            return conn.State == ConnectionState.Open;
        }
        public Boolean DBOpen()
        {

            try
            {
                if (conn.State != ConnectionState.Open)
                {// 打开数据库连接 n
                    conn.Open();
                }
                // 如果当前连接状态打开，则在控制台上显示输出
                if (conn.State == ConnectionState.Open)
                {
                    Console.Write("connected to Database！" + "\n");
                    Console.Write("connection string is：" + conn.ConnectionString);
                    return true;
                }
            }
            catch
            {
                if (conn.State != ConnectionState.Open)
                {
                    Console.Write("cannot connect to Database！");
                }
                return false;
            }
            return false;
        }

        public void DBClose()
        {
            if (conn.State == ConnectionState.Open)
            {
                Trace.WriteLine("Close the connection to DB!" + "\n");
                Release();
            }
        }
        //need to rewrite

        public void DBAddLine(string table, string sqlData, params NameAndValues[] rowNameandValues)
        {
            SqlDataAdapter myDataAdapter = new SqlDataAdapter("select * from " + table, conn);
            DataSet myDataSet = new DataSet();
            try
            {
                myDataAdapter.Fill(myDataSet, table);
                DataTable myTable = myDataSet.Tables[table];

                // 添加一行
                DataRow myRow = myTable.NewRow();
                foreach (NameAndValues nv in rowNameandValues)
                {
                    if ("ParamIndex" == nv.name || 
                        "SessionID" == nv.name || 
                        "ProcessID" == nv.name || 
                        "ThreadID" == nv.name || 
                        "IndexInSegment" == nv.name || 
                        "LineNum" == nv.name || 
                        "IndexInPattern" == nv.name ||
                        "IsIssued" == nv.name ||
                        "LineNumInTraceFile" == nv.name)
                        myRow[nv.name] = Int32.Parse(nv.value);
                   else if("Time" == nv.name)
                    {
                        //SqlDateTime stime = new SqlDateTime(DateTime.Parse(nv.value));
                        myRow[nv.name] = DateTime.Parse(nv.value);
                    }
                    else
                        myRow[nv.name] = nv.value;
                }
                myTable.Rows.Add(myRow);

                // 将DataSet的修改提交至“数据库”
                SqlCommandBuilder mySqlCommandBuilder = new SqlCommandBuilder(myDataAdapter);
                myDataAdapter.Update(myDataSet, table);
            }
            catch
            {
                //Console.WriteLine("addline failed" + "\n");
                Console.WriteLine(String.Format("Add new line into {0} failed ", table));
                foreach (NameAndValues nv in rowNameandValues)
                    Console.WriteLine(String.Format("The parameters is {0}",nv.ToString()));
                Trace.WriteLine(String.Format("Add new line into {0} failed ",table));
                foreach (NameAndValues nv in rowNameandValues)
                    Trace.WriteLine(String.Format("The parameters is {0}", nv.ToString()));
                myDataSet.Dispose();        // 释放DataSet对象
                myDataAdapter.Dispose();    // 释放SqlDataAdapter对象
                return;
            }
            myDataSet.Dispose();        // 释放DataSet对象
            myDataAdapter.Dispose();    // 释放SqlDataAdapter对象
        }
        //need to rewirte
        public void DBDelteLine(string table,string sqlData)
        {
            SqlDataAdapter myDataAdapter = new SqlDataAdapter("select * from product", conn);
            DataSet myDataSet = new DataSet();
            myDataAdapter.Fill(myDataSet, table);

            // 删除第一行
            DataTable myTable = myDataSet.Tables[table];
            myTable.Rows[0].Delete();

            SqlCommandBuilder mySqlCommandBuilder = new SqlCommandBuilder(myDataAdapter);
            myDataAdapter.Update(myDataSet, table);
            myDataSet.Dispose();        // 释放DataSet对象
            myDataAdapter.Dispose();    // 释放SqlDataAdapter对象

        }
        // get the row id by the node/pattern/segment name
        public List<NameAndValues> getTableItemsbyNameOrId(string table, string name,string Id)
        {
            string cmd="";
            if (name !=null)
                cmd = "select *" + " from " + table + " where Name like \'" + name + "\'";
            else if(Id != null)
                cmd = "select *" + " from " + table + " where ID like \'" + Id + "\'";
            List<NameAndValues> listNV = new List<NameAndValues>();
            //SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            //using (var reader = sqlCmd.ExecuteReader())
            //{

            //    if (reader.Read()) id = (reader.GetString(0));
            //}
            if (cmd == "")
                return listNV;
            SqlDataAdapter myDataAdapter = new SqlDataAdapter(cmd, conn);
            DataSet myDataSet = new DataSet();
            myDataAdapter.Fill(myDataSet, table);

            DataTable myTable = myDataSet.Tables[table];
            foreach (DataRow myRow in myTable.Rows)
            {
                foreach (DataColumn myColumn in myTable.Columns)
                {
                    listNV.Add(new NameAndValues(myColumn.ColumnName, myRow[myColumn].ToString())); //遍历表中的每个单元格
                }
            }
            return listNV;
        }
        public List<DataRow> getTableRowbyNameOrId(string table,string name, string id)
        {
            string cmd = "";
            if (name != null)
                cmd = "select *" + " from " + table + " where Name like \'" + name + "\'";
            else if (id != null)
            {
                if ("PatternTable"==table)
                    cmd = "select *" + " from " + table + " where ID like \'" + id + "\'";
                else if ("LogTable" == table)
                    cmd = "select *" + " from " + table + " where SegmentID like \'" + id + "\'";
                else if ("ContextTable" == table)
                    cmd = "select *" + " from " + table + " where LogID like \'" + id + "\'";
                else
                    cmd = "select *" + " from " + table + " where ParentID like \'" + id + "\'";
            }
            List<DataRow> listNV = new List<DataRow>();
            //SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            //using (var reader = sqlCmd.ExecuteReader())
            //{

            //    if (reader.Read()) id = (reader.GetString(0));
            //}
            if (cmd == "")
                return listNV;
            SqlDataAdapter myDataAdapter = new SqlDataAdapter(cmd, conn);
            DataSet myDataSet = new DataSet();
            myDataAdapter.Fill(myDataSet, table);

            DataTable myTable = myDataSet.Tables[table];
            foreach (DataRow myRow in myTable.Rows)
            {
                    listNV.Add(myRow); //遍历表中的每个单元格
            }
            return listNV;
        }
       public List<string> GetSimilarIssue(int IssueID)
        {
            string cmd = "select *" + " from " + CADISSUESTABLENAME + " where ID = \'" + IssueID + "\'";
            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            string RootCause = "";
            
            using (var reader1 = sqlCmd.ExecuteReader())
            {
//                if (reader.Read()) RootCause = (string)reader["RootCause"];
                if (reader1.Read())
                {
                    RootCause = reader1.GetString(5);
//                    Console.Write("reader.GetString is：" + reader1.GetSqlString(5) + "\n");
                    
                    
                }
            }
            //
            string m_StartMenuEnabled             = "true";
            if (m_StartMenuEnabled == "true")
            {
                Console.Write("m_StartMenuEnabled true\n");
            }
            else
            {
                Console.Write("m_StartMenuEnabled false\n");
            }
            //
            string[] splitRootCauses = RootCause.Split(' ');
            List<string> LCIDs = new List<string>();
//            Console.Write("Rootcause string is：" + RootCause.Length + "\n");
            foreach(var splitRC in splitRootCauses)
            {
                if (splitRC.Length != 0)
                { 
                   Console.Write("splitRC is not null：" + splitRC + "\n");
                   string cmd2 = "select *" + " from " + CADISSUESTABLENAME + " where ID <> \'" + IssueID + "\'" + " and RootCause like \'%" + splitRC + "%\'";
                   SqlCommand sqlCmd2 = new SqlCommand(cmd2, conn);
                   using (var reader2 = sqlCmd2.ExecuteReader())
                   {
                       while (reader2.Read())
                       {
                           LCIDs.Add((string)reader2["LCID"]);
                       }
                   }
                }
            }



            return LCIDs;
        }
                public void GetSimilarIssue2()                                                           
        {
            string cmd           = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed = \'0\'";
            string updatecmd     = "";
            string IntermediateResult = "";
            //string resolution    = "";
            string SimilarLCID   = "";
            int    ID            = 0;
            
            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            using (var reader1 = sqlCmd.ExecuteReader())
            {
                while (reader1.Read())
                {
                    //RootCause = reader1.GetString(5);

                    try
                    {
                        IntermediateResult = (string)reader1["IntermediateResult"];
                        
                    }
                    catch
                    {
                        Console.WriteLine("get IntermediateResult failed" + "\n");
                        continue;
                    }
                    //try 
                    //{
                    //    resolution = (string)reader1["Resolution"];
                    //}
                    //catch
                    //{
                    //    Console.WriteLine("get resolution failed" + "\n");
                    //}

                    ID         = (int)reader1["ID"];
                    //IssuePro = (string)reader1["IssueProcessed"];
                    //JobID = (string)reader1["JobID"];
                    string[] splitIntermediateResult = IntermediateResult.Split(' ');
                    foreach (var splitIR in splitIntermediateResult)
                    {
                        if (splitIR.Length == 0)
                            continue;
                        //Console.Write("splitRC is not null：" + splitRC + "\n" + "RootCause=" + RootCause+ "Resolution="+ resolution);
                        //Console.Write("splitRC is not null：" + splitRC + "\n") ;
                        string cmd2 = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed <> \'0\'" + " and IntermediateResult like \'%" + splitIR + "%\'";
                        SqlCommand sqlCmd2 = new SqlCommand(cmd2, conn);
                        using (var reader2 = sqlCmd2.ExecuteReader())
                        {
                            while (reader2.Read())
                            {
                                SimilarLCID = (string)reader2["LCID"];
                                break;
                            }
                        }
                    }
                    if (0 != SimilarLCID.Length)
                    {
                        string[] splitSimilarLCID = SimilarLCID.Split(' ');
                        foreach (var splitSLCID in splitSimilarLCID)
                        {
                            if (splitSLCID.Length == 0)
                                continue;
                            //updatecmd = "update " + CADISSUESTABLENAME + " set LCID = \'" + splitSLCID + "\'" + " where JobID = \'" + JobID + "\'";
                            updatecmd = "update " + CADISSUESTABLENAME + " set LCID = \'" + splitSLCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                            SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                            updatesqlCmd.ExecuteNonQuery();
                            Console.Write(updatecmd + "\n");
                        }
                    }
                    else
                    {
                        updatecmd = "update " + CADISSUESTABLENAME + " set IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write(updatecmd + "\n");

                    }

                }
            }
        }
        public void pTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.Write("timer elapsed \n");
            try
            {
                GetSimilarIssue2();
            }
            catch 
            {
                Console.WriteLine("get issue failed" + "\n");
            }
            
            return;
        }
        public void StartTimerThread()
        {
            //GetSimilarIssue2();
            System.Timers.Timer pTimer = new System.Timers.Timer(5000);
            pTimer.Elapsed += pTimer_Elapsed;
            pTimer.AutoReset = true;
            pTimer.Enabled = true;
 
            return;
        }

    }
    }

