using System; // 引入 System 命名空间，用于使用 Exception 和 StringComparer 等基础类型。
using System.Collections.Generic; // 引入泛型集合命名空间，用于使用 List<T>、HashSet<T>、Stack<T>。
using System.IO; // 引入 IO 命名空间，用于使用 Directory、Path 等文件系统功能。
using BookShelfApp.Helpers; // 引入帮助类命名空间，用于使用文件类型、文件名、时间、分类工具。
using BookShelfApp.Models; // 引入模型命名空间，用于使用 Book 类型。
using BookShelfApp.Repositories; // 引入仓储层命名空间，用于使用 IBookRepository。

namespace BookShelfApp.Services // 定义当前类所在命名空间，表示这是服务层实现代码。
{ // 命名空间开始。
    public class BookScannerService : IBookScannerService // 定义扫描服务实现类，并实现 IBookScannerService 接口。
    { // 类开始。
        private readonly IBookRepository _bookRepository; // 定义只读字段，保存仓储对象，用于写入和查询数据库。
        private readonly IBookCoverService _bookCoverService; // 定义只读字段，保存封面服务对象，用于获取默认封面路径。
        private readonly BookMetadataService _bookMetadataService; // 定义书籍元数据服务字段，用于扫描导入时读取作者信息。


        public BookScannerService(IBookRepository bookRepository) // 定义构造函数，通过依赖注入传入仓储实现。
        { // 构造函数开始。
            _bookRepository = bookRepository; // 把传入的仓储对象赋值给私有字段，供后续方法使用。
            _bookCoverService = new BookCoverService(); // 初始化封面服务对象，扫描时复用。
            _bookMetadataService = new BookMetadataService(); // 创建书籍元数据服务对象，后续用于读取 EPUB/PDF 作者。

        } // 构造函数结束。

        public List<Book> ScanAndImportBooks(string folderPath) // 实现扫描并导入方法，输入主文件夹路径，返回本次新导入图书列表。
        { // 方法开始。
            List<Book> importedBookList = new List<Book>(); // 创建空列表，用于收集本次导入成功的图书。
            if (string.IsNullOrWhiteSpace(folderPath)) // 判断传入路径是否为空或空白。
            { // if 代码块开始。
                return importedBookList; // 路径无效时直接返回空结果。
            } // if 代码块结束。

            if (!TryNormalizeAbsolutePath(folderPath, out string normalizedFolderPath)) // 尝试把主目录路径标准化成绝对路径。
            { // if 代码块开始。
                return importedBookList; // 路径标准化失败时直接返回，避免后续路径 API 抛异常。
            } // if 代码块结束。

            try // 对目录存在性检查做异常保护，防止非法路径导致崩溃。
            { // try 代码块开始。
                if (!Directory.Exists(normalizedFolderPath)) // 判断主文件夹是否存在。
                { // if 代码块开始。
                    return importedBookList; // 主文件夹不存在时直接返回空结果。
                } // if 代码块结束。
            } // try 代码块结束。
            catch (Exception) // 捕获目录检查异常。
            { // catch 代码块开始。
                return importedBookList; // 检查失败时返回空结果，保证程序不崩溃。
            } // catch 代码块结束。

            List<string> allBookFilePathList = EnumerateBookFilesSafely(normalizedFolderPath); // 安全递归扫描主文件夹和子文件夹，拿到候选文件列表。
            HashSet<string> handledFilePathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 创建去重集合，避免重复路径导致重复处理。

            foreach (string filePath in allBookFilePathList) // 逐个处理扫描到的候选文件路径。
            { // foreach 代码块开始。
                if (!TryNormalizeAbsolutePath(filePath, out string normalizedFilePath)) // 尝试把路径标准化，避免异常路径进入后续流程。
                { // if 代码块开始。
                    continue; // 标准化失败就跳过当前文件。
                } // if 代码块结束。

                if (!handledFilePathSet.Add(normalizedFilePath)) // 放入去重集合，若返回 false 说明重复。
                { // if 代码块开始。
                    continue; // 已处理过则跳过，防止重复导入。
                } // if 代码块结束。

                try // 单文件异常隔离：一个文件报错不影响整个扫描。
                { // try 代码块开始。
                    if (!FileTypeHelper.IsSupportedBookFile(normalizedFilePath)) // 只允许 PDF/EPUB/TXT。
                    { // if 代码块开始。
                        continue; // 非允许类型直接跳过。
                    } // if 代码块结束。

                    string fileType = FileTypeHelper.GetFileTypeFromPath(normalizedFilePath); // 从扩展名得到统一文件类型文本。

                    if (_bookRepository.ExistsByFilePath(normalizedFilePath)) // 检查数据库是否已有同路径记录。
                    { // if 代码块开始。
                        continue; // 已存在则跳过，避免重复写入。
                    } // if 代码块结束。

                    string title = FileNameParser.GetTitleFromFilePath(normalizedFilePath); // 从文件名提取书名。
                    string category = BookCategoryHelper.GetCategoryByTitle(title); // 按关键词自动分类。
                    string nowIsoTime = DateTimeHelper.GetNowIsoString(); // 获取当前时间字符串。

                    Book newBook = new Book(); // 创建新书对象。
                    newBook.Title = title; // 设置书名。
                    string detectedAuthor = _bookMetadataService.GetAuthorFromFile(normalizedFilePath, fileType); // 从 EPUB/PDF 元数据中尝试读取作者；失败时返回“未知作者”。
                    newBook.Author = detectedAuthor; // 把识别到的作者保存到数据库 Author 字段。
                    newBook.FilePath = normalizedFilePath; // 设置文件完整路径。
                    newBook.FileType = fileType; // 设置文件类型。
                    newBook.CoverPath = _bookCoverService.GenerateCoverPath(normalizedFilePath, fileType); // 自动生成或读取封面；失败时服务内部会返回默认封面。
                    newBook.Category = category; // 设置内容分类。
                    newBook.IsFavorite = false; // 默认未收藏。
                    newBook.ReadProgress = 0; // 默认阅读进度 0。
                    newBook.LastOpenTime = string.Empty; // 默认最近打开时间为空。
                    newBook.AddedTime = nowIsoTime; // 设置导入时间。
                    newBook.Description = string.Empty; // 默认简介为空。

                    _bookRepository.InsertBook(newBook); // 写入数据库。
                    importedBookList.Add(newBook); // 加入本次导入结果列表。
                } // try 代码块结束。
                catch (UnauthorizedAccessException) // 无权限访问文件时捕获。
                { // catch 代码块开始。
                    continue; // 跳过当前文件继续。
                } // catch 代码块结束。
                catch (IOException) // IO 错误（占用/读写失败等）时捕获。
                { // catch 代码块开始。
                    continue; // 跳过当前文件继续。
                } // catch 代码块结束。
                catch // 兜底捕获其他未预期异常。
                { // catch 代码块开始。
                    continue; // 保证扫描不中断。
                } // catch 代码块结束。
            } // foreach 代码块结束。


            return importedBookList; // 返回本次成功导入的图书列表。
        } // 方法结束。



        private List<string> EnumerateBookFilesSafely(string rootFolderPath) // 定义私有方法：安全递归扫描主目录与所有子目录中的图书文件。
        { // 方法开始。
            List<string> resultFilePathList = new List<string>(); // 创建结果列表，用于保存所有支持格式文件路径。
            Stack<string> pendingFolderStack = new Stack<string>(); // 创建待处理目录栈，用于手动递归遍历目录树。
            HashSet<string> visitedFolderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 创建已访问目录集合，防止重复遍历同一路径。

            if (!TryNormalizeAbsolutePath(rootFolderPath, out string normalizedRootPath)) // 尝试标准化主目录路径。
            { // if 代码块开始。
                return resultFilePathList; // 主目录路径无效时返回空结果。
            } // if 代码块结束。

            pendingFolderStack.Push(normalizedRootPath); // 把主目录压栈，作为递归扫描起点。

            while (pendingFolderStack.Count > 0) // 当还有待处理目录时持续循环。
            { // while 代码块开始。
                string currentFolderPath = pendingFolderStack.Pop(); // 弹出一个待处理目录作为当前目录。
                if (visitedFolderSet.Contains(currentFolderPath)) // 判断当前目录是否已经处理过。
                { // if 代码块开始。
                    continue; // 已处理过则跳过，避免重复扫描。
                } // if 代码块结束。
                visitedFolderSet.Add(currentFolderPath); // 把当前目录标记为已访问。

                string[] filePathArrayInCurrentFolder; // 定义变量用于保存当前目录中的文件列表。
                try // 尝试读取当前目录文件，防止无权限目录导致崩溃。
                { // try 代码块开始。
                    filePathArrayInCurrentFolder = Directory.GetFiles(currentFolderPath, "*.*", SearchOption.TopDirectoryOnly); // 只读取当前目录文件。
                } // try 代码块结束。
                catch (Exception) // 捕获目录访问异常（例如权限不足、路径失效等）。
                { // catch 代码块开始。
                    continue; // 当前目录读取失败时跳过整个目录，继续扫描其他目录。
                } // catch 代码块结束。

                foreach (string filePath in filePathArrayInCurrentFolder) // 遍历当前目录读取到的每个文件。
                { // foreach 代码块开始。
                    if (!TryNormalizeAbsolutePath(filePath, out string normalizedFilePath)) // 尝试标准化文件路径。
                    { // if 代码块开始。
                        continue; // 路径异常则跳过当前文件。
                    } // if 代码块结束。

                    if (FileTypeHelper.IsSupportedBookFile(normalizedFilePath)) // 判断文件是否是支持的书籍格式。
                    { // if 代码块开始。
                        resultFilePathList.Add(normalizedFilePath); // 支持格式则加入结果列表。
                    } // if 代码块结束。
                } // foreach 代码块结束。

                string[] subFolderPathArray; // 定义变量用于保存当前目录中的子目录列表。
                try // 尝试读取当前目录子目录，防止某些子目录无权限导致崩溃。
                { // try 代码块开始。
                    subFolderPathArray = Directory.GetDirectories(currentFolderPath, "*", SearchOption.TopDirectoryOnly); // 只读取当前层子目录。
                } // try 代码块结束。
                catch (Exception) // 捕获读取子目录异常（例如权限不足、目录被删除等）。
                { // catch 代码块开始。
                    continue; // 当前目录子目录读取失败时跳过，继续扫描其他目录。
                } // catch 代码块结束。

                foreach (string subFolderPath in subFolderPathArray) // 遍历当前目录下的每个子目录路径。
                { // foreach 代码块开始。
                    if (!TryNormalizeAbsolutePath(subFolderPath, out string normalizedSubFolderPath)) // 尝试标准化子目录路径。
                    { // if 代码块开始。
                        continue; // 路径异常则跳过当前子目录。
                    } // if 代码块结束。

                    if (!visitedFolderSet.Contains(normalizedSubFolderPath)) // 判断该子目录是否尚未访问。
                    { // if 代码块开始。
                        pendingFolderStack.Push(normalizedSubFolderPath); // 未访问则压栈，后续循环继续扫描它。
                    } // if 代码块结束。
                } // foreach 代码块结束。
            } // while 代码块结束。

            return resultFilePathList; // 返回安全递归扫描得到的全部支持格式文件路径列表。
        } // 方法结束。

        private static bool TryNormalizeAbsolutePath(string inputPath, out string normalizedPath) // 定义路径标准化辅助方法：把输入路径安全转换为绝对路径。
        { // 方法开始。
            normalizedPath = string.Empty; // 先给输出参数赋默认值，避免未赋值使用。
            if (string.IsNullOrWhiteSpace(inputPath)) // 判断输入路径是否为空或空白。
            { // if 代码块开始。
                return false; // 输入无效则返回 false。
            } // if 代码块结束。

            try // 尝试执行路径标准化，捕获非法路径异常。
            { // try 代码块开始。
                normalizedPath = Path.GetFullPath(inputPath); // 把路径转换为绝对路径并输出。
                return true; // 转换成功返回 true。
            } // try 代码块结束。
            catch (Exception) // 捕获路径转换过程中出现的所有异常。
            { // catch 代码块开始。
                return false; // 转换失败返回 false。
            } // catch 代码块结束。
        } // 方法结束。



    } // 类结束。
} // 命名空间结束。
