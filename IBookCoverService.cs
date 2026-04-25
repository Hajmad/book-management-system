namespace BookShelfApp.Services // 定义当前接口所在命名空间，表示这是服务层代码。
{ // 命名空间开始。
    public interface IBookCoverService // 定义封面服务接口，用于约束“根据文件类型返回封面路径”的行为。
    { // 接口开始。
        string GetDefaultCoverPath(string fileType); // 定义方法：输入文件类型（PDF/EPUB/TXT），返回对应默认封面路径。
        string GenerateCoverPath(string filePath, string fileType); // 声明自动生成封面路径的方法，PDF/EPUB 会尝试提取封面，失败时返回默认封面。

    } // 接口结束。
} // 命名空间结束。
