using System; // 引入 System 命名空间，提供 StringComparison 等基础功能。
using System.IO; // 引入 IO 命名空间，提供 Path.GetExtension 等文件路径工具。

namespace BookShelfApp.Helpers // 定义当前类所在命名空间，表示这是通用帮助类模块。
{ // 命名空间开始。
    public static class FileTypeHelper // 定义静态帮助类，专门处理文件类型识别和判断。
    { // 类开始。
        public static bool IsSupportedBookFile(string filePath) // 定义公共方法，用于判断一个文件路径是否是当前支持的电子书格式。
        { // 方法开始。
            string extension = Path.GetExtension(filePath); // 从完整文件路径中提取扩展名（例如 .pdf）。
            if (string.IsNullOrWhiteSpace(extension)) // 判断扩展名是否为空或空白，空值说明不是有效图书文件。
            { // if 代码块开始。
                return false; // 返回 false，表示不支持。
            } // if 代码块结束。
            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) // 判断是否是 PDF 扩展名（忽略大小写）。
                || extension.Equals(".epub", StringComparison.OrdinalIgnoreCase) // 判断是否是 EPUB 扩展名（忽略大小写）。
                || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase); // 判断是否是 TXT 扩展名（忽略大小写）。
        } // 方法结束。

        public static string GetFileTypeFromPath(string filePath) // 定义公共方法，用于把文件扩展名转换成数据库里统一使用的文件类型值。
        { // 方法开始。
            string extension = Path.GetExtension(filePath); // 提取扩展名作为后续判断依据。
            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) // 判断是否为 PDF 文件。
            { // if 代码块开始。
                return "PDF"; // 返回统一的大写类型值 PDF。
            } // if 代码块结束。
            if (extension.Equals(".epub", StringComparison.OrdinalIgnoreCase)) // 判断是否为 EPUB 文件。
            { // if 代码块开始。
                return "EPUB"; // 返回统一的大写类型值 EPUB。
            } // if 代码块结束。
            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) // 判断是否为 TXT 文件。
            { // if 代码块开始。
                return "TXT"; // 返回统一的大写类型值 TXT。
            } // if 代码块结束。
            return string.Empty; // 如果不是支持的类型，返回空字符串让调用方自行处理。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
