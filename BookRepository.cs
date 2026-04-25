using BookShelfApp.Config;
using BookShelfApp.Data; // 引入数据层命名空间，用于使用 SQLiteConnectionFactory。
using BookShelfApp.Models; // 引入模型命名空间，用于使用 Book 实体类。
using Microsoft.Data.Sqlite; // 引入 SQLite 命名空间，用于 SqliteConnection、SqliteCommand、SqliteDataReader。
using System; // 引入 System 命名空间，提供 Convert 等基础类型转换功能。
using System.Collections.Generic; // 引入泛型集合命名空间，提供 List<T> 类型。

namespace BookShelfApp.Repositories // 定义当前类所属命名空间，表示这是仓储层代码。
{ // 命名空间开始。
    public class BookRepository : IBookRepository // 定义 BookRepository 类，并实现 IBookRepository 接口。
    { // 类开始。
        public List<Book> GetAllBooks() // 实现获取全部图书的方法。
        { // 方法开始。
            List<Book> bookList = new List<Book>(); // 创建一个空列表，用来接收查询结果。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接并交给 using 自动释放。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Id, Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description FROM Books ORDER BY AddedTime DESC;"; // 设置查询 SQL，按导入时间倒序返回。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并获取结果读取器。
            while (sqliteDataReader.Read()) // 循环读取每一行结果。
            { // while 代码块开始。
                Book book = MapBook(sqliteDataReader); // 把当前行映射成 Book 对象。
                bookList.Add(book); // 把 Book 对象加入结果列表。
            } // while 代码块结束。
            return bookList; // 返回查询到的图书列表。
        } // 方法结束。

        public void DeleteBookById(int bookId) // 定义按主键删除书籍的方法。
        { // 方法开始。
            string connectionString = "Data Source=" + AppPaths.DatabaseFilePath + ";"; // 组装 SQLite 连接字符串，指向当前程序数据库文件。
            using (SqliteConnection connection = new SqliteConnection(connectionString)) // 创建 SQLite 连接对象。
            { // using 代码块开始。
                connection.Open(); // 打开数据库连接。

                string sql = "DELETE FROM Books WHERE Id = @Id;"; // 定义参数化删除语句。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建命令对象并绑定连接。
                { // using 代码块开始。
                    command.Parameters.AddWithValue("@Id", bookId); // 传入主键参数值，避免 SQL 注入风险。
                    command.ExecuteNonQuery(); // 执行删除命令。
                } // using 代码块结束。
            } // using 代码块结束。
        } // 方法结束。

        public void DeleteAllBooks() // 实现清空全部书籍记录的方法，与接口 IBookRepository 中的方法声明保持一致。
        { // 方法开始。
            string connectionString = "Data Source=" + AppPaths.DatabaseFilePath + ";"; // 组装 SQLite 连接字符串，指向当前应用数据库文件。
            using (SqliteConnection connection = new SqliteConnection(connectionString)) // 创建数据库连接对象，并用 using 保证资源会被自动释放。
            { // using 代码块开始。
                connection.Open(); // 打开数据库连接，准备执行 SQL 命令。

                string sql = "DELETE FROM Books;"; // 定义删除语句：删除 Books 表中的所有记录。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令对象并绑定到当前连接。
                { // using 代码块开始。
                    command.ExecuteNonQuery(); // 执行不返回结果集的命令，完成全表清空操作。
                } // using 代码块结束。
            } // using 代码块结束。
        } // 方法结束。




        public List<Book> SearchBooksByTitle(string keyword) // 实现按标题关键词搜索的方法。
        { // 方法开始。
            List<Book> bookList = new List<Book>(); // 创建结果列表，用于保存查询返回的图书。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接对象。
            sqliteConnection.Open(); // 打开数据库连接，准备执行查询。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Id, Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description FROM Books WHERE Title LIKE @Keyword ESCAPE '\\' ORDER BY AddedTime DESC;"; // 设置参数化查询 SQL，使用 LIKE 并声明转义字符。
            string safeKeyword = keyword ?? string.Empty; // 对输入关键词做空值保护，避免 null 拼接。
            safeKeyword = safeKeyword.Replace("\\", "\\\\"); // 先转义反斜杠，防止转义语义被破坏。
            safeKeyword = safeKeyword.Replace("%", "\\%"); // 转义百分号，避免被当作任意长度通配符。
            safeKeyword = safeKeyword.Replace("_", "\\_"); // 转义下划线，避免被当作单字符通配符。
            string likePattern = "%" + safeKeyword + "%"; // 组装最终 LIKE 模式，实现包含匹配。
            SqliteParameter keywordParameter = sqliteCommand.CreateParameter(); // 显式创建参数对象，避免 AddWithValue 的隐式类型推断。
            keywordParameter.ParameterName = "@Keyword"; // 设置参数名称，需与 SQL 中占位符一致。
            keywordParameter.SqliteType = SqliteType.Text; // 显式声明参数类型为 Text，确保 SQLite 按文本处理。
            keywordParameter.Value = likePattern; // 设置参数值为处理后的 LIKE 模式。
            sqliteCommand.Parameters.Add(keywordParameter); // 把参数加入命令对象参数集合。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并获取读取器。
            while (sqliteDataReader.Read()) // 循环读取每一条结果记录。
            { // while 代码块开始。
                Book book = MapBook(sqliteDataReader); // 把当前记录映射为 Book 对象。
                bookList.Add(book); // 把 Book 对象加入结果列表。
            } // while 代码块结束。
            return bookList; // 返回查询结果列表。
        } // 方法结束。


        public List<Book> GetBooksByFileType(string fileType) // 实现按文件类型筛选的方法。
        { // 方法开始。
            List<Book> bookList = new List<Book>(); // 创建结果列表。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Id, Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description FROM Books WHERE FileType = @FileType ORDER BY AddedTime DESC;"; // 设置按文件类型查询的 SQL。
            sqliteCommand.Parameters.AddWithValue("@FileType", fileType); // 给文件类型参数赋值。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并读取结果。
            while (sqliteDataReader.Read()) // 循环读取每一条记录。
            { // while 代码块开始。
                Book book = MapBook(sqliteDataReader); // 把当前记录映射成 Book。
                bookList.Add(book); // 加入结果列表。
            } // while 代码块结束。
            return bookList; // 返回指定类型图书列表。
        } // 方法结束。

        public List<Book> GetFavoriteBooks() // 实现获取收藏图书的方法。
        { // 方法开始。
            List<Book> bookList = new List<Book>(); // 创建结果列表。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Id, Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description FROM Books WHERE IsFavorite = 1 ORDER BY AddedTime DESC;"; // 设置收藏图书查询 SQL。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并获取读取器。
            while (sqliteDataReader.Read()) // 循环读取每一条记录。
            { // while 代码块开始。
                Book book = MapBook(sqliteDataReader); // 把记录映射为 Book。
                bookList.Add(book); // 把 Book 加入结果列表。
            } // while 代码块结束。
            return bookList; // 返回收藏图书列表。
        } // 方法结束。

        public List<Book> GetRecentBooks(int count) // 实现获取最近阅读图书的方法。
        { // 方法开始。
            List<Book> bookList = new List<Book>(); // 创建结果列表。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Id, Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description FROM Books WHERE LastOpenTime IS NOT NULL AND LastOpenTime <> '' ORDER BY LastOpenTime DESC LIMIT @Count;"; // 设置最近阅读查询 SQL。
            sqliteCommand.Parameters.AddWithValue("@Count", count); // 给返回条数参数赋值。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并读取结果。
            while (sqliteDataReader.Read()) // 循环读取查询结果。
            { // while 代码块开始。
                Book book = MapBook(sqliteDataReader); // 映射当前行到 Book。
                bookList.Add(book); // 加入结果列表。
            } // while 代码块结束。
            return bookList; // 返回最近阅读图书列表。
        } // 方法结束。

        public List<Book> GetUncategorizedBooks() // 实现获取未分类图书的方法。
        { // 方法开始。
            List<Book> bookList = new List<Book>(); // 创建结果列表。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Id, Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description FROM Books WHERE Category IS NULL OR TRIM(Category) = '' ORDER BY AddedTime DESC;"; // 设置未分类查询 SQL。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并读取结果。
            while (sqliteDataReader.Read()) // 循环读取每一条记录。
            { // while 代码块开始。
                Book book = MapBook(sqliteDataReader); // 映射当前记录为 Book。
                bookList.Add(book); // 把结果加入列表。
            } // while 代码块结束。
            return bookList; // 返回未分类图书列表。
        } // 方法结束。

        public bool ExistsByFilePath(string filePath) // 实现按文件路径检查是否存在的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT COUNT(1) FROM Books WHERE FilePath = @FilePath;"; // 设置统计相同路径记录数的 SQL。
            sqliteCommand.Parameters.AddWithValue("@FilePath", filePath); // 给文件路径参数赋值。
            object? scalarResult = sqliteCommand.ExecuteScalar(); // 执行标量查询，返回单个值。
            int existedCount = Convert.ToInt32(scalarResult); // 把结果转换为整数数量。
            return existedCount > 0; // 只要数量大于 0，就说明数据库里已存在该路径。
        } // 方法结束。

        public void InsertBook(Book book) // 实现新增图书的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "INSERT INTO Books (Title, Author, FilePath, FileType, CoverPath, Category, IsFavorite, ReadProgress, LastOpenTime, AddedTime, Description) VALUES (@Title, @Author, @FilePath, @FileType, @CoverPath, @Category, @IsFavorite, @ReadProgress, @LastOpenTime, @AddedTime, @Description);"; // 设置插入 SQL。
            sqliteCommand.Parameters.AddWithValue("@Title", book.Title); // 绑定书名参数。
            sqliteCommand.Parameters.AddWithValue("@Author", book.Author); // 绑定作者参数。
            sqliteCommand.Parameters.AddWithValue("@FilePath", book.FilePath); // 绑定文件路径参数。
            sqliteCommand.Parameters.AddWithValue("@FileType", book.FileType); // 绑定文件类型参数。
            sqliteCommand.Parameters.AddWithValue("@CoverPath", string.IsNullOrWhiteSpace(book.CoverPath) ? (object)DBNull.Value : book.CoverPath); // 绑定封面路径参数，为空时写入数据库空值。
            sqliteCommand.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(book.Category) ? (object)DBNull.Value : book.Category); // 绑定分类参数，为空时写入数据库空值。
            sqliteCommand.Parameters.AddWithValue("@IsFavorite", book.IsFavorite ? 1 : 0); // 把 bool 收藏值转换为 1/0 写入数据库。
            sqliteCommand.Parameters.AddWithValue("@ReadProgress", book.ReadProgress); // 绑定阅读进度参数。
            sqliteCommand.Parameters.AddWithValue("@LastOpenTime", string.IsNullOrWhiteSpace(book.LastOpenTime) ? (object)DBNull.Value : book.LastOpenTime); // 绑定最近打开时间参数，为空时写空值。
            sqliteCommand.Parameters.AddWithValue("@AddedTime", book.AddedTime); // 绑定导入时间参数。
            sqliteCommand.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(book.Description) ? (object)DBNull.Value : book.Description); // 绑定简介参数，为空时写空值。
            sqliteCommand.ExecuteNonQuery(); // 执行插入命令，不返回结果集。
        } // 方法结束。

        public void UpdateFavoriteStatus(int id, bool isFavorite) // 实现更新收藏状态的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "UPDATE Books SET IsFavorite = @IsFavorite WHERE Id = @Id;"; // 设置更新收藏状态 SQL。
            sqliteCommand.Parameters.AddWithValue("@IsFavorite", isFavorite ? 1 : 0); // 绑定收藏状态参数，把 bool 转换成 1/0。
            sqliteCommand.Parameters.AddWithValue("@Id", id); // 绑定要更新的图书 Id 参数。
            sqliteCommand.ExecuteNonQuery(); // 执行更新命令。
        } // 方法结束。

        public void UpdateBookCategory(int bookId, string categoryName) // 实现更新书籍内容分类的方法。
        { // 方法开始。
            string cleanCategoryName = (categoryName ?? string.Empty).Trim(); // 清理分类名称首尾空格，避免保存多余空格。
            if (string.IsNullOrWhiteSpace(cleanCategoryName)) // 判断分类名是否为空。
            { // if 开始。
                throw new ArgumentException("分类名称不能为空。"); // 分类名为空时抛出异常，避免写入无效数据。
            } // if 结束。

            string connectionString = "Data Source=" + AppPaths.DatabaseFilePath + ";"; // 组装 SQLite 连接字符串，指向当前数据库文件。
            using (SqliteConnection connection = new SqliteConnection(connectionString)) // 创建 SQLite 数据库连接对象。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "UPDATE Books SET Category = @Category WHERE Id = @Id;"; // 定义参数化更新 SQL，只更新指定书籍的 Category 字段。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令对象并绑定连接。
                { // using 开始。
                    command.Parameters.AddWithValue("@Category", cleanCategoryName); // 参数化传入目标分类名称，避免 SQL 注入。
                    command.Parameters.AddWithValue("@Id", bookId); // 参数化传入书籍 Id。
                    command.ExecuteNonQuery(); // 执行更新命令。
                } // using 结束。
            } // using 结束。
        } // 方法结束。



        public void UpdateCategory(int id, string category) // 实现按图书 Id 更新内容分类的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接对象，并在 using 结束时自动释放资源。
            sqliteConnection.Open(); // 打开数据库连接，准备执行更新语句。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 基于当前连接创建 SQL 命令对象。
            sqliteCommand.CommandText = "UPDATE Books SET Category = @Category WHERE Id = @Id;"; // 设置更新 SQL，把指定图书的 Category 字段改成新值。
            sqliteCommand.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(category) ? (object)DBNull.Value : category); // 绑定分类参数；如果传入空白则写入数据库空值。
            sqliteCommand.Parameters.AddWithValue("@Id", id); // 绑定图书 Id 参数，用于定位要更新的记录。
            sqliteCommand.ExecuteNonQuery(); // 执行更新命令，不返回结果集。
        } // 方法结束。

        public void UpdateBookCoverPath(int bookId, string coverPath) // 实现更新书籍封面路径的方法。
        { // 方法开始。
            string cleanCoverPath = coverPath ?? string.Empty; // 读取封面路径，如果为 null 就转为空字符串。
            string connectionString = "Data Source=" + AppPaths.DatabaseFilePath + ";"; // 组装 SQLite 连接字符串，指向当前数据库文件。
            using (SqliteConnection connection = new SqliteConnection(connectionString)) // 创建 SQLite 数据库连接对象。
            { // using 开始。
                connection.Open(); // 打开数据库连接。

                string sql = "UPDATE Books SET CoverPath = @CoverPath WHERE Id = @Id;"; // 定义参数化更新 SQL，只更新指定书籍的 CoverPath 字段。
                using (SqliteCommand command = new SqliteCommand(sql, connection)) // 创建 SQL 命令对象并绑定连接。
                { // using 开始。
                    command.Parameters.AddWithValue("@CoverPath", cleanCoverPath); // 参数化传入新的封面路径。
                    command.Parameters.AddWithValue("@Id", bookId); // 参数化传入书籍 Id。
                    command.ExecuteNonQuery(); // 执行更新命令。
                } // using 结束。
            } // using 结束。
        } // 方法结束。


        public void UpdateLastOpenTime(int id, string lastOpenTime) // 实现更新最近打开时间的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "UPDATE Books SET LastOpenTime = @LastOpenTime WHERE Id = @Id;"; // 设置更新最近打开时间 SQL。
            sqliteCommand.Parameters.AddWithValue("@LastOpenTime", string.IsNullOrWhiteSpace(lastOpenTime) ? (object)DBNull.Value : lastOpenTime); // 绑定最近打开时间参数，为空时写入空值。
            sqliteCommand.Parameters.AddWithValue("@Id", id); // 绑定图书 Id 参数。
            sqliteCommand.ExecuteNonQuery(); // 执行更新命令。
        } // 方法结束。

        private static Book MapBook(SqliteDataReader sqliteDataReader) // 定义私有静态方法，把一行数据库记录转换成 Book 对象。
        { // 方法开始。
            Book book = new Book(); // 创建一个新的 Book 对象。
            book.Id = Convert.ToInt32(sqliteDataReader["Id"]); // 读取并转换 Id 字段。
            book.Title = sqliteDataReader["Title"] == DBNull.Value ? string.Empty : sqliteDataReader["Title"].ToString() ?? string.Empty; // 读取 Title 字段并做空值保护。
            book.Author = sqliteDataReader["Author"] == DBNull.Value ? string.Empty : sqliteDataReader["Author"].ToString() ?? string.Empty; // 读取 Author 字段并做空值保护。
            book.FilePath = sqliteDataReader["FilePath"] == DBNull.Value ? string.Empty : sqliteDataReader["FilePath"].ToString() ?? string.Empty; // 读取 FilePath 字段并做空值保护。
            book.FileType = sqliteDataReader["FileType"] == DBNull.Value ? string.Empty : sqliteDataReader["FileType"].ToString() ?? string.Empty; // 读取 FileType 字段并做空值保护。
            book.CoverPath = sqliteDataReader["CoverPath"] == DBNull.Value ? string.Empty : sqliteDataReader["CoverPath"].ToString() ?? string.Empty; // 读取 CoverPath 字段并做空值保护。
            book.Category = sqliteDataReader["Category"] == DBNull.Value ? string.Empty : sqliteDataReader["Category"].ToString() ?? string.Empty; // 读取 Category 字段并做空值保护。
            book.IsFavorite = Convert.ToInt32(sqliteDataReader["IsFavorite"]) == 1; // 读取 IsFavorite 字段并把 1/0 转为 bool。
            book.ReadProgress = Convert.ToInt32(sqliteDataReader["ReadProgress"]); // 读取 ReadProgress 字段并转换为整数。
            book.LastOpenTime = sqliteDataReader["LastOpenTime"] == DBNull.Value ? string.Empty : sqliteDataReader["LastOpenTime"].ToString() ?? string.Empty; // 读取 LastOpenTime 字段并做空值保护。
            book.AddedTime = sqliteDataReader["AddedTime"] == DBNull.Value ? string.Empty : sqliteDataReader["AddedTime"].ToString() ?? string.Empty; // 读取 AddedTime 字段并做空值保护。
            book.Description = sqliteDataReader["Description"] == DBNull.Value ? string.Empty : sqliteDataReader["Description"].ToString() ?? string.Empty; // 读取 Description 字段并做空值保护。
            return book; // 返回映射完成的 Book 对象。
        } // 方法结束。

        public List<string> GetAllContentCategories() // 实现获取全部内容分类名称列表的方法。
        { // 方法开始。
            List<string> categoryNameList = new List<string>(); // 创建分类名称结果列表。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接对象。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT Name FROM ContentCategories ORDER BY Id ASC;"; // 设置查询 SQL，按插入顺序返回分类名称。
            using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(); // 执行查询并获取读取器。
            while (sqliteDataReader.Read()) // 循环读取每一行分类记录。
            { // while 代码块开始。
                string categoryName = sqliteDataReader["Name"] == DBNull.Value ? string.Empty : sqliteDataReader["Name"].ToString() ?? string.Empty; // 读取 Name 字段并做空值保护。
                if (!string.IsNullOrWhiteSpace(categoryName)) // 判断分类名称是否有效。
                { // if 代码块开始。
                    categoryNameList.Add(categoryName); // 把有效分类名称加入结果列表。
                } // if 代码块结束。
            } // while 代码块结束。
            return categoryNameList; // 返回分类名称列表。
        } // 方法结束。

        public bool ContentCategoryExists(string categoryName) // 实现检查分类是否存在的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接对象。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "SELECT COUNT(1) FROM ContentCategories WHERE Name = @Name;"; // 设置查询 SQL，统计同名分类数量。
            sqliteCommand.Parameters.AddWithValue("@Name", categoryName); // 绑定分类名称参数。
            object? scalarResult = sqliteCommand.ExecuteScalar(); // 执行标量查询并拿到数量结果。
            int existsCount = Convert.ToInt32(scalarResult); // 把结果转换为整数数量。
            return existsCount > 0; // 数量大于 0 说明分类已存在。
        } // 方法结束。

        public void AddContentCategory(string categoryName) // 实现新增内容分类的方法。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接对象。
            sqliteConnection.Open(); // 打开数据库连接。
            using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand(); // 创建 SQL 命令对象。
            sqliteCommand.CommandText = "INSERT INTO ContentCategories (Name, AddedTime) VALUES (@Name, @AddedTime);"; // 设置新增分类 SQL。
            sqliteCommand.Parameters.AddWithValue("@Name", categoryName); // 绑定分类名称参数。
            sqliteCommand.Parameters.AddWithValue("@AddedTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")); // 绑定新增时间参数，便于后续追踪。
            sqliteCommand.ExecuteNonQuery(); // 执行新增分类命令。
        } // 方法结束。


    } // 类结束。
} // 命名空间结束。
