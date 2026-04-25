using System.Collections.Generic; // 引入泛型集合命名空间，用于使用 List<T> 类型。
using BookShelfApp.Models; // 引入模型命名空间，用于使用 Book 类型。

namespace BookShelfApp.Repositories // 定义当前接口所在命名空间，表示它属于仓储层。
{ // 命名空间开始。
    public interface IBookRepository // 定义图书仓储接口，用于约束图书数据的增删改查方法。
    { // 接口开始。
        List<Book> GetAllBooks(); // 定义获取全部图书的方法，返回数据库中的所有图书列表。
        List<Book> SearchBooksByTitle(string keyword); // 定义按书名关键词搜索的方法，返回匹配关键词的图书列表。
        List<Book> GetBooksByFileType(string fileType); // 定义按文件类型筛选的方法，返回指定类型（PDF/EPUB/TXT）的图书列表。
        List<Book> GetFavoriteBooks(); // 定义获取收藏图书的方法，返回 IsFavorite 为 true 的图书列表。
        List<Book> GetRecentBooks(int count); // 定义获取最近阅读图书的方法，按最近打开时间倒序返回前 count 条记录。
        List<Book> GetUncategorizedBooks(); // 定义获取未分类图书的方法，返回 Category 为空或空字符串的图书列表。
        List<string> GetAllContentCategories(); // 定义获取全部内容分类名称列表的方法，用于左侧和右键菜单动态显示。
        bool ExistsByFilePath(string filePath); // 定义按文件路径检查是否已存在的方法，避免重复导入。
        bool ContentCategoryExists(string categoryName); // 定义检查某个内容分类是否已存在的方法，用于新增分类前去重。
        void InsertBook(Book book); // 定义新增图书的方法，把一本书写入数据库。
        void UpdateFavoriteStatus(int id, bool isFavorite); // 定义更新收藏状态的方法，根据图书 Id 修改是否收藏。
        void UpdateLastOpenTime(int id, string lastOpenTime); // 定义更新最近打开时间的方法，根据图书 Id 写入最近打开时间。
        void UpdateCategory(int id, string category); // 定义按图书 Id 更新内容分类的方法，供手动修改分类功能调用。
        void AddContentCategory(string categoryName); // 定义新增内容分类的方法，用于手动添加新分类并持久化保存。
        void DeleteBookById(int bookId); // 声明按主键删除书籍记录的方法，让通过接口类型访问时也能调用删除逻辑。
        void DeleteAllBooks(); // 声明“清空 Books 表全部记录”的方法，供界面层通过接口调用。
        void UpdateBookCategory(int bookId, string categoryName); // 声明更新书籍内容分类的方法，用于把某本书移动到指定 TreeView 内容分类。

        void UpdateBookCoverPath(int bookId, string coverPath); // 声明更新书籍封面路径的方法，用于修复缺失封面后写回数据库。



    } // 接口结束。

} // 命名空间结束。
