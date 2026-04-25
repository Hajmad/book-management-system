-- 如果 Categories 表还不存在，就创建这张表。
CREATE TABLE IF NOT EXISTS Categories
(
    -- Id 是分类的唯一编号，INTEGER PRIMARY KEY AUTOINCREMENT 表示 SQLite 会自动生成递增编号。
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    -- Name 是分类名称，例如“计算机类”“英语类”“考研资料”。
    Name TEXT NOT NULL,

    -- ParentId 是父分类编号，NULL 表示主分类，有数字表示它属于某个主分类。
    ParentId INTEGER NULL,

    -- CreatedTime 是分类创建时间，用文本保存，方便第一版简单处理。
    CreatedTime TEXT NOT NULL,

    -- 外键约束：ParentId 指向同一张 Categories 表里的 Id。
    FOREIGN KEY (ParentId) REFERENCES Categories(Id)
);

-- 给分类名称和父分类建立唯一索引，防止同一个父分类下面出现重名分类。
CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_ParentId_Name
ON Categories(ParentId, Name);
