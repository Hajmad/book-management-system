namespace BookShelfApp.Models // 定义当前文件所属命名空间，表示这个类属于数据模型层。
{ // 命名空间开始。
    public class Book // 定义 Book 类，用来表示“一本书”的完整信息。
    { // 类开始。
        public int Id { get; set; } // Id：数据库主键编号，对应 Books 表的 Id 字段。
        public string Title { get; set; } = string.Empty; // Title：书名，对应 Books 表的 Title 字段，默认空字符串避免空引用。
        public string Author { get; set; } = string.Empty; // Author：作者，对应 Books 表的 Author 字段，第一版可先存“未知作者”。
        public string FilePath { get; set; } = string.Empty; // FilePath：电子书文件完整路径，对应 Books 表的 FilePath 字段。
        public string FileType { get; set; } = string.Empty; // FileType：文件类型，对应 Books 表的 FileType 字段（PDF/EPUB/TXT）。
        public string CoverPath { get; set; } = string.Empty; // CoverPath：封面图片路径，对应 Books 表的 CoverPath 字段。
        public string Category { get; set; } = string.Empty; // Category：分类名称，对应 Books 表的 Category 字段。
        public bool IsFavorite { get; set; } // IsFavorite：是否收藏，对应 Books 表的 IsFavorite 字段（0/1 映射为 false/true）。
        public int ReadProgress { get; set; } // ReadProgress：阅读进度，对应 Books 表的 ReadProgress 字段（0 到 100）。
        public string LastOpenTime { get; set; } = string.Empty; // LastOpenTime：最近打开时间，对应 Books 表的 LastOpenTime 字段。
        public string AddedTime { get; set; } = string.Empty; // AddedTime：导入时间，对应 Books 表的 AddedTime 字段。
        public string Description { get; set; } = string.Empty; // Description：简介或备注，对应 Books 表的 Description 字段。
    } // 类结束。
} // 命名空间结束。
