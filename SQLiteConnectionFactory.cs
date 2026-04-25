using BookShelfApp.Config; // 引入我们刚写的路径配置类 AppPaths，用来拿数据库文件路径。
using Microsoft.Data.Sqlite; // 引入 SQLite 官方库，提供 SqliteConnection 类。

namespace BookShelfApp.Data // 定义当前类所在命名空间，表示属于数据访问层。
{ // 命名空间开始。
    public static class SQLiteConnectionFactory // 定义静态工厂类，专门负责创建数据库连接对象。
    { // 类开始。
        public static SqliteConnection CreateConnection() // 定义公共方法，返回一个新的 SQLite 连接实例。
        { // 方法开始。
            string connectionString = $"Data Source={AppPaths.DatabaseFilePath}"; // 按 SQLite 格式拼接连接字符串，Data Source 指向数据库文件路径。
            SqliteConnection sqliteConnection = new SqliteConnection(connectionString); // 用连接字符串创建一个 SQLite 连接对象。
            return sqliteConnection; // 把连接对象返回给调用方（后续可 Open 并执行 SQL）。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
