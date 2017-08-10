using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace FineUIMvc.EmptyProject
{
    public class ExcelOperation
    {

        private string filePath;
        private string fileName;
        /**     
         * example : D://uploadFiles//test.xlsx
         * filePath : D://uploadFiles//
         * fileName : test.xlsx        
         */
        public ExcelOperation(string filePath, string fileName)
        {
            this.fileName = fileName;
            if (filePath.LastIndexOf("/") != filePath.Length - 1)
            {
                filePath += "//";
            }
            this.filePath = filePath;
        }

        public DataTable loadDataFromExcel()
        {
            OleDbConnection conn = null;
            DataTable dt = new DataTable();
            string connstring = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + fileName + ";Extended Properties='Excel 8.0;HDR=NO;IMEX=1';";// Office 07及以上版本
            //string connstring = Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + path + ";Extended Properties='Excel 8.0;HDR=NO;IMEX=1';"; //Office 07以下版本 
            try
            {
                conn = new OleDbConnection(connstring);
                conn.Open();
                DataTable sheetsName = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null); //得到所有sheet的名字              
                string firstSheetName = sheetsName.Rows[0][2].ToString(); //得到第一个sheet的名字
                //遍历输出各Sheet的名称
                //foreach (DataRow row in sheetsName.Rows)
                //{
                //    Debug.WriteLine(row["TABLE_NAME"]);
                //}
                string sql = string.Format("SELECT * FROM [{0}]", firstSheetName); //查询字符串
                //string sql = "select * from [Sheet1$]";
                OleDbDataAdapter ada = new OleDbDataAdapter(sql, connstring);
                DataSet set = new DataSet();
                ada.Fill(set);
                dt = set.Tables[0];
            }
            catch (OleDbException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

            return dt;
        }
    }
}