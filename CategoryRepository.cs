using System; // 引入基础类型，例如 int? 和 Exception。
using System.Collections.Generic; // 引入 List 集合，用于返回分类列表。
using BookShelfApp.Config; // 引入 AppPaths，用于获取 SQLite 数据库文件路径。
using BookShelfApp.Models; // 引入 Category 模型类。
using Microsoft.Data.Sqlite; // 引入 SQLite 数据库访问类。

namespace BookShelfApp.Repositories // 定义仓储类所在命名空间。
{ // 命名空间开始。
    public class CategoryRepository // 定义分类仓储类，负责 Categories 表的增删查。
    { // 类开始。
        private string GetConnectionString() // 获取 SQLite 数据库连接字符串。
        { // 方法开始。
            return "Data Source=" + AppPaths.DatabaseFilePath + ";"; // 返回当前应用数据库文件路径对应的连接字符串。
        } // 方法结束。

        public List<Category> GetAllCategories() // 获取所有分类，并整理成“主分类包含子分类”的树形列表。
        { // 方法开始。
            List<Category> allCategoryList = new List<Category>(); // 保存从数据库读出的所有分类。
            Dictionary<int, Category> categoryMap = new Dictionary<int, Category>(); // 用 Id 快速找到对应分类对象。
            List<Category> rootCategoryList = new List<Category>(); // 保存最终返回的主分类列表。

            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建数据库连接对象。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "SELECT Id, Name, ParentId, CreatedTime FROM Categories ORDER BY ParentId IS NOT NULL, ParentId, Name;"; // 查询所有分类。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令对象。
                { // using 开始。
                    using (SqliteDataReader reader = command.ExecuteReader()) // 执行查询并获取读取器。
                    { // using 开始。
                        while (reader.Read()) // 逐行读取查询结果。
                        { // while 开始。
                            Category category = new Category(); // 创建分类对象。
                            category.Id = reader.GetInt32(0); // 读取 Id 字段。
                            category.Name = reader.GetString(1); // 读取 Name 字段。
                            category.ParentId = reader.IsDBNull(2) ? null : reader.GetInt32(2); // 读取 ParentId，数据库 NULL 转成 C# null。
                            category.CreatedTime = reader.GetString(3); // 读取 CreatedTime 字段。
                            allCategoryList.Add(category); // 加入所有分类列表。
                            categoryMap[category.Id] = category; // 放入字典，方便按 Id 查找。
                        } // while 结束。
                    } // using 结束。
                } // using 结束。
            } // using 结束。

            foreach (Category category in allCategoryList) // 遍历所有分类，开始整理父子关系。
            { // foreach 开始。
                if (category.ParentId == null) // ParentId 为空表示主分类。
                { // if 开始。
                    rootCategoryList.Add(category); // 加入主分类列表。
                    continue; // 主分类处理完，继续下一个。
                } // if 结束。

                if (categoryMap.ContainsKey(category.ParentId.Value)) // 如果能找到它的父分类。
                { // if 开始。
                    categoryMap[category.ParentId.Value].Children.Add(category); // 把当前分类加入父分类的 Children 列表。
                } // if 结束。
            } // foreach 结束。

            return rootCategoryList; // 返回整理好的树形分类列表。
        } // 方法结束。

        public int AddRootCategory(string categoryName) // 新增主分类，ParentId 保存为 NULL。
        { // 方法开始。
            return AddCategory(categoryName, null); // 调用通用新增方法，父分类传 null。
        } // 方法结束。

        public int AddChildCategory(string categoryName, int parentId) // 在指定主分类下面新增子分类。
        { // 方法开始。
            return AddCategory(categoryName, parentId); // 调用通用新增方法，父分类传主分类 Id。
        } // 方法结束。

        private int AddCategory(string categoryName, int? parentId) // 通用新增分类方法，主分类和子分类都走这里。
        { // 方法开始。
            string cleanName = (categoryName ?? string.Empty).Trim(); // 清理分类名称首尾空格。
            if (string.IsNullOrWhiteSpace(cleanName)) // 判断分类名是否为空。
            { // if 开始。
                throw new ArgumentException("分类名称不能为空。"); // 分类名无效时抛出异常。
            } // if 结束。

            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "INSERT INTO Categories (Name, ParentId, CreatedTime) VALUES (@Name, @ParentId, @CreatedTime); SELECT last_insert_rowid();"; // 插入分类并返回新 Id。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令。
                { // using 开始。
                    command.Parameters.AddWithValue("@Name", cleanName); // 参数化传入分类名。
                    command.Parameters.AddWithValue("@ParentId", parentId.HasValue ? parentId.Value : DBNull.Value); // 参数化传入父分类，主分类用 NULL。
                    command.Parameters.AddWithValue("@CreatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); // 参数化传入创建时间。
                    object result = command.ExecuteScalar(); // 执行插入并读取新 Id。
                    return Convert.ToInt32(result); // 把 SQLite 返回的 Id 转成 int。
                } // using 结束。
            } // using 结束。
        } // 方法结束。

        public void DeleteCategory(int categoryId) // 删除指定分类，删除前会检查该分类下是否还有书籍。
        { // 方法开始。
            Category category = GetCategoryById(categoryId); // 先根据 Id 查询分类对象，拿到分类名称。
            if (category == null) // 判断分类是否存在。
            { // if 开始。
                return; // 分类不存在时直接结束，不需要删除。
            } // if 结束。

            if (HasBooksInCategory(category.Name)) // 检查该分类名称下是否还有书籍。
            { // if 开始。
                throw new InvalidOperationException("该分类下还有书籍，不能直接删除。"); // 有书时抛出异常，让 UI 层提示用户。
            } // if 结束。

            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "DELETE FROM Categories WHERE Id = @Id;"; // 删除指定 Id 的分类。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令。
                { // using 开始。
                    command.Parameters.AddWithValue("@Id", categoryId); // 参数化传入分类 Id。
                    command.ExecuteNonQuery(); // 执行删除。
                } // using 结束。
            } // using 结束。
        } // 方法结束。

        public Category GetCategoryById(int categoryId) // 根据分类 Id 查询单个分类对象。
        { // 方法开始。
            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "SELECT Id, Name, ParentId, CreatedTime FROM Categories WHERE Id = @Id;"; // 按 Id 查询分类。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令。
                { // using 开始。
                    command.Parameters.AddWithValue("@Id", categoryId); // 参数化传入分类 Id。
                    using (SqliteDataReader reader = command.ExecuteReader()) // 执行查询并获取读取器。
                    { // using 开始。
                        if (reader.Read()) // 如果查询到一行数据。
                        { // if 开始。
                            Category category = new Category(); // 创建分类对象。
                            category.Id = reader.GetInt32(0); // 读取 Id。
                            category.Name = reader.GetString(1); // 读取 Name。
                            category.ParentId = reader.IsDBNull(2) ? null : reader.GetInt32(2); // 读取 ParentId，数据库 NULL 转 C# null。
                            category.CreatedTime = reader.GetString(3); // 读取 CreatedTime。
                            return category; // 返回查询到的分类。
                        } // if 结束。
                    } // using 结束。
                } // using 结束。
            } // using 结束。

            return null; // 没查到分类时返回 null。
        } // 方法结束。


        public bool HasChildCategories(int categoryId) // 检查指定分类下面是否还有子分类。
        { // 方法开始。
            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建 SQLite 数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "SELECT COUNT(1) FROM Categories WHERE ParentId = @ParentId;"; // 查询以当前分类为父分类的子分类数量。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令对象。
                { // using 开始。
                    command.Parameters.AddWithValue("@ParentId", categoryId); // 参数化传入当前分类 Id，避免 SQL 注入。
                    long childCount = (long)command.ExecuteScalar(); // 执行统计查询，得到子分类数量。
                    return childCount > 0; // 子分类数量大于 0 表示不能直接删除。
                } // using 结束。
            } // using 结束。
        } // 方法结束。

        public int GetBookCountByCategoryName(string categoryName) // 根据分类名称统计该分类下直接包含的书籍数量。
        { // 方法开始。
            string cleanCategoryName = (categoryName ?? string.Empty).Trim(); // 去掉分类名称前后空格，避免因为空格导致统计不准。
            if (string.IsNullOrWhiteSpace(cleanCategoryName)) // 判断分类名称是否为空。
            { // if 开始。
                return 0; // 分类名为空时直接返回 0。
            } // if 结束。

            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建 SQLite 数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "SELECT COUNT(1) FROM Books WHERE Category = @Category;"; // 统计 Books 表中 Category 等于当前分类名称的书籍数量。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令对象。
                { // using 开始。
                    command.Parameters.AddWithValue("@Category", cleanCategoryName); // 使用参数传入分类名称，避免 SQL 注入。
                    long count = (long)command.ExecuteScalar(); // 执行统计查询，SQLite COUNT 返回 long。
                    return Convert.ToInt32(count); // 转成 int 返回给界面层使用。
                } // using 结束。
            } // using 结束。
        } // 方法结束。

        public bool CategoryNameExists(string categoryName, int? parentId) // 检查同一个父分类下是否已经存在同名分类。
        { // 方法开始。
            string cleanCategoryName = (categoryName ?? string.Empty).Trim(); // 去掉分类名称前后空格，确保“ 英语类 ”和“英语类”按同一个名称判断。
            if (string.IsNullOrWhiteSpace(cleanCategoryName)) // 判断分类名是否为空。
            { // if 开始。
                return false; // 空名称不在这里判断重复，交给界面层提示“不能为空”。
            } // if 结束。

            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建 SQLite 数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = string.Empty; // 先定义 SQL 字符串，下面根据主分类/子分类分别赋值。
                using (SqliteCommand command = connection.CreateCommand()) // 创建 SQL 命令对象。
                { // using 开始。
                    if (parentId == null) // ParentId 为空表示检查主分类是否重名。
                    { // if 开始。
                        sql = "SELECT COUNT(1) FROM Categories WHERE ParentId IS NULL AND Name = @Name;"; // 只检查主分类中的同名分类。
                        command.CommandText = sql; // 设置 SQL。
                        command.Parameters.AddWithValue("@Name", cleanCategoryName); // 传入分类名称。
                    } // if 结束。
                    else // ParentId 有值表示检查某个主分类下面的子分类是否重名。
                    { // else 开始。
                        sql = "SELECT COUNT(1) FROM Categories WHERE ParentId = @ParentId AND Name = @Name;"; // 检查同一个父分类下是否同名。
                        command.CommandText = sql; // 设置 SQL。
                        command.Parameters.AddWithValue("@ParentId", parentId.Value); // 传入父分类 Id。
                        command.Parameters.AddWithValue("@Name", cleanCategoryName); // 传入分类名称。
                    } // else 结束。

                    long count = (long)command.ExecuteScalar(); // 执行统计查询。
                    return count > 0; // 大于 0 表示同级已有这个名称。
                } // using 结束。
            } // using 结束。
        } // 方法结束。



        public bool HasBooksInCategory(string categoryName) // 检查某个分类名称下是否已有书籍。
        { // 方法开始。
            string cleanCategoryName = (categoryName ?? string.Empty).Trim(); // 清理分类名称首尾空格。
            if (string.IsNullOrWhiteSpace(cleanCategoryName)) // 判断分类名是否为空。
            { // if 开始。
                return false; // 空分类名不需要阻止删除。
            } // if 结束。

            using (SqliteConnection connection = new SqliteConnection(GetConnectionString())) // 创建数据库连接。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "SELECT COUNT(1) FROM Books WHERE Category = @Category;"; // 统计 Books 表中该分类下有多少本书。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令。
                { // using 开始。
                    command.Parameters.AddWithValue("@Category", cleanCategoryName); // 参数化传入分类名称，避免 SQL 注入。
                    long count = (long)command.ExecuteScalar(); // 执行统计并得到数量。
                    return count > 0; // 数量大于 0 表示分类下还有书。
                } // using 结束。
            } // using 结束。
        } // 方法结束。

    } // 类结束。
} // 命名空间结束。
