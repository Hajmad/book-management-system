using System.Collections.Generic; // 引入集合命名空间，用于保存子分类列表。

namespace BookShelfApp.Models // 定义模型类所在命名空间。
{ // 命名空间开始。
    public class Category // 定义分类模型类，对应数据库中的 Categories 表。
    { // 类开始。
        public int Id { get; set; } // 分类唯一编号，对应 Categories 表的 Id 字段。

        public string Name { get; set; } = string.Empty; // 分类名称，对应 Categories 表的 Name 字段。

        public int? ParentId { get; set; } // 父分类编号；null 表示主分类，有值表示子分类。

        public string CreatedTime { get; set; } = string.Empty; // 创建时间，对应 Categories 表的 CreatedTime 字段。

        public List<Category> Children { get; set; } = new List<Category>(); // 子分类列表，用于以后绑定 TreeView 树形分类。
    } // 类结束。
} // 命名空间结束。
