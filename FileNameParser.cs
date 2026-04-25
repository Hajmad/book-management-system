using System.IO; // 引入 IO 命名空间，用于使用 Path 工具类处理文件路径。

namespace BookShelfApp.Helpers // 定义当前类所在命名空间，表示这是通用帮助类模块。
{ // 命名空间开始。
    public static class FileNameParser // 定义静态帮助类，专门处理文件名解析逻辑。
    { // 类开始。
        public static string GetTitleFromFilePath(string filePath) // 定义公共方法，用于把文件路径转换成书名。
        { // 方法开始。
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath); // 从完整文件路径中提取“去掉扩展名后的文件名”。
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension)) // 判断提取出来的文件名是否为空或只有空白字符。
            { // if 代码块开始。
                return "未命名书籍"; // 如果文件名无效，就返回一个默认书名，避免空值进入界面和数据库。
            } // if 代码块结束。
            return fileNameWithoutExtension.Trim(); // 返回去掉首尾空格后的文件名作为书名。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
