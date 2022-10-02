﻿using DAL;
using DAL.DTO;
using DAL.Entity;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Daily_Newspaper_Tools.Views
{
    public partial class OrganizationUnitForm : UIPage
    {
        public OrganizationUnitForm()
        {
            InitializeComponent();
        }
        #region 事件
        private void OrganizationUnitForm_Load(object sender, EventArgs e)
        {
            InitDepartment();
        }
        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var treeNode = this.uiTreeView1.SelectedNode;
            if (treeNode != null)
            {
                Guid id = Guid.Empty;
                Guid.TryParse(treeNode.Tag.ToString(), out id);
                using (var ctx = new EntityContext())
                {
                    var entity = ctx.Departments.FirstOrDefault(x=>x.Id== id);
                    ctx.Departments.Remove(entity);
                    ctx.SaveChanges();
                    ShowSuccessDialog("添加成功");
                    InitDepartment();
                }
            }
        }
        /// <summary>
        /// 树节点选中显示菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiTreeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)//判断你点的是不是右键
            {
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode CurrentNode = uiTreeView1.GetNodeAt(ClickPoint);
                if (CurrentNode != null)//判断你点的是不是一个节点
                {
                    CurrentNode.ContextMenuStrip = uiContextMenuStrip1;
                    uiTreeView1.SelectedNode = CurrentNode;//选中这个节点
                }
            }
        }
        /// <summary>
        /// 新增节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiBtnCreateRoot_Click(object sender, EventArgs e)
        {
            Department department = new Department()
            {
                Id = Guid.NewGuid(),
            };
            var treeNode = this.uiTreeView1.SelectedNode;
            if (treeNode != null)
            {
                Guid parentId = Guid.Empty;
                Guid.TryParse(treeNode.Tag.ToString(), out parentId);
                department.ParentId = parentId;
            }

            DepartmentEditForm frm = new DepartmentEditForm();
            frm.Departments = department;
            frm.Render();
            frm.ShowDialogWithMask();
            if (frm.IsOK)
            {
                var isSuccess = Create(frm.Departments);
                if (isSuccess)
                {
                    ShowSuccessDialog("添加成功");
                    InitDepartment();
                }
                else
                    ShowErrorTip("添加失败");

            }
            frm.Dispose();
        }
        /// <summary>
        /// 编辑节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiBtnEditNode_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 新增成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiSymbolBtnAdd_Click(object sender, EventArgs e)
        {

        }
        #endregion
        #region 初始化列表及数据


        #endregion
        #region 生成树结构私有方法
        private void InitDepartment()
        {
            using (var ctx = new EntityContext())
            {
                var departments = ctx.Departments.ToList();
                var trees = ConvertToTree(departments);
                uiTreeView1.Nodes.Clear();

                uiTreeView1.Nodes.AddRange(trees.ToArray());

                uiTreeView1.ExpandAll();
            }
        }
        private List<TreeNode> ConvertToTree(
            List<Department> list,
            Guid? Id = null)
        {
            var result = new List<TreeNode>();
            var childList = Children(list, Id);
            foreach (var item in childList)
            {
                var tree = new TreeNode
                {
                    Name = item.Id.ToString(),
                    Text = item.Name,
                    Tag = item.Id.ToString(),
                };
                var childs = ConvertToTree(list, item.Id);
                foreach (var items in childs)
                {
                    tree.Nodes.Add(items);
                }
                result.Add(tree);
            }

            return result;
        }

        private List<Department> Children(
            List<Department> list,
            Guid? Id)
        {
            var childList = list.Where(x => x.ParentId == Id).ToList();
            return childList;
        }

        #endregion
        #region 私有方法
        /// <summary>
        /// 新增节点
        /// </summary>
        /// <param name="department"></param>
        private bool Create(Department department)
        {
            department.Code = GetNextChildCode(department.ParentId);
            var validSuccess = ValidateOrganizationUnit(department);
            if (validSuccess)
            {
                using (var ctx = new EntityContext())
                {
                    ctx.Departments.Add(department);
                    ctx.SaveChanges();
                }
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// 校验
        /// </summary>
        /// <param name="department"></param>
        /// <returns></returns>
        private bool ValidateOrganizationUnit(Department department)
        {
            var siblings = FindChildren(department.ParentId)
            .Where(ou => ou.Id != department.Id)
            .ToList();

            if (siblings.Any(ou => ou.Name == department.Name))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 查询子节点
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public List<Department> FindChildren(Guid? parentId, bool recursive = false)
        {
            if (!recursive)
            {
                return GetChildren(parentId);
            }

            if (!parentId.HasValue)
            {
                return GetList();
            }

            var code = GetCodeOrDefault(parentId.Value);

            return GetAllChildrenWithParentCode(code, parentId);
        }

        public virtual List<Department> GetAllChildrenWithParentCode(string code, Guid? parentId)
        {
            using (var ctx = new EntityContext())
            {
                return ctx.Departments.Where(x => x.Code.StartsWith(code) && x.Id != parentId.Value).ToList();
            }
        }
        /// <summary>
        /// 查询children
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public List<Department> GetChildren(Guid? parentId)
        {
            using (var ctx = new EntityContext())
            {
                return ctx.Departments.Where(e => e.ParentId == parentId).ToList();
            }
        }

        public virtual List<Department> GetList()
        {
            using (var ctx = new EntityContext())
            {
                return ctx.Departments.OrderBy(e => e.Name).ToList();
            }
        }
        /// <summary>
        /// 获取code
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        private string GetNextChildCode(Guid? parentId)
        {
            var lastChild = GetLastChildOrNull(parentId);
            if (lastChild != null)
            {
                return CalculateNextCode(lastChild.Code);
            }

            var parentCode = parentId != null
                ? GetCodeOrDefault(parentId.Value)
                : null;

            return AppendCode(
                parentCode,
                CreateCode(1)
            );
        }

        public virtual string GetCodeOrDefault(Guid id)
        {
            using (var ctx = new EntityContext())
            {
                var entity = ctx.Departments.FirstOrDefault(e => e.Id == id);
                return entity?.Code;
            }
        }
        private string CalculateNextCode(string code)
        {
            var parentCode = GetParentCode(code);
            var lastUnitCode = GetLastUnitCode(code);
            return AppendCode(parentCode, CreateCode(Convert.ToInt32(lastUnitCode) + 1));
        }
        public static string GetLastUnitCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code不能为空");
            }

            var splittedCode = code.Split('.');
            return splittedCode[splittedCode.Length - 1];
        }
        public static string CreateCode(params int[] numbers)
        {
            if (numbers.IsNullOrEmpty())
            {
                return null;
            }

            var code = numbers.Select(number => number.ToString(new string('0', 5)));

            return string.Join(".", code);
        }
        public static string GetParentCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code不能为空");
            }
            var splittedCode = code.Split('.');
            if (splittedCode.Length == 1)
            {
                return null;
            }
            var splitCode = splittedCode.Take(splittedCode.Length - 1);
            return string.Join(".", splitCode);
        }
        public static string AppendCode(string parentCode, string childCode)
        {
            if (childCode.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(childCode), "childCode不能为空");
            }

            if (parentCode.IsNullOrEmpty())
            {
                return childCode;
            }

            return parentCode + "." + childCode;
        }
        /// <summary>
        /// 获取子节点code或根节点
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>

        private Department GetLastChildOrNull(Guid? parentId)
        {
            using (var ctx = new EntityContext())
            {
                var children = ctx.Departments.Where(x => x.ParentId == parentId).ToList();
                return children.OrderBy(c => c.Code).LastOrDefault();
            }
        }
        #endregion

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
