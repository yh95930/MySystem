using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Collections;
using System.Text.RegularExpressions;

namespace FineUIMvc.EmptyProject
{
    public class TableVerification
    {

        private DataTable dataTable;
        private int tableID;
        private string majorID;
        private string schoolID;
        /**
         dataTable 第一行必须包含表头
        */

        public TableVerification(string schoolID, string majorID, int tableID, DataTable dataTable)
        {
            this.tableID = tableID;
            this.majorID = majorID;
            this.schoolID = schoolID;
            this.dataTable = dataTable;
        }

        public bool verifyArchitecture()
        {
            string sql = string.Format("select AttributeName from tableAttributeDefine,"
                + "(select AttributeID,attriDisplayOrder from tableAttributeArrange where TableID = {0})t"
                + " where tableAttributeDefine.AttributeID = t.AttributeID order by t.attriDisplayOrder", tableID);

            DataView dv = SqlHelper.getDataSource(sql);
            if (dv == null || dv.Count != dataTable.Columns.Count)
            {
                return false;
            }
            for (int i = 0; i < dv.Count; i++)
            {
                DataRowView r = dv[i];

                if (!r[0].Equals(dataTable.Rows[0][i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool verifyPrimaryKey()
        {
            string sql = string.Format("select attriDisplayOrder from tableAttributeArrange where TableID = {0} and attriIsPrimaryKey = 1", tableID);
            DataView dv = SqlHelper.getDataSource(sql);
            if (dv == null)
            {
                return false;
            }
            int col = Convert.ToInt32(dv[0][0]) - 1;
            Hashtable hTable = new Hashtable();
            for (int i = 1; i < dataTable.Rows.Count; i++)
            {
                object key = dataTable.Rows[i][col];

                if (key == null || key.ToString().Equals("") || hTable.ContainsKey(key))
                {
                    return false;
                }
                else
                {
                    hTable.Add(key, 1);
                }

            }
            return true;
        }

        /**
         * {0,2}：表示第0行，第2列数据验证失败
         * 
         */
        public ArrayList verifyData()
        {
            ArrayList list = new ArrayList();
            string sql = string.Format("exec dbo.getTableVerifyValues '{0}','{1}','{2}'", new Object[] { schoolID, majorID, tableID });
            DataView dv = SqlHelper.getDataSource(sql);
            for (int j = 0; j < dv.Count; j++)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    if (i + 1 == Convert.ToInt32(dv[j][0]))
                    {

                        for (int k = 1; k < dataTable.Rows.Count; k++)
                        {
                            bool valid = false;
                            string value = (string)dv[j][1];
                            string[] values = Regex.Split(value, "\\$", RegexOptions.IgnoreCase);
                            //string[] values = value.Split(new char['$']);
                            foreach (string v in values)
                            {
                                if (v.Equals(dataTable.Rows[k][i]))
                                {
                                    valid = true;
                                    break;
                                }
                            }
                            if (!valid)
                            {
                                list.Add(k + "," + i);
                            }
                        }

                    }

                }
            }
            return list;
        }

    }
}