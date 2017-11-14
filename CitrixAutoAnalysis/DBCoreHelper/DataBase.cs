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
        // 连接字符串
        //public const string ConnectionString = "Data Source=10.150.143.83;Initial Catalog=cse_auto_debugger;Integrated Security=False;uid=cad_admin;pwd=njlcm@2017";
        //public const string ConnectionString = "Data Source=10.150.143.83;Initial Catalog=cse_auto_debugger;Integrated Security=False;MultipleActiveResultSets=True;uid=cad_admin;pwd=njlcm@2017";
        public const string ConnectionString = "Data Source=DUKE-PC\\SQLEXPRESS;Initial Catalog=ParsePattern;Integrated Security=False;uid=sa;pwd=7ujm*IK<";
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
        public Boolean DBOpen()
        {

            try
            {
                // 打开数据库连接 n
                conn.Open();
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

    }

}
