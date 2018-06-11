using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data;
using CitrixAutoAnalysis.analysis.tools;


namespace CitrixAutoAnalysis.analysis.io
{
    class DBHelper :IDisposable
    {
        private static string ConnectionString = @"Data Source=10.150.154.25;Initial Catalog=cse_auto_debugger;User ID=cad_admin;Password=njlcm@2017";

        private SqlConnection conn = null;

        public DBHelper() {
            Initialize();
        }

        private void Initialize() { 
            conn = new SqlConnection(ConnectionString);

            if (conn == null)
            {
                return;
            }
            else
            {
                conn.Open();
            }
        }

        public void Dispose()
        {
            if (conn != null)
            {
                conn.Close();
            }
        }

        public DataTable FillDataTable(string SqlString) {
            SqlCommand cmd = new SqlCommand(SqlString, conn);
            cmd.CommandTimeout = 240;//4 mintues engough?
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running the sql"+SqlString+" : "+ex.ToString());
            }
            return dt;
        }

        public string RetriveStringFromDB(string SqlString)
        {
            SqlCommand cmd = new SqlCommand(SqlString, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count != 1)
            {
                return "";
            }

            return DBConverter.StringFromDBItem(dt.Rows[0][0]);
        }

        public bool UpdateDB(string SqlString)
        {
            SqlCommand cmd = new SqlCommand(SqlString, conn);
            int result = 0;

            try
            {
                result = cmd.ExecuteNonQuery();
            }catch(Exception ex){
                Console.WriteLine("Error running the sql"+SqlString+" : "+ex.ToString());
            }
            return 1 == result;
        }
    }
}
