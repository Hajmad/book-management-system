using Microsoft.Data.Sqlite; // 引入 SQLite 类型，用于数据库连接和命令执行。

namespace BookShelfApp.Data // 定义当前文件所属命名空间，表示这是数据层代码。
{ // 命名空间开始。
    public static class DbInitializer // 定义数据库初始化静态类，负责建表和基础数据准备。
    { // 类开始。
        public static void InitializeDatabase() // 定义应用启动时调用的方法，用于初始化数据库结构。
        { // 方法开始。
            using SqliteConnection sqliteConnection = SQLiteConnectionFactory.CreateConnection(); // 创建数据库连接对象并交给 using 自动释放。
            sqliteConnection.Open(); // 打开数据库连接，准备执行 SQL。

            string createBooksTableSql = @"CREATE TABLE IF NOT EXISTS Books (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Author TEXT, FilePath TEXT NOT NULL UNIQUE, FileType TEXT NOT NULL CHECK (FileType IN ('PDF', 'EPUB', 'TXT')), CoverPath TEXT, Category TEXT, IsFavorite INTEGER NOT NULL DEFAULT 0 CHECK (IsFavorite IN (0, 1)), ReadProgress INTEGER NOT NULL DEFAULT 0 CHECK (ReadProgress >= 0 AND ReadProgress <= 100), LastOpenTime TEXT, AddedTime TEXT NOT NULL, Description TEXT);"; // 定义 Books 表建表 SQL，表不存在时创建。

            using SqliteCommand createBooksTableCommand = sqliteConnection.CreateCommand(); // 创建执行 Books 建表 SQL 的命令对象。
            createBooksTableCommand.CommandText = createBooksTableSql; // 把 Books 建表 SQL 赋值给命令对象。
            createBooksTableCommand.ExecuteNonQuery(); // 执行 Books 建表命令。

            string createContentCategoriesTableSql = @"CREATE TABLE IF NOT EXISTS ContentCategories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, AddedTime TEXT NOT NULL);"; // 定义内容分类表建表 SQL，Name 唯一防止重复分类。
            string createCategoriesTableSql = @"CREATE TABLE IF NOT EXISTS Categories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, ParentId INTEGER NULL, CreatedTime TEXT NOT NULL, FOREIGN KEY (ParentId) REFERENCES Categories(Id));"; // 定义树形分类表，ParentId 为空表示主分类，有值表示子分类。


            using SqliteCommand createContentCategoriesTableCommand = sqliteConnection.CreateCommand(); // 创建执行分类表建表 SQL 的命令对象。
            createContentCategoriesTableCommand.CommandText = createContentCategoriesTableSql; // 把分类表建表 SQL 赋值给命令对象。
            createContentCategoriesTableCommand.ExecuteNonQuery(); // 执行分类表建表命令。

            using (SqliteCommand createCategoriesTableCommand = sqliteConnection.CreateCommand()) // 创建树形分类表的 SQLite 命令对象。
            { // using 代码块开始。
                createCategoriesTableCommand.CommandText = createCategoriesTableSql; // 把树形分类表建表 SQL 赋值给命令对象。
                createCategoriesTableCommand.ExecuteNonQuery(); // 执行树形分类表建表命令，确保 Categories 表存在。
            } // using 代码块结束。

            string createCategoriesIndexSql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_ParentId_Name ON Categories(ParentId, Name);"; // 定义唯一索引 SQL，防止同一个父分类下出现同名分类。
            using SqliteCommand createCategoriesIndexCommand = sqliteConnection.CreateCommand(); // 创建执行唯一索引 SQL 的命令对象。
            createCategoriesIndexCommand.CommandText = createCategoriesIndexSql; // 把唯一索引 SQL 赋值给命令对象。
            createCategoriesIndexCommand.ExecuteNonQuery(); // 执行唯一索引创建命令。


            string seedDefaultCategoriesSql = @"INSERT OR IGNORE INTO ContentCategories (Name, AddedTime) VALUES ('电子信息类', datetime('now')); INSERT OR IGNORE INTO ContentCategories (Name, AddedTime) VALUES ('计算机类', datetime('now')); INSERT OR IGNORE INTO ContentCategories (Name, AddedTime) VALUES ('文学类', datetime('now')); INSERT OR IGNORE INTO ContentCategories (Name, AddedTime) VALUES ('历史政治法律类', datetime('now')); INSERT OR IGNORE INTO ContentCategories (Name, AddedTime) VALUES ('未分类', datetime('now'));"; // 定义默认分类初始化 SQL，使用 INSERT OR IGNORE 防止重复插入。

            using SqliteCommand seedDefaultCategoriesCommand = sqliteConnection.CreateCommand(); // 创建执行默认分类初始化 SQL 的命令对象。
            seedDefaultCategoriesCommand.CommandText = seedDefaultCategoriesSql; // 把默认分类初始化 SQL 赋值给命令对象。
            seedDefaultCategoriesCommand.ExecuteNonQuery(); // 执行默认分类初始化命令。

            string seedTreeCategoriesSql = @"INSERT INTO Categories (Name, ParentId, CreatedTime) SELECT '电子信息类', NULL, datetime('now') WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = '电子信息类' AND ParentId IS NULL); INSERT INTO Categories (Name, ParentId, CreatedTime) SELECT '计算机类', NULL, datetime('now') WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = '计算机类' AND ParentId IS NULL); INSERT INTO Categories (Name, ParentId, CreatedTime) SELECT '文学类', NULL, datetime('now') WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = '文学类' AND ParentId IS NULL); INSERT INTO Categories (Name, ParentId, CreatedTime) SELECT '历史政治法律类', NULL, datetime('now') WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = '历史政治法律类' AND ParentId IS NULL); INSERT INTO Categories (Name, ParentId, CreatedTime) SELECT '未分类', NULL, datetime('now') WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = '未分类' AND ParentId IS NULL);"; // 定义树形分类默认主分类初始化 SQL，只有同名主分类不存在时才插入，避免 ParentId 为 NULL 时重复插入。

            using SqliteCommand seedTreeCategoriesCommand = sqliteConnection.CreateCommand(); // 创建执行树形分类默认数据初始化 SQL 的命令对象。
            seedTreeCategoriesCommand.CommandText = seedTreeCategoriesSql; // 把默认树形分类初始化 SQL 赋值给命令对象。
            seedTreeCategoriesCommand.ExecuteNonQuery(); // 执行默认树形分类初始化命令。


        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
