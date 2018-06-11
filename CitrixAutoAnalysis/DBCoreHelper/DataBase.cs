#define TRACE
using System.Diagnostics;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Data.SqlTypes;

using System.Web.Script.Serialization;
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
	    public class SupportCase
    {
	    public String caseID;
        public String title;
        public String description;
        public String serviceProduct;
        public String productVersion;
        public String component;
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
        public const string CADONEFIXCASES = "CadOnefixCases";
        public const string JOBPATTERN = "JobPattern";
        public const string PATTERNKEYWORDS = "PatternKeywords";
        System.Timers.Timer pTimer = null;
        static int iCount = 0;
        // dongsheng end

        // 连接字符串
        //public const string ConnectionString = "Data Source=10.150.143.83;Initial Catalog=cse_auto_debugger;Integrated Security=False;uid=cad_admin;pwd=njlcm@2017";
        public const string ConnectionString = "Data Source=10.150.154.25;Initial Catalog=cse_auto_debugger;Integrated Security=False;MultipleActiveResultSets=True;uid=cad_admin;pwd=njlcm@2017";
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
                if (_instance == null)
                {
                    lock (_lock)
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
            lock (_lock)
            {
                if (_instance != null)
                {
                    _instance = default(DataBaseHelper2);
                }
            }
        }
        public Boolean DBOpen()
        {

            try
            {
                // 打开数据库连接 n
                if (conn.State == ConnectionState.Open)
                {
                    Console.WriteLine("DB connected already");
                    return true;
                }

                conn.Open();
                // 如果当前连接状态打开，则在控制台上显示输出
                if (conn.State == ConnectionState.Open)
                {
                    Console.Write("connected to Database！" + "\n");
                    //Console.Write("connection string is：" + conn.ConnectionString);
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
            // try
            // {
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
                else if ("Time" == nv.name)
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

            // }
            //catch
            //{
            //    //Console.WriteLine("addline failed" + "\n");
            //    Console.WriteLine(String.Format("Add new line into {0} failed ", table));
            //    foreach (NameAndValues nv in rowNameandValues)
            //        Console.WriteLine(String.Format("The parameters is {0}",nv.ToString()));
            //    Trace.WriteLine(String.Format("Add new line into {0} failed ",table));
            //    foreach (NameAndValues nv in rowNameandValues)
            //        Trace.WriteLine(String.Format("The parameters is {0}", nv.ToString()));
            //    myDataSet.Dispose();        // 释放DataSet对象
            //    myDataAdapter.Dispose();    // 释放SqlDataAdapter对象
            //    return;
            //}
            myDataSet.Dispose();        // 释放DataSet对象
            myDataAdapter.Dispose();    // 释放SqlDataAdapter对象
        }
        //need to rewirte
        public void DBDelteLine(string table, string sqlData)
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
        public List<NameAndValues> getTableItemsbyNameOrId(string table, string name, string Id)
        {
            string cmd = "";
            if (name != null)
                cmd = "select *" + " from " + table + " where Name like \'" + name + "\'";
            else if (Id != null)
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
        public List<DataRow> getTableRowbyNameOrId(string table, string name, string id)
        {
            string cmd = "";
            if (name != null)
                cmd = "select *" + " from " + table + " where Name like \'" + name + "\'";
            else if (id != null)
            {
                if ("PatternTable" == table)
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
        public void pTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            Console.Write("Timer Elapsed {0}\n", iCount);

//            if (iCount++ > 5)
//                StopTimerThread();
            try
            {
                GetSimilarCaseFromElasticSearch();
            }
            catch
            {
                Console.WriteLine("Get Issue Failed" + "\n");
            }

            return;
        }
        public void StartTimerThread()
        {
           //       GetSimilarCaseFromElasticSearch();
            pTimer = new System.Timers.Timer(20000);
            pTimer.Elapsed += pTimer_Elapsed;
            pTimer.AutoReset = true;
            pTimer.Enabled = true;

            return;
        }
        public void StopTimerThread()
        {
            pTimer.Stop();
            pTimer.Dispose();
            Console.Write("Timer Stopped \n");
            return;
        }


        public int GetTitleAndComponent(string LCID, out string IssueTitle, out string IssueComponent,out string IssueDescription,out string IssueServiceProduct,out string IssueProductVersion)
        {
            int ret = 1;
            string Component = "";
            string Title = "";
            string Description = "";
            string ServiceProduct = "";
            string ProductVersion = "";
            string cmd = "select *" + " from " + CADONEFIXCASES + " where " + " CaseID = \'" + LCID + "\'";

            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            using (var reader = sqlCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    try
                    {
                        Title = (string)reader["Title"];
                        Console.WriteLine("Title     : " + Title);
                    }
                    catch
                    {
                        Console.WriteLine("Get Case " + LCID + "Title failed" + "\n");ret = 0;
                    }

                    try
                    {
                        Component = (string)reader["Component"];
                        Console.WriteLine("Component : " + Component);
                    }
                    catch
                    {
                        Console.WriteLine("Get Case " + LCID + "Component failed" + "\n");ret = 0;
                    }
                    try
                    {
                        Description = (string)reader["Description"];
          //              Console.WriteLine("Description : " + Description);
                    }
                    catch
                    {
                        Console.WriteLine("Get Case " + LCID + "description failed" + "\n");ret = 0;
                    }
                    try
                    {
                        ServiceProduct = (string)reader["ServiceProduct"];
                        Console.WriteLine("ServiceProduct : " + ServiceProduct);
                    }
                    catch
                    {
                        Console.WriteLine("Get Case " + LCID + "ServiceProduct failed" + "\n");ret = 0;
                    }
                    try
                    {
                        ProductVersion = (string)reader["ProductVersion"];
                        Console.WriteLine("ProductVersion : " + ProductVersion);
                    }
                    catch
                    {
                        Console.WriteLine("Get Case " + LCID + "ProductVersion failed" + "\n");ret = 0;
                    }
                }
            }
            IssueTitle = Title;
            IssueComponent = Component;
            IssueDescription= Description;
            IssueServiceProduct=ServiceProduct;
            IssueProductVersion=ProductVersion;
            return ret;
        }

        public void GetSimilarCaseFromElasticSearch()
        {
            string cmd = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed = \'0\'";
            string updatecmd = "";
            string IntermediateResult = "";
            int ID = 0;
            string LCID = "";
      
            string IssueTitle = "";
            string IssueComponent = "";
            string IssueDescription = "";
            string IssueServiceProduct = "";
            string IssueProductVersion = "";                    

            string SimilarLCs = "";
            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            int count = 0;

            using (var reader1 = sqlCmd.ExecuteReader())
            {
                while (reader1.Read())
                {

                    //RootCause = reader1.GetString(5);
                    ID = (int)reader1["ID"];

                    Console.WriteLine("ID        : " + ID);

                    SimilarLCs = "";
                    try
                    {
                        LCID = (string)reader1["LCID"];
                        Console.WriteLine("LCID      : " + LCID);
//                        Console.WriteLine("");
                    }
                    catch
                    {
                        Console.WriteLine("Get LCID Failed" + "\n");
                        continue;
                    }

                    LCID = LCID.Trim();
                    if (LCID.Length  != 0)
                    {
                        if (1 == GetTitleAndComponent(LCID, out IssueTitle, out IssueComponent, out IssueDescription,out IssueServiceProduct,out IssueProductVersion))
                        {
                            SimilarLCs = GetSimilarCaseFromElasticSearchInternal(LCID, IssueTitle, IssueComponent,IssueDescription,IssueServiceProduct,IssueProductVersion);
                            
                        }
                    }
                    if (0 != SimilarLCs.Length)
                    {
                        updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + SimilarLCs + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write("SQL Cmd   : " + updatecmd + "\n");
                        Console.WriteLine("");Console.WriteLine("");Console.WriteLine("");
                    }
                    else
                    {
//                        updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + LCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        updatecmd = "update " + CADISSUESTABLENAME + " set IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write("SQL Cmd   : " + updatecmd + "\n");Console.WriteLine("");Console.WriteLine("");Console.WriteLine("");

                    }
                    count++;
                    if (count > 15)
                    {
                        Console.WriteLine("Total = " + count);Console.WriteLine("");Console.WriteLine("");Console.WriteLine("");
                        return;
                    }

                }
            }
            Console.WriteLine("Total = " + count);Console.WriteLine("");Console.WriteLine("");Console.WriteLine("");
        }


        public string GetSimilarCaseFromElasticSearchInternal(string LCID, string IssueTitle, string IssueComponent,string IssueDescription,string IssueServiceProduct,string IssueProductVersion)
        {
        //    string strURL = "http://localhost:8080/cases/search";
            string strURL = "http://10.150.154.25:8080/cases/search";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 80000;
            string postData = "";

            SupportCase sCase= new SupportCase();
            sCase.caseID = LCID;
            sCase.title = IssueTitle;
            sCase.component = IssueComponent;
            sCase.description = IssueDescription;
            sCase.serviceProduct = IssueServiceProduct;
            sCase.productVersion = IssueProductVersion;

            postData = new JavaScriptSerializer().Serialize(sCase);
            byte[] data = Encoding.UTF8.GetBytes(postData);
            request.ContentLength=data.Length;
            Stream writer;
            try
            {
                writer = request.GetRequestStream();
            }
            catch (Exception)
            {
                writer = null;
                Console.Write("连接服务器失败!");
            }
        
            writer.Write(data, 0, data.Length);
            writer.Close();

            String strValue = "";
            string result = "";
            HttpWebResponse response;
        
            try
            {
                //获得响应流
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
            }

            Stream stream1 = response.GetResponseStream();

            using (StreamReader reader = new StreamReader(stream1, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            Console.Write("result ={0}\n", result);
            return result;
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
            string m_StartMenuEnabled = "true";
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
            foreach (var splitRC in splitRootCauses)
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
            string cmd = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed = \'0\'";
            string updatecmd = "";
            string IntermediateResult = "";
            //string resolution    = "";
            string SimilarLCID = "";
            int ID = 0;

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

                    ID = (int)reader1["ID"];
                    //IssuePro = (string)reader1["IssueProcessed"];
                    //JobID = (string)reader1["JobID"];
                    string[] splitIntermediateResult = IntermediateResult.Split(' ');
                    foreach (var splitIR in splitIntermediateResult)
                    {
                        if (splitIR.Length == 0)
                            continue;
                        string[] splitWithSharpResult = splitIR.Split('#');
                        foreach (var splitWSR in splitWithSharpResult)
                        {
                            if (splitWSR.Length == 0)
                                continue;

                            //Console.Write("splitRC is not null：" + splitRC + "\n" + "RootCause=" + RootCause+ "Resolution="+ resolution);
                            //Console.Write("splitRC is not null：" + splitRC + "\n") ;
                            string cmd2 = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed <> \'0\'" + " and IntermediateResult like \'%" + splitWSR + "%\'";
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
        public string GetSimilarIssueInternal()
        {
            string IssueTitle = "unable to launch Apps ";
            string SimilarLCID = "";
            string SimilarLCs = "";
            string cmd2 = "select *" + " from " + CADONEFIXCASES + " where " + " Title like \'%" + IssueTitle + "%\'";
            SqlCommand sqlCmd2 = new SqlCommand(cmd2, conn);
            using (var reader2 = sqlCmd2.ExecuteReader())
            {
                while (reader2.Read())
                {
                    SimilarLCID = (string)reader2["CaseID"];

                    if (0 != SimilarLCID.Length)
                    {
                        string[] splitSimilarLCID = SimilarLCID.Split(' ');
                        foreach (var splitSLCID in splitSimilarLCID)
                        {
                            if (splitSLCID.Length == 0)
                                continue;

                            SimilarLCs = SimilarLCs + splitSLCID + ",";
                            //      Console.Write(updatecmd + "\n");
                            if (SimilarLCs.Length >= 900)
                                break;

                        }
                    }

                }
            }
            return SimilarLCs;
        }
        public void GetSimilarIssue3()
        {
            string cmd = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed = \'0\'";
            string updatecmd = "";
            string IntermediateResult = "";
            //string resolution    = "";
            int ID = 0;
            string LCID = "";
            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            int count = 0;



            using (var reader1 = sqlCmd.ExecuteReader())
            {
                while (reader1.Read())
                {

                    //RootCause = reader1.GetString(5);

                    try
                    {
                        LCID = (string)reader1["LCID"];
                        Console.WriteLine("-----" + LCID);
                        Console.WriteLine("");
                    }
                    catch
                    {
                        Console.WriteLine("get IntermediateResult failed" + "\n");
                        continue;
                    }

                    ID = (int)reader1["ID"];

                    Console.Write(ID + "\n");
                    if (ID == 95)
                    {
                        Console.Write(ID + "\n");
                    }

                    string SimilarLCs = GetSimilarIssueInternal();
                    if (0 != SimilarLCs.Length)
                    {
                        string[] splitSimilarLCID = SimilarLCs.Split(' ');
                        foreach (var splitSLCID in splitSimilarLCID)
                        {
                            if (splitSLCID.Length == 0)
                                continue;
                            //updatecmd = "update " + CADISSUESTABLENAME + " set LCID = \'" + splitSLCID + "\'" + " where JobID = \'" + JobID + "\'";
                            updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + splitSLCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                            SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                            updatesqlCmd.ExecuteNonQuery();
                            Console.Write(updatecmd + "\n");
                        }
                    }
                    else
                    {
                        //                        updatecmd = "update " + CADISSUESTABLENAME + " set IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + LCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write(updatecmd + "\n");

                    }
                    count++;
                    if (count > 15)
                        return;
                    Console.WriteLine("total = " + count);

                }
            }
        }
        public List<string> GetKeywords(int JobID)
        {
            string PatternId = "";
            string OutPatternId = "";
            List<string> KeywordsList = new List<string>();
            string PatternKeyword = "";
            string cmd = "select *" + " from " + JOBPATTERN + " where " + " JobId = \'" + JobID + "\'";
            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            using (var reader = sqlCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    PatternId = (string)reader["PatternId"];

                    if (0 != PatternId.Length)
                    {
                        string[] splitPatternId = PatternId.Split(' ');
                        foreach (var splitPID in splitPatternId)
                        {
                            if (splitPID.Length == 0)
                                continue;

                            Console.WriteLine(splitPID);
                            OutPatternId = splitPID;
                        }
                    }

                }
            }
            string cmd2 = "select *" + " from " + PATTERNKEYWORDS + " where " + " ID = \'" + OutPatternId + "\'";
            SqlCommand sqlCmd2 = new SqlCommand(cmd2, conn);
            using (var reader2 = sqlCmd2.ExecuteReader())
            {
                while (reader2.Read())
                {
                    PatternKeyword = (string)reader2["Keyword"];

                    if (0 != PatternKeyword.Length)
                    {
                        string[] splitPatternKeyword = PatternKeyword.Split(' ');
                        foreach (var splitPKD in splitPatternKeyword)
                        {
                            if (splitPKD.Length == 0)
                                continue;
                            KeywordsList.Add(splitPKD);
                            Console.WriteLine(splitPKD);
                        }
                    }

                }
            }
            return KeywordsList;
        }
        public string GetSimilarIssueFromOneFixInternal(int JobID)
        {
            string SimilarLCID = "";
            string SimilarLCs = "";
            List<string> KeywordsList = new List<string>();


            KeywordsList = GetKeywords(JobID);
            foreach (var keyw in KeywordsList)
            {
                string cmd2 = "select *" + " from " + CADONEFIXCASES + " where " + " Title like \'%" + keyw + "%\'";
                SqlCommand sqlCmd2 = new SqlCommand(cmd2, conn);
                using (var reader2 = sqlCmd2.ExecuteReader())
                {
                    while (reader2.Read())
                    {
                        SimilarLCID = (string)reader2["CaseID"];

                        if (0 != SimilarLCID.Length)
                        {
                            string[] splitSimilarLCID = SimilarLCID.Split(' ');
                            foreach (var splitSLCID in splitSimilarLCID)
                            {
                                if (splitSLCID.Length == 0)
                                    continue;

                                SimilarLCs = SimilarLCs + splitSLCID + ",";
                                //      Console.Write(updatecmd + "\n");
                                if (SimilarLCs.Length >= 22)
                                    return SimilarLCs;

                            }
                        }

                    }
                }
            }

            return SimilarLCs;
        }
        public void GetSimilarIssueFromOneFix()
        {
            string cmd = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed = \'0\'";
            string updatecmd = "";
            string IntermediateResult = "";
            //string resolution    = "";
            int ID = 0;
            int JOBID = 0;
            string LCID = "";

            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            int count = 0;



            using (var reader1 = sqlCmd.ExecuteReader())
            {
                while (reader1.Read())
                {

                    //RootCause = reader1.GetString(5);

                    try
                    {
                        LCID = (string)reader1["LCID"];
                        Console.WriteLine("-----" + LCID);
                        Console.WriteLine("");


                    }
                    catch
                    {
                        Console.WriteLine("get IntermediateResult failed" + "\n");
                        continue;
                    }

                    ID = (int)reader1["ID"];

                    Console.WriteLine("ID:" + ID);
                    if (ID == 95)
                    {
                        Console.WriteLine(ID);
                    }
                    try
                    {
                        JOBID = (int)reader1["JobID"];
                        Console.WriteLine("JobID:" + JOBID);
                        Console.WriteLine("");
                    }
                    catch
                    {
                        Console.WriteLine("get JobID failed" + "\n");
                        continue;
                    }

                    string SimilarLCs = GetSimilarIssueFromOneFixInternal(JOBID);
                    if (0 != SimilarLCs.Length)
                    {
                        string[] splitSimilarLCID = SimilarLCs.Split(' ');
                        foreach (var splitSLCID in splitSimilarLCID)
                        {
                            if (splitSLCID.Length == 0)
                                continue;
                            //updatecmd = "update " + CADISSUESTABLENAME + " set LCID = \'" + splitSLCID + "\'" + " where JobID = \'" + JobID + "\'";
                            updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + splitSLCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                            SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                            updatesqlCmd.ExecuteNonQuery();
                            Console.Write("SQL Cmd:" + updatecmd + "\n");
                        }
                    }
                    else
                    {
                        //                        updatecmd = "update " + CADISSUESTABLENAME + " set IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + LCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write("SQL Cmd:" + updatecmd + "\n");

                    }
                    count++;
                    if (count > 15)
                        return;
                    Console.WriteLine("total = " + count);

                }
            }
        }

        public string GetSimilarIssue5Internal(string LCID, string Title, string Component)
        {
            //            string[] IssueTitle = "unable to launch Apps ";
            string SimilarLCID = "";
            string SimilarLCs = "";
            string cmd = "";

            int len = 0, index = -1;
            int i = 0, j = 0;
            int[] tempi = new int[] { -1, -1, -1, -1 };
            int count = 0;

            string[] IssueTitle = Title.Split(' ');
            while (j < 4)
            {
                len = 0;
                index = -1;
                for (i = 0; i < IssueTitle.Length; i++)
                {
                    if ((i == tempi[0]) || (i == tempi[1]) || (i == tempi[2]))
                        continue;
                    if (IssueTitle[i].Length > len)
                    {
                        len = IssueTitle[i].Length;
                        index = i;
                    }

                }
                tempi[count++] = index;
                j++;
                cmd = "select *" + " from " + CADONEFIXCASES + " where " + " Title like \'%" + IssueTitle[index] + "%\'" + "And Component like \'%" + Component + "%\'" + "And CaseID !=\'" + LCID + "\'";
                if (Component.Length == 0)
                    cmd = "select *" + " from " + CADONEFIXCASES + " where " + " Title like \'%" + IssueTitle[index] + "%\'";

                SqlCommand sqlCmd = new SqlCommand(cmd, conn);
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SimilarLCID = (string)reader["CaseID"];
                        if (0 != SimilarLCID.Length)
                        {
                            SimilarLCs = SimilarLCs + SimilarLCID.Trim() + ",";
                            if (SimilarLCs.Length >= 24)
                                return SimilarLCs;
                        }
                    }
                }
            }

            return SimilarLCs;
        }
        public void GetSimilarIssue5()    //from onefix using component and title
        {
            string cmd = "select *" + " from " + CADISSUESTABLENAME + " where IssueProcessed = \'0\'";
            string updatecmd = "";
            string IntermediateResult = "";
            //string resolution    = "";
            int ID = 0;
            string LCID = "";

            string IssueTitle = "";
            string IssueComponent = "";
            string SimilarLCs = "";
            SqlCommand sqlCmd = new SqlCommand(cmd, conn);
            int count = 0;



            using (var reader1 = sqlCmd.ExecuteReader())
            {
                while (reader1.Read())
                {

                    //RootCause = reader1.GetString(5);
                    ID = (int)reader1["ID"];

                    Console.WriteLine("ID        : " + ID);

                    SimilarLCs = "";
                    try
                    {
                        LCID = (string)reader1["LCID"];
                        Console.WriteLine("LCID      : " + LCID);
                        //                        Console.WriteLine("");
                    }
                    catch
                    {
                        Console.WriteLine("Get LCID Failed" + "\n");
                        continue;
                    }

                    LCID = LCID.Trim();
                    if (LCID.Length != 0)
                    {
                        //                        if (1 == GetTitleAndComponent(LCID, out IssueTitle, out IssueComponent))
                        {
                            SimilarLCs = GetSimilarIssue5Internal(LCID, IssueTitle, IssueComponent);
                        }
                    }
                    if (0 != SimilarLCs.Length)
                    {
                        updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + SimilarLCs + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write("SQL Cmd   : " + updatecmd + "\n");
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("");
                    }
                    else
                    {
                        //                        updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + LCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        updatecmd = "update " + CADISSUESTABLENAME + " set IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                        SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                        updatesqlCmd.ExecuteNonQuery();
                        Console.Write("SQL Cmd   : " + updatecmd + "\n");
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("");

                    }
                    count++;
                    if (count > 15)
                    {
                        Console.WriteLine("Total = " + count);
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("");
                        return;
                    }

                }
            }
            Console.WriteLine("Total = " + count);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
        }



    }

}

//            string cmd2 = "select *" + " from " + CADISSUESTABLENAME + " where ID <> \'" + IssueID + "\'" +" and ID = \'5" + "\'";


/*
                        string[] splitSimilarLCID = SimilarLCs.Split(' ');
                        foreach (var splitSLCID in splitSimilarLCID)
                        {
                            if (splitSLCID.Length == 0)
                                continue;
                            
                            updatecmd = "update " + CADISSUESTABLENAME + " set similarLCIDs = \'" + splitSLCID + "\'" + ",IssueProcessed = \'1\'" + " where ID = \'" + ID + "\'";
                            SqlCommand updatesqlCmd = new SqlCommand(updatecmd, conn);
                            updatesqlCmd.ExecuteNonQuery();
                            Console.Write(updatecmd + "\n");
                            Console.WriteLine("");
                            Console.WriteLine("");
                            Console.WriteLine("");
                        }
 */
/*
                        if (0 != SimilarLCID.Length)
                        {
                            string[] splitSimilarLCID = SimilarLCID.Split(' ');
                            foreach (var splitSLCID in splitSimilarLCID)
                            {
                                if (splitSLCID.Length == 0)
                                    continue;

                                if (SimilarLCs.Contains(splitSLCID))
                                    continue;
                                SimilarLCs = SimilarLCs + splitSLCID + ",";
                                if (SimilarLCs.Length >= 24)
                                    return SimilarLCs;

                            }
                        }
*/







