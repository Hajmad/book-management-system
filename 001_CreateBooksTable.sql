-- 第1步：创建 Books 表；如果表已经存在，就不会重复创建
CREATE TABLE IF NOT EXISTS Books (
    -- Id：主键ID；每插入一条书籍记录自动 +1；用于唯一标识一本书
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    -- Title：书名；不能为空；例如“活着”
    Title TEXT NOT NULL,

    -- Author：作者；可以为空；例如“余华”
    Author TEXT,

    -- FilePath：电子书文件完整路径；不能为空；并且必须唯一，防止重复导入同一本文件
    FilePath TEXT NOT NULL UNIQUE,

    -- FileType：文件类型；不能为空；只允许 PDF、EPUB、TXT 三种值
    FileType TEXT NOT NULL CHECK (FileType IN ('PDF', 'EPUB', 'TXT')),

    -- CoverPath：封面图片路径；可以为空；第一版可存默认封面图路径
    CoverPath TEXT,

    -- Category：分类名称；可以为空；为空时可在界面中归到“未分类”
    Category TEXT,

    -- IsFavorite：是否收藏；不能为空；0 表示未收藏，1 表示已收藏；默认 0
    IsFavorite INTEGER NOT NULL DEFAULT 0 CHECK (IsFavorite IN (0, 1)),

    -- ReadProgress：阅读进度；不能为空；范围 0 到 100；默认 0（未读）
    ReadProgress INTEGER NOT NULL DEFAULT 0 CHECK (ReadProgress >= 0 AND ReadProgress <= 100),

    -- LastOpenTime：最近打开时间；可以为空；建议存 ISO 8601 时间文本（如 2026-04-17T14:30:00）
    LastOpenTime TEXT,

    -- AddedTime：导入时间；不能为空；建议存 ISO 8601 时间文本
    AddedTime TEXT NOT NULL,

    -- Description：简介或备注；可以为空
    Description TEXT
);
