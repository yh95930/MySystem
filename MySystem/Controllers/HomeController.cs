﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Threading;

namespace FineUIMvc.EmptyProject.Controllers
{
    public class HomeController : BaseController
    {
        // ------------------------------------首页
        // Index
        public ActionResult Index()
        {
            // 若未登录，转至登录界面
            Session["SchoolId"] = "10611";
            if (Session["SchoolId"] == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Tree1Nodes = loadTreeTableData().ToArray();

            //初始化“添加专业”窗体
            string sSQL = "SELECT majorID '专业ID', majorName '专业名称' from [fGetSchoolMajorInfo](" + Session["SchoolId"] + ") WHERE status = 0";
            DataView AddMajorDv = SqlHelper.getDataSource(sSQL);
            ViewBag.AddMajorTable = AddMajorDv.ToTable();
            ViewBag.AddMajorGridColumns = AddOtherColumn(InitGridColumns(AddMajorDv.ToTable())).ToArray();
            ViewBag.SchoolId = Session["SchoolId"];

            return View();
        }

#region 树节点相关函数
        // loadTreeTableData:加载树控件数据
        private List<TreeNode> loadTreeTableData()
        {
            List<TreeNode> nodes = new List<TreeNode>();

            // 模拟从数据库返回数据表
            DataTable table = CreateDataTable();

            DataSet ds = new DataSet();
            ds.Tables.Add(table);
            ds.Relations.Add("TreeRelation", ds.Tables[0].Columns["Id"], ds.Tables[0].Columns["ParentId"]);

            //修改"ParentId"为空的节点，一般是一层节点
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row.IsNull("ParentId"))
                {
                    TreeNode node = new TreeNode();
                    node.Text = row["Text"].ToString();
                    node.NavigateUrl = row["NavigateUrl"].ToString();
                    nodes.Add(node);

                    ResolveSubTree(row, node);
                }
            }

            // 返回节点数据
            return nodes;
        }

        //ResolveSubTree
        private void ResolveSubTree(DataRow dataRow, TreeNode treeNode)
        {
            DataRow[] rows = dataRow.GetChildRows("TreeRelation");
            //修改低层次节点
            if (rows.Length > 0)
            {
                // 如果是目录，则默认展开
                treeNode.Expanded = false;
                foreach (DataRow row in rows)
                {
                    TreeNode node = new TreeNode();
                    // 如若该节点是“删除节点”，给该节点赋予id，并修改css
                    if(row["Id"].ToString().IndexOf("Delete") > 0)
                    {
                        node.NodeID = row["Id"].ToString();
                        node.CssClass = "NodeRed";
                        node.IconFont = IconFont.Remove;
                    }
                    node.Text = row["Text"].ToString();
                    node.NavigateUrl = row["NavigateUrl"].ToString();
                    treeNode.Nodes.Add(node);

                    ResolveSubTree(row, node);
                }
            }
        }

        //CreateDataTable 建立树节点
        private DataTable CreateDataTable()
        {
            DataTable table = new DataTable();
            DataColumn column1 = new DataColumn("Id", typeof(string));
            DataColumn column2 = new DataColumn("Text", typeof(String));
            DataColumn column3 = new DataColumn("ParentId", typeof(string));
            DataColumn column4 = new DataColumn("NavigateUrl", typeof(String));
            table.Columns.Add(column1);
            table.Columns.Add(column2);
            table.Columns.Add(column3);
            table.Columns.Add(column4);

            //从数据库中获取数据
            string sSQL = "SELECT [tableID],[tableOriginID]+[tableName] as tableName,[tableOriginID],[tableDemo] FROM dbo.tableDefine";
            DataView dt = SqlHelper.getDataSource(sSQL);
            sSQL = "select * from [dbo].[fGetSchoolMajorInfo](" + Session["SchoolId"] + ") where status = 1";
            DataView majorStatus = SqlHelper.getDataSource(sSQL);
            DataRow row;

            row = table.NewRow();
            row[0] = "MajorInWriting";
            row[1] = "填报中的专业";
            row[2] = DBNull.Value;
            row[3] = DBNull.Value;
            table.Rows.Add(row);

            for (int j = 0; j < majorStatus.Count; j++)
            {
                //建立树的一级节点
                row = table.NewRow();
                row[0] = majorStatus[j][0].ToString();
                row[1] = majorStatus[j][1].ToString();
                row[2] = "MajorInWriting";
                row[3] = DBNull.Value;
                table.Rows.Add(row);

                //建立删除节点
                row = table.NewRow();
                row[0] = majorStatus[j][0].ToString() + "Delete";
                row[1] = "删除" + majorStatus[j][1].ToString() + "专业的所有数据";
                row[2] = majorStatus[j][0].ToString();
                row[3] = DBNull.Value;
                table.Rows.Add(row);

                //建立数的二级节点
                for (int i = 0; i < Convert.ToInt32(dt.Count); i++)
                {
                    row = table.NewRow();
                    row[0] = Convert.ToString(i + 1)+Convert.ToString(j);
                    row[1] = dt[i][1].ToString();
                    row[2] = majorStatus[j][0].ToString();
                    row[3] = "~/Home/TableCollection/"+dt[i][0].ToString()+"/"+majorStatus[j][0].ToString();
                    table.Rows.Add(row);
                }
            }
            
            return table;
        }

        // TreeMenu_DelNodeClick 删除节点被点击
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TreeMenu_DelNodeClick(string nodeId, string nodeText)
        {
            // 这里添加数据库操作
            nodeId = System.Text.RegularExpressions.Regex.Replace(nodeId, "Delete", "");
            string sSQL = "DELETE FROM [dbo].[tableValues] WHERE SchoolID = '"+ Session["SchoolId"] + "' AND MajorID = '" + nodeId + "'";
            if(SqlHelper.deleteDataSource(sSQL) == 0)
            {
                ShowNotify(nodeText + "失败");
                return UIHelper.Result();
            }
            UIHelper.Tree("treeMenu").LoadData(loadTreeTableData());

            ShowNotify(nodeText+ "成功");

            return UIHelper.Result();
        }

#endregion

        //BtnAddMajorWindow_Click 显示添加专业窗体
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BtnAddMajorWindow_Click()
        {
            UIHelper.Window("AddMajowWindow").Show();

            return UIHelper.Result();
        }

        //BtnCloseAddMajorWindow_Click 关闭添加专业窗体
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BtnCloseAddMajorWindow_Click()
        {
            UIHelper.Window("AddMajowWindow").Hide();

            return UIHelper.Result();
        }

        //BtnOpenChangePwd_Click 打开“添加专业”窗体
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BtnOpenChangePwd_Click()
        {
            UIHelper.Window("ChangePwdWindow").Show();

            return UIHelper.Result();
        }

        //BtnCloseChangePwd_Click 关闭“修改密码”窗体
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BtnCloseChangePwd_Click()
        {
            UIHelper.SimpleForm("ChangePwdForm").Reset();
            UIHelper.Window("ChangePwdWindow").Close();

            return UIHelper.Result();
        }

        //AddOtherColumn 为“添加专业”表格添加额外行
        public List<GridColumn> AddOtherColumn(List<GridColumn> columns)
        {
            RenderField field1 = null;

            field1 = new RenderField();
            field1.HeaderText = "添加";
            field1.RendererFunction = "renderAction3";
            field1.MinWidth = 80;
            field1.BoxFlex = 1;
            columns.Add(field1);

            return columns;
        }

        //“添加专业”按钮被点击
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnAddMajor_RowCommand(string MajorId, string MajorName)
        {
            // 此处添加数据库操作代码
            string sSQL = "INSERT INTO [dbo].[tableValues] (SchoolID, MajorID, TableID, RowID, AttributeID, AttributeValue) " +
                "VALUES ('" + Session["SchoolId"] + "','" + MajorId + "',0,1,1,'1')";
            SqlHelper.insertDataSource(sSQL);
            UIHelper.Tree("treeMenu").LoadData(loadTreeTableData());

            UIHelper.Window("AddMajowWindow").Hide();
            ShowNotify("添加" + MajorName + "专业成功!");

            return UIHelper.Result();
        }

        // btnChangePwd_Click 修改密码按钮
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnChangePwd_Click(string FirstPwd, string SecondPwd)
        {
            if (FirstPwd == SecondPwd)
            {
                string sSQL = "UPDATE [dbo].[baseSchool] SET password = " + FirstPwd + " WHERE schoolID = '" + Session["SchoolId"] + "'";
                ShowNotify("修改成功！");
                SqlHelper.updateDataSource(sSQL);

                UIHelper.Window("ChangePwdWindow").Close();
            }
            else
            {
                ShowNotify("两次密码输入不一致！");
            }

            UIHelper.SimpleForm("ChangePwdForm").Reset();

            return UIHelper.Result();
        }

        //DataImport_Click导入按钮
        public ActionResult DataImport_Click()
        {
            UIHelper.Window("Window2").Show();
         

            return UIHelper.Result();
        }
        //关闭导入窗口
        public ActionResult BtnCloseDataImportWindow_Click()
        {
            UIHelper.Window("Window2").Hide();

            return UIHelper.Result();
        }

        // ------------------------------------右侧子页面，TableCollection包含View()函数
        //TableCollection 建立所有表格子界面的函数
        public ActionResult TableCollection(int TableId, string MajorId)
        {
            ViewBag.TableMajorMsg = "'" + TableId.ToString() + "$" + MajorId + "'";
            ViewBag.MajorId = MajorId;

            //初始化表名和Demo,dt中存取TableOriginID, TableName, TableDemo
            string sSQL = "SELECT TableOriginID, TableName, TableDemo FROM TableDefine WHERE TableID = " + TableId;
            DataView dt = SqlHelper.getDataSource(sSQL);
            ViewBag.TableName = dt.Table.Rows[0][0].ToString() + " " + dt.Table.Rows[0][1].ToString();
            string tableDemo = dt.Table.Rows[0][2].ToString();
            ViewBag.noTableDemo = true;
            ViewBag.TableTbMidStyle = "border-top: none;border-left: none;border-right: none";
            if ( tableDemo != "")
            {
                ViewBag.TableDemo = "填表提示：" + tableDemo;
                ViewBag.noTableDemo = false;
                ViewBag.TableTbMidStele = "border: none";
            }
            
            // 初始化专业下拉列表
            sSQL = "SELECT majorID, majorName FROM baseMajor";
            dt = SqlHelper.getDataSource(sSQL);
            InitMajorDropList(dt);

            //初始化表格
            sSQL = "exec [dbo].[GetTableDataList] " + TableId + ",'" + Session["SchoolId"] +"','" + MajorId + "'";
            dt = SqlHelper.getDataSource(sSQL);
            sSQL = "SELECT    dbo.tableAttributeArrange.attributeID, dbo.tableAttributeDefine.attributeName," +
                    "dbo.tableAttributeArrange.attriDisplayOrder, dbo.tableAttributeDefine.attriFormat, " +
                    "dbo.tableAttributeVerifyRule.attriVerifyRuleName, dbo.tableAttributeVerifyRule.attriVerifyRuleValues " +
                    "FROM      dbo.tableAttributeArrange LEFT JOIN " +
                    "dbo.tableAttributeDefine ON dbo.tableAttributeArrange.attributeID = dbo.tableAttributeDefine.attributeID LEFT JOIN " +
                    "dbo.tableAttributeVerifyRule ON dbo.tableAttributeDefine.attriVerifyRuleID = dbo.tableAttributeVerifyRule.attriVerifyRuleID " +
                    "WHERE dbo.tableAttributeArrange.tableID = " + TableId + " ORDER BY dbo.tableAttributeArrange.attriDisplayOrder";
            DataView VerifyDv = SqlHelper.getDataSource(sSQL);
            ViewBag.table = dt.ToTable();
            ViewBag.Grid1Columns = InitGridColumns(dt.ToTable(),VerifyDv, MajorId).ToArray();

            // 初始化字段下拉列表
            InitFieldDropList(dt);

            ViewBag.PrintPage = "/Home/PrintPage/" + TableId + "/" + MajorId;

            
            ViewBag.AddDataFormItems = AutoGenerateForm(VerifyDv, MajorId, TableId.ToString()).ToArray();

            return View();
        }

        // InitGridColumns 初始化表格函数 (只有一个DataTable参数)
        public List<GridColumn> InitGridColumns(DataTable ds)
        {
            List<GridColumn> columns = new List<GridColumn>();

            RenderField field = null;

            columns.Add(new RowNumberField());

            //为表格创建动态列
            for (int i = 1; i < ds.Columns.Count; i++)
            {
                field = new RenderField();
                field.HeaderText = ds.Columns[i].ToString();
                field.DataField = ds.Columns[i].ToString();
                if (i == ds.Columns.Count - 1)
                {
                    field.BoxFlex = 1;
                }
                if (ds.Columns[i].ToString() == "专业名称")
                {
                    field.Width = 200;
                }
                field.MinWidth = 100;

                columns.Add(field);
            }

            return columns;
        }

        // InitGridColumns 初始化表格函数 (DataTable参数+校验表参数)
        public List<GridColumn> InitGridColumns(DataTable ds, DataView VerifyDv, string MajorId)
        {
            List<GridColumn> columns = new List<GridColumn>();

            RenderField field = null;
            TextBox NewControl1 = null;         // 单文本
            TextArea NewControl2 = null;        // 文本框
            DatePicker NewControl3 = null;      // 日期
            DropDownList NewControl4 = null;    // 单选框和其他字段
            NumberBox NewControl5 = null;       // 数值
            ListItem NewList = null;            // 单选下拉列表
            string RadioValue = null;           // 单选列表值
            string VerifyType = null;           // 校验字段类型
            string VerifyValue = null;          // 校验值
            string sSQL = null;
            DataView DependedTable = null;      // 其他字段 - 依赖的表
            int DependedTableId;                // 其他字段 - 依赖的表名
            int DependedAttriId;                // 其他字段 - 依赖的属性ID
            int DisplayOrder;                   // 其他字段 - 依赖字段的显示顺序
            int i;
            int j;

            columns.Add(new RowNumberField());

            //为表格创建动态列
            for (i = 1; i < ds.Columns.Count; i++)
            {
                field = new RenderField();
                field.HeaderText = ds.Columns[i].ToString();
                field.DataField = ds.Columns[i].ToString();
                if (i == ds.Columns.Count - 1)
                {
                    field.BoxFlex = 1;
                }
                field.MinWidth = 80;

                VerifyType = VerifyDv[i - 1][3].ToString();
                VerifyValue = VerifyDv[i - 1][5].ToString();
                if (VerifyType == "单文本")
                {
                    NewControl1 = new TextBox();
                    field.Editor.Add(NewControl1);
                }
                else if (VerifyType == "文本框")
                {
                    NewControl2 = new TextArea();
                    field.Editor.Add(NewControl2);
                }
                else if (VerifyType == "日期")
                {
                    NewControl3 = new DatePicker();
                    field.FieldType = FieldType.Date;
                    field.Renderer = Renderer.Date;
                    field.RendererArgument = "yyyyMMdd";
                    field.Editor.Add(NewControl3);
                }
                else if (VerifyType == "单选")
                {
                    NewControl4 = new DropDownList();
                    while (VerifyValue != null)
                    {
                        // 截取校验值并赋值给RadioValue
                        RadioValue = VerifyValue.IndexOf('$') >= 0 ? (VerifyValue.Substring(0, VerifyValue.IndexOf('$'))) : VerifyValue;
                        // 创建新列表元素并加入下拉列表中
                        NewList = new ListItem();
                        NewList.Text = RadioValue;
                        NewList.Value = RadioValue;
                        NewControl4.Items.Add(NewList);

                        // 移除校验值中已经确认的值
                        VerifyValue = VerifyValue.IndexOf('$') >= 0 ? VerifyValue.Remove(0, VerifyValue.IndexOf("$") + 1) : null;
                    }
                    field.Editor.Add(NewControl4);
                }
                else if (VerifyType == "数值")
                {
                    field.FieldType = FieldType.Int;
                    NewControl5 = new NumberBox();
                    NewControl5.NoDecimal = true;
                    field.Editor.Add(NewControl5);
                }
                else if (VerifyType == "其他字段")
                {
                    NewControl4 = new DropDownList();
                    DependedTableId = Convert.ToInt32(VerifyValue.Substring(0, VerifyValue.IndexOf('$')));
                    DependedAttriId = Convert.ToInt32(VerifyValue.Substring(VerifyValue.IndexOf('$') + 1));
                    sSQL = "SELECT attriDisplayOrder FROM [dbo].[tableAttributeArrange] WHERE tableID = " + DependedTableId + " AND attributeID = " + DependedAttriId;
                    DisplayOrder = Convert.ToInt32(SqlHelper.getDataSource(sSQL)[0][0].ToString());
                    
                    sSQL = "exec [dbo].[GetTableDataList] " + DependedTableId + ",'" + Session["SchoolId"] + "','" + MajorId + "'";
                    DependedTable = SqlHelper.getDataSource(sSQL);
                    for (j = 0; j < DependedTable.Count; j++)
                    {
                        RadioValue = DependedTable[j][DisplayOrder].ToString();
                        NewList = new ListItem();
                        NewList.Text = RadioValue;
                        NewList.Value = RadioValue;
                        NewControl4.Items.Add(NewList);
                    }
                    field.Editor.Add(NewControl4);
                }

                columns.Add(field);
            }

            return columns;
        }

        #region “增加数据”按钮功能实现
        // addData_Click “增加数据”按钮
        public ActionResult addData_Click(string TableMsg)
        {
            //string MajorId = TableMsg.Substring(TableMsg.IndexOf('$') + 1);
            //string TableId = TableMsg.Substring(0, TableMsg.IndexOf('$'));

            //string sSQL = "SELECT    dbo.tableAttributeArrange.attributeID, dbo.tableAttributeDefine.attributeName," +
            //        "dbo.tableAttributeArrange.attriDisplayOrder, dbo.tableAttributeDefine.attriFormat, " +
            //        "dbo.tableAttributeVerifyRule.attriVerifyRuleName, dbo.tableAttributeVerifyRule.attriVerifyRuleValues " +
            //        "FROM      dbo.tableAttributeArrange LEFT JOIN " +
            //        "dbo.tableAttributeDefine ON dbo.tableAttributeArrange.attributeID = dbo.tableAttributeDefine.attributeID LEFT JOIN " +
            //        "dbo.tableAttributeVerifyRule ON dbo.tableAttributeDefine.attriVerifyRuleID = dbo.tableAttributeVerifyRule.attriVerifyRuleID " +
            //        "WHERE dbo.tableAttributeArrange.tableID = " + TableId + " ORDER BY dbo.tableAttributeArrange.attriDisplayOrder";
            //DataView VerifyDv = SqlHelper.getDataSource(sSQL);
            //AutoGenerateForm(VerifyDv, MajorId, TableId);

            UIHelper.Window("Window1").Show();

            return UIHelper.Result();
        }

        private List<ControlBase> AutoGenerateForm(DataView VerifyDv, string MajorId, string TableId)
        {
            List<ControlBase> items = new List<ControlBase>();

            string AttriFormat = null;          // 表单校验类型
            TextBox Control1 = null;            // 单文本
            TextArea Control2 = null;           // 文本框
            DatePicker Control3 = null;         // 日期选择框
            DropDownList Control4 = null;       // 单选框、复选框、其他字段
            NumberBox Control5 = null;          // 数值
            string VerifyValue = null;          // 校验值
            ListItem NewList = null;            // 下拉列表元素
            string RadioValue = null;           // 单选列表值
            bool IsControlLeft = true;          // 判断控件是否该放在窗口左边
            FormRow NewFormRow = null;          // 新的表单行
            DataView DependedTable = null;      // 其他字段 - 依赖的表
            int DependedTableId;                // 其他字段 - 依赖的表名
            int DependedAttriId;                // 其他字段 - 依赖的属性ID
            int DisplayOrder;                   // 其他字段 - 依赖字段的显示顺序
            string sSQL = null;
            int j;

            for (int i = 0; i < VerifyDv.Count; i++)
            {
                AttriFormat = VerifyDv[i][3].ToString();
                VerifyValue = VerifyDv[i][5].ToString();
                if (IsControlLeft)
                    NewFormRow = CreatNewRow();
                if (AttriFormat == "单文本")
                {
                    Control1 = new TextBox();
                    Control1.Label = VerifyDv[i][1].ToString();
                    Control1.BoxFlex = 1;
                    Control1.ID = VerifyDv[i][0].ToString();

                    NewFormRow.Items.Add(Control1);
                }
                else if (AttriFormat == "文本框")
                {
                    Control2 = new TextArea();
                    Control2.Label = VerifyDv[i][1].ToString();
                    Control2.BoxFlex = 1;
                    Control2.ID = VerifyDv[i][0].ToString();

                    NewFormRow.Items.Add(Control2);
                }
                else if (AttriFormat == "日期")
                {
                    Control3 = new DatePicker();
                    Control3.Label = VerifyDv[i][1].ToString();
                    Control3.BoxFlex = 1;
                    Control3.DateFormatString = "yyyyMMdd";
                    Control3.ID = VerifyDv[i][0].ToString();

                    NewFormRow.Items.Add(Control3);
                }
                else if (AttriFormat == "单选")
                {
                    Control4 = new DropDownList();
                    Control4.Label = VerifyDv[i][1].ToString();
                    Control4.BoxFlex = 1;
                    Control4.ID = VerifyDv[i][0].ToString();

                    while (VerifyValue != null)
                    {
                        // 截取校验值并赋值给RadioValue
                        RadioValue = VerifyValue.IndexOf('$') >= 0 ? (VerifyValue.Substring(0, VerifyValue.IndexOf('$'))) : VerifyValue;
                        // 创建新列表元素并加入下拉列表中
                        NewList = new ListItem();
                        NewList.Text = RadioValue;
                        NewList.Value = RadioValue;
                        Control4.Items.Add(NewList);

                        // 移除校验值中已经确认的值
                        VerifyValue = VerifyValue.IndexOf('$') >= 0 ? VerifyValue.Remove(0, VerifyValue.IndexOf("$") + 1) : null;
                    }

                    NewFormRow.Items.Add(Control4);
                }
                else if (AttriFormat == "数值")
                {
                    Control5 = new NumberBox();
                    Control5.Label = VerifyDv[i][1].ToString();
                    Control5.BoxFlex = 1;
                    Control5.ID = VerifyDv[i][0].ToString();

                    NewFormRow.Items.Add(Control5);
                }
                else if (AttriFormat == "其他字段")
                {
                    Control4 = new DropDownList();
                    Control4.Label = VerifyDv[i][1].ToString();
                    Control4.BoxFlex = 1;
                    Control4.ID = VerifyDv[i][0].ToString();

                    DependedTableId = Convert.ToInt32(VerifyValue.Substring(0, VerifyValue.IndexOf('$')));
                    DependedAttriId = Convert.ToInt32(VerifyValue.Substring(VerifyValue.IndexOf('$') + 1));
                    sSQL = "SELECT attriDisplayOrder FROM [dbo].[tableAttributeArrange] WHERE tableID = " + DependedTableId + " AND attributeID = " + DependedAttriId;
                    DisplayOrder = Convert.ToInt32(SqlHelper.getDataSource(sSQL)[0][0].ToString());

                    sSQL = "exec [dbo].[GetTableDataList] " + DependedTableId + ",'" + Session["SchoolId"] + "','" + MajorId + "'";
                    DependedTable = SqlHelper.getDataSource(sSQL);
                    for (j = 0; j < DependedTable.Count; j++)
                    {
                        RadioValue = DependedTable[j][DisplayOrder].ToString();
                        NewList = new ListItem();
                        NewList.Text = RadioValue;
                        NewList.Value = RadioValue;
                        Control4.Items.Add(NewList);
                    }

                    NewFormRow.Items.Add(Control4);
                }
                else if (AttriFormat == "复选框")
                {
                    Control4 = new DropDownList();
                    Control4.Label = VerifyDv[i][1].ToString();
                    Control4.BoxFlex = 1;
                    Control4.EnableMultiSelect = true;
                    Control4.ID = VerifyDv[i][0].ToString();

                    while (VerifyValue != null)
                    {
                        // 截取校验值并赋值给RadioValue
                        RadioValue = VerifyValue.IndexOf('$') >= 0 ? (VerifyValue.Substring(0, VerifyValue.IndexOf('$'))) : VerifyValue;
                        // 创建新列表元素并加入下拉列表中
                        NewList = new ListItem();
                        NewList.Text = RadioValue;
                        NewList.Value = RadioValue;
                        Control4.Items.Add(NewList);

                        // 移除校验值中已经确认的值
                        VerifyValue = VerifyValue.IndexOf('$') >= 0 ? VerifyValue.Remove(0, VerifyValue.IndexOf("$") + 1) : null;
                    }

                    NewFormRow.Items.Add(Control4);
                }

                if (!IsControlLeft)
                    items.Add(NewFormRow);
                IsControlLeft = !IsControlLeft;
            }
            // 向AddDataForm载入剩下的单个控件
            if (!IsControlLeft)
                items.Add(NewFormRow);

            return items;
        }

        // CreatNewRow()在表单中创建新的一行
        private FormRow CreatNewRow()
        {
            FineUIMvc.FormRow NewRow = new FineUIMvc.FormRow();
            NewRow.Layout = LayoutType.HBox;

            return NewRow;
        }

        #endregion


        // InitMajorDropList初始化专业下拉列表
        public void InitMajorDropList(DataView dv)
        {
            DataTable table = new DataTable();
            DataColumn column1 = new DataColumn("MajorText", typeof(String));
            DataColumn column2 = new DataColumn("MajorValue", typeof(String));
            table.Columns.Add(column1);
            table.Columns.Add(column2);

            for (int i = 0; i < Convert.ToInt16(dv.Count); i++)
            {
                DataRow row = table.NewRow();
                row[0] = dv[i][1].ToString();
                row[1] = dv[i][0].ToString();
                table.Rows.Add(row);
                row = table.NewRow();
            }

            ViewBag.BindMajorField = "Major";
            ViewBag.MajorDropDownListDataSource = table;
        }

        // InitFieldDropList 初始化字段下拉列表
        public void InitFieldDropList(DataView dv)
        {
            DataTable table = new DataTable();
            DataColumn column1 = new DataColumn("FieldText", typeof(String));
            DataColumn column2 = new DataColumn("FieldValue", typeof(String));
            table.Columns.Add(column1);
            table.Columns.Add(column2);
            DataTable dt = dv.ToTable();

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                DataRow row = table.NewRow();
                row[0] = dt.Columns[i].ToString();
                row[1] = dt.Columns[i].ToString();
                table.Rows.Add(row);
                row = table.NewRow();
            }

            ViewBag.BindFieldField = "Field";
            ViewBag.FieldDropDownListDataSource = table;
        }

        // BtnQuery_Click 查询按钮
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BtnQuery_Click(string QueryField, string QueryValue)
        {
            if (QueryValue == "")
                return UIHelper.Result();
            //string sSQL = ""

            return UIHelper.Result();
        }

        // CloseWindow1 关闭ID为“Window1”的窗口
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CloseWindow1()
        {
            UIHelper.Window("Window1").Close();

            return UIHelper.Result();
        }

        // btnAddData_Click 添加数据中的“添加”按钮
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnAddData_Click(FormCollection values)
        {
            
            

            return UIHelper.Result();
        }

        // btnSaveData_Click “保存数据”按钮
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnSaveData_Click(JArray Grid1_fields, JArray Grid1_modifiedData, string TableMsg)
        {
            string MajorId = TableMsg.Substring(TableMsg.IndexOf('$') + 1);
            string TableId = TableMsg.Substring(0, TableMsg.IndexOf('$'));

            string sSQL = "exec [dbo].[GetTableDataList] " + TableId + ",'" + Session["SchoolId"] + "','" + MajorId + "'";
            DataTable source = SqlHelper.getDataSource(sSQL).ToTable();

            foreach (JObject modifiedRow in Grid1_modifiedData)
            {
                string status = modifiedRow.Value<string>("status");
                int TableIndex = Convert.ToInt32(modifiedRow.Value<string>("index"));

                if (status == "modified")
                {
                    UpdateDataRow(modifiedRow, TableIndex, source);
                }

            }

            UIHelper.Grid("Grid1").DataSource(source, Grid1_fields);

            ShowNotify(MajorId + "+" + TableId);

            return UIHelper.Result();
        }

        private void UpdateDataRow(JObject modifiedRow, int TableIndex, DataTable source)
        {
            Dictionary<string, object> rowDict = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();
            DataRow rowData = source.Rows[TableIndex];
            
            for(int i = 0; i < source.Columns.Count; i ++)
            {
                UpdateDataRow(source.Columns[i].ToString(), rowDict, rowData);
            }
        }

        private void UpdateDataRow(string columnName, Dictionary<string, object> rowDict, DataRow rowData)
        {
            // columnName是列名，rowDict[columnName]是列值，可以通过函数外面的source.Rows[TableIndex][0].ToStrubg()获取RowID
            if (rowDict.ContainsKey(columnName))
            {
                rowData[columnName] = rowDict[columnName];
            }
        }

        // ------------------------------------登录界面
        // Login
        public ActionResult Login()
        {
            LoadVerifyData();

            return View();
        }

        // 加载验证码图片
        private void LoadVerifyData()
        {
            string imageHTML = InitCaptchaCode();

            ViewBag.ImgCaptchaText = imageHTML;
        }

        // 初始化验证码
        private string InitCaptchaCode()
        {
            // 创建一个 6 位的随机数并保存在 Session 对象中
            Session["CaptchaImageText"] = GenerateRandomCode();

            string imageUrl = Url.Content("/Home/CaptchaImage?w=100&h=26&t=" + DateTime.Now.Ticks);
            CaptchaImage();
            return String.Format("<img src=\"{0}\" />", imageUrl);
        }

        // 创建一个 6 位的随机数
        // </summary>
        // <returns></returns>
        private string GenerateRandomCode()
        {
            string s = String.Empty;
            Random random = new Random();
            for (int i = 0; i < 6; i++)
            {
                s += random.Next(10).ToString();
            }
            return s;
        }

        // imgCaptcha_Click
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult imgCaptcha_Click()
        {
            string imageHTML = InitCaptchaCode();

            UIHelper.LinkButton("imgCaptcha").Text(imageHTML, encodeText: false);

            return UIHelper.Result();
        }

        //btnLogin_Click “登录系统”按钮
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnLogin_Click(FormCollection values)
        {
            if (values["tbxCaptcha"] != Session["CaptchaImageText"].ToString())
            {
                ShowNotify("验证码错误！");
            }
            else
            {
                string sSQL = "SELECT schoolID, schoolName, password FROM baseSchool WHERE schoolID = " + values["tbxUserName"];
                DataView dv = SqlHelper.getDataSource(sSQL);
                DataTable dt = dv.ToTable();

                if (dv.Count > 0 && values["tbxPassword"] == dt.Rows[0][2].ToString())
                {
                    Session["SchoolID"] = dt.Rows[0][0].ToString();
                    Session["SchoolName"] = dt.Rows[0][1].ToString();
                    ShowNotify("登录成功");

                    return RedirectToAction("Index");
                }
                else
                {
                    ShowNotify("用户名或密码错误！", MessageBoxIcon.Error);
                }
            }
               

            return UIHelper.Result();
        }

        // 返回验证图片
        public ActionResult CaptchaImage(int w = 200, int h = 300)
        {
            byte[] imageBytes;

            // 从 Session 中读取验证码，并创建图片
            using (CaptchaImage.CaptchaImage ci = new CaptchaImage.CaptchaImage(Session["CaptchaImageText"].ToString(), w, h, "Consolas"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ci.Image.Save(ms, ImageFormat.Jpeg);
                    imageBytes = ms.ToArray();
                }
            }

            return File(imageBytes, "image/jpeg");
        }

        // ------------------------------------打印页面
        public ActionResult PrintPage(int TableId = 1, string MajorId = "050101")
        {
            //初始化表名和Demo,dt中存取TableOriginID, TableName, TableDemo
            string sSQL = "SELECT TableOriginID, TableName, TableDemo FROM TableDefine WHERE TableID = " + TableId;
            DataView dt = SqlHelper.getDataSource(sSQL);
            ViewBag.TableName = dt.Table.Rows[0][0].ToString() + " " + dt.Table.Rows[0][1].ToString();

            // 初始化专业下拉列表
            sSQL = "SELECT majorID, majorName FROM baseMajor";
            dt = SqlHelper.getDataSource(sSQL);
            InitMajorDropList(dt);
            ViewBag.MajorId = MajorId;

            //初始化表格
            sSQL = "exec [dbo].[GetTableDataList] " + TableId;
            dt = SqlHelper.getDataSource(sSQL);
            ViewBag.table = dt.ToTable();
            ViewBag.Grid1Columns = InitGridColumns(dt.ToTable()).ToArray();

            return View();
        }

        // GET: Themes
        public ActionResult Themes()
        {
            return View();
        }

        //ProIntroduction
        public ActionResult ProIntroduction()
        {
            //string sSQL = "select * from [fGetSchoolMajorInfo](1)";
            //DataView dt = SqlHelper.getDataSource(sSQL);
            //AddOtherColumn(InitGridColumns(dt.ToTable()));

            return View();
        }

        //Button_OnClick
        public ContentResult Button_OnClick()
        {
            return Content("点击过的按钮");
        }
    }
}