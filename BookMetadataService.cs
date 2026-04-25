using System; // 引入基础类型，例如 Exception。
using System.Collections.Generic; // 引入 List 集合，用于处理 EPUB 作者列表。
using System.IO; // 引入 File，用于判断文件是否存在。
using UglyToad.PdfPig; // 引入 PdfPig，用于读取 PDF 文档属性。
using VersOne.Epub; // 引入 EPUB 读取库，用于读取 EPUB 元数据。

namespace BookShelfApp.Services // 定义当前类所在命名空间，表示这是服务层代码。
{ // 命名空间开始。
    public class BookMetadataService // 定义书籍元数据服务类，专门负责读取作者等元数据。
    { // 类开始。
        public string GetAuthorFromFile(string filePath, string fileType) // 根据文件路径和文件类型读取作者。
        { // 方法开始。
            try // 捕获所有异常，避免某个文件元数据异常导致扫描中断。
            { // try 开始。
                if (string.IsNullOrWhiteSpace(filePath)) // 判断文件路径是否为空。
                { // if 开始。
                    return "未知作者"; // 路径为空时返回默认作者。
                } // if 结束。

                if (!File.Exists(filePath)) // 判断文件是否真实存在。
                { // if 开始。
                    return "未知作者"; // 文件不存在时返回默认作者。
                } // if 结束。

                string normalizedFileType = (fileType ?? string.Empty).Trim().ToUpperInvariant(); // 统一文件类型格式，方便判断。

                if (normalizedFileType == "EPUB") // EPUB 优先读取元数据里的作者。
                { // if 开始。
                    string epubAuthor = TryReadEpubAuthor(filePath); // 尝试读取 EPUB 作者。
                    if (!string.IsNullOrWhiteSpace(epubAuthor)) // 判断 EPUB 作者是否有效。
                    { // if 开始。
                        return epubAuthor; // 读取成功就返回 EPUB 作者。
                    } // if 结束。
                } // if 结束。

                if (normalizedFileType == "PDF") // PDF 尝试读取文档属性 Author。
                { // if 开始。
                    string pdfAuthor = TryReadPdfAuthor(filePath); // 尝试读取 PDF 作者。
                    if (!string.IsNullOrWhiteSpace(pdfAuthor)) // 判断 PDF 作者是否有效。
                    { // if 开始。
                        return pdfAuthor; // 读取成功就返回 PDF 作者。
                    } // if 结束。
                } // if 结束。

                return "未知作者"; // TXT 或无法读取作者时返回默认作者。
            } // try 结束。
            catch // 捕获未预料异常。
            { // catch 开始。
                return "未知作者"; // 出错时返回默认作者，保证导入流程稳定。
            } // catch 结束。
        } // 方法结束。

        private string TryReadEpubAuthor(string filePath) // 尝试从 EPUB 元数据中读取作者。
        { // 方法开始。
            try // 捕获 EPUB 读取异常。
            { // try 开始。
                EpubBook epubBook = EpubReader.ReadBook(filePath); // 读取 EPUB 文件。
                if (epubBook == null) // 判断 EPUB 对象是否为空。
                { // if 开始。
                    return string.Empty; // 为空表示读取失败。
                } // if 结束。

                if (epubBook.AuthorList != null && epubBook.AuthorList.Count > 0) // 优先使用 AuthorList 作者列表。
                { // if 开始。
                    List<string> cleanAuthorList = new List<string>(); // 创建清洗后的作者列表。
                    foreach (string authorName in epubBook.AuthorList) // 遍历 EPUB 作者列表。
                    { // foreach 开始。
                        if (!string.IsNullOrWhiteSpace(authorName)) // 只保留有效作者名。
                        { // if 开始。
                            cleanAuthorList.Add(authorName.Trim()); // 去掉首尾空格后加入列表。
                        } // if 结束。
                    } // foreach 结束。

                    if (cleanAuthorList.Count > 0) // 判断清洗后是否还有作者。
                    { // if 开始。
                        return string.Join(", ", cleanAuthorList); // 多个作者用逗号连接后返回。
                    } // if 结束。
                } // if 结束。

                if (!string.IsNullOrWhiteSpace(epubBook.Author)) // 如果 AuthorList 没有值，再尝试 Author 字段。
                { // if 开始。
                    return epubBook.Author.Trim(); // 返回去掉空格后的作者。
                } // if 结束。

                return string.Empty; // 没有读取到作者时返回空字符串。
            } // try 结束。
            catch // 捕获 EPUB 读取失败。
            { // catch 开始。
                return string.Empty; // 返回空字符串表示读取失败。
            } // catch 结束。
        } // 方法结束。

        private string TryReadPdfAuthor(string filePath) // 尝试从 PDF 文档属性中读取作者。
        { // 方法开始。
            try // 捕获 PDF 读取异常。
            { // try 开始。
                using (PdfDocument pdfDocument = PdfDocument.Open(filePath)) // 使用 PdfPig 打开 PDF 文件。
                { // using 开始。
                    if (pdfDocument.Information == null) // 判断 PDF 信息对象是否存在。
                    { // if 开始。
                        return string.Empty; // 没有信息对象就返回空。
                    } // if 结束。

                    string author = pdfDocument.Information.Author; // 读取 PDF 文档属性里的 Author 字段。
                    if (string.IsNullOrWhiteSpace(author)) // 判断作者是否为空。
                    { // if 开始。
                        return string.Empty; // 空作者表示读取失败。
                    } // if 结束。

                    return author.Trim(); // 返回去掉首尾空格后的作者。
                } // using 结束。
            } // try 结束。
            catch // 捕获 PDF 打开或读取失败。
            { // catch 开始。
                return string.Empty; // 返回空字符串表示读取失败。
            } // catch 结束。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
