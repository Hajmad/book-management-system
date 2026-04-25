using Docnet.Core; // 引入 Docnet，用于读取 PDF 页面。
using Docnet.Core.Models; // 引入 Docnet 模型，例如 PageDimensions。
using Docnet.Core.Readers;
using SixLabors.ImageSharp; // 引入 ImageSharp 图片处理主命名空间。
using SixLabors.ImageSharp.PixelFormats; // 引入像素格式，例如 Bgra32。
using SixLabors.ImageSharp.Processing; // 引入图片缩放、背景处理等操作。
using System; // 引入基础类型，例如 Guid 和 Exception。
using System.IO; // 引入文件和文件夹操作，例如 File、Directory、Path。
using VersOne.Epub; // 引入 EPUB 读取库，用于读取 EPUB 内置封面。

namespace BookShelfApp.Services // 定义当前类所在命名空间，表示这是服务层代码。
{ // 命名空间开始。
    public class BookCoverService : IBookCoverService // 定义书籍封面服务类，并实现 IBookCoverService 接口。
    { // 类开始。
        private const int CoverMaxWidth = 240; // 定义生成封面的最大宽度，避免图片太大。
        private const int CoverMaxHeight = 320; // 定义生成封面的最大高度，保持接近书籍封面比例。

        public string GenerateCoverPath(string filePath, string fileType) // 根据书籍文件路径和文件类型生成封面路径。
        { // 方法开始。
            try // 捕获封面生成过程中的所有异常，失败时使用默认封面。
            { // try 开始。
                if (string.IsNullOrWhiteSpace(filePath)) // 判断文件路径是否为空。
                { // if 开始。
                    return GetDefaultCoverPath(fileType); // 路径无效时返回默认封面。
                } // if 结束。

                if (!File.Exists(filePath)) // 判断书籍文件是否真实存在。
                { // if 开始。
                    return GetDefaultCoverPath(fileType); // 文件不存在时返回默认封面。
                } // if 结束。

                string normalizedFileType = (fileType ?? string.Empty).Trim().ToUpperInvariant(); // 把文件类型统一转成大写，方便判断。

                if (normalizedFileType == "PDF") // 如果是 PDF 文件。
                { // if 开始。
                    string pdfCoverPath = TryCreatePdfCover(filePath); // 尝试用 PDF 第一页生成封面。
                    if (!string.IsNullOrWhiteSpace(pdfCoverPath)) // 判断 PDF 封面是否生成成功。
                    { // if 开始。
                        return pdfCoverPath; // 成功则返回生成的封面路径。
                    } // if 结束。
                } // if 结束。

                if (normalizedFileType == "EPUB") // 如果是 EPUB 文件。
                { // if 开始。
                    string epubCoverPath = TryCreateEpubCover(filePath); // 尝试读取 EPUB 内置封面。
                    if (!string.IsNullOrWhiteSpace(epubCoverPath)) // 判断 EPUB 封面是否生成成功。
                    { // if 开始。
                        return epubCoverPath; // 成功则返回生成的封面路径。
                    } // if 结束。
                } // if 结束。

                return GetDefaultCoverPath(normalizedFileType); // TXT 或封面提取失败时返回默认封面。
            } // try 结束。
            catch // 捕获任何未预料异常。
            { // catch 开始。
                return GetDefaultCoverPath(fileType); // 出错时返回默认封面，保证导入流程不中断。
            } // catch 结束。
        } // 方法结束。

        public string GetDefaultCoverPath(string fileType) // 根据文件类型返回默认封面路径。
        { // 方法开始。
            string normalizedFileType = (fileType ?? string.Empty).Trim().ToUpperInvariant(); // 统一文件类型格式。

            if (normalizedFileType == "PDF") // 判断是否是 PDF。
            { // if 开始。
                return "Assets/Covers/default_pdf.png"; // 返回 PDF 默认封面。
            } // if 结束。

            if (normalizedFileType == "EPUB") // 判断是否是 EPUB。
            { // if 开始。
                return "Assets/Covers/default_epub.png"; // 返回 EPUB 默认封面。
            } // if 结束。

            if (normalizedFileType == "TXT") // 判断是否是 TXT。
            { // if 开始。
                return "Assets/Covers/default_txt.png"; // 返回 TXT 默认封面。
            } // if 结束。

            return "Assets/Covers/default_txt.png"; // 未知类型默认使用 TXT 封面。
        } // 方法结束。

        private string TryCreatePdfCover(string filePath) // 尝试从 PDF 第一页生成封面图片。
        { // 方法开始。
            try // 捕获 PDF 渲染异常。
            { // try 开始。
                string outputPath = CreateGeneratedCoverPath(filePath); // 生成封面图片要保存到的路径。

                using (IDocReader docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(600, 800))) // 打开 PDF 并指定渲染尺寸。
                { // using 开始。
                    using (IPageReader pageReader = docReader.GetPageReader(0)) // 读取 PDF 第一页，页码从 0 开始。
                    { // using 开始。
                        byte[] rawBytes = pageReader.GetImage(); // 获取第一页渲染后的原始像素数据。
                        int pageWidth = pageReader.GetPageWidth(); // 获取渲染后图片宽度。
                        int pageHeight = pageReader.GetPageHeight(); // 获取渲染后图片高度。

                        using (Image<Bgra32> image = Image.LoadPixelData<Bgra32>(rawBytes, pageWidth, pageHeight)) // 把原始像素数据转换成 ImageSharp 图片对象。
                        { // using 开始。
                            image.Mutate(process => process.BackgroundColor(Color.White)); // 给透明区域补白色背景，避免封面发黑或透明。
                            image.Mutate(process => process.Resize(new ResizeOptions { Size = new Size(CoverMaxWidth, CoverMaxHeight), Mode = ResizeMode.Max })); // 等比例缩小封面。
                            image.SaveAsPng(outputPath); // 把生成的封面保存成 PNG 文件。
                        } // using 结束。
                    } // using 结束。
                } // using 结束。

                return outputPath; // 返回生成成功的封面路径。
            } // try 结束。
            catch // 捕获 PDF 读取或图片保存失败。
            { // catch 开始。
                return string.Empty; // 返回空字符串表示生成失败。
            } // catch 结束。
        } // 方法结束。

        private string TryCreateEpubCover(string filePath) // 尝试从 EPUB 文件读取内置封面。
        { // 方法开始。
            try // 捕获 EPUB 读取异常。
            { // try 开始。
                EpubBook epubBook = EpubReader.ReadBook(filePath); // 读取 EPUB 文件内容。
                if (epubBook.Content == null || epubBook.Content.Cover == null) // 判断 EPUB 是否存在内置封面。
                { // if 开始。
                    return string.Empty; // 没有封面则返回失败。
                } // if 结束。

                byte[] coverBytes = epubBook.Content.Cover.Content; // 读取 EPUB 内置封面的图片字节。
                if (coverBytes == null || coverBytes.Length == 0) // 判断封面字节是否有效。
                { // if 开始。
                    return string.Empty; // 字节无效则返回失败。
                } // if 结束。

                string outputPath = CreateGeneratedCoverPath(filePath); // 生成封面图片保存路径。

                using (Image image = Image.Load(coverBytes)) // 用 ImageSharp 读取 EPUB 内置封面图片。
                { // using 开始。
                    image.Mutate(process => process.Resize(new ResizeOptions { Size = new Size(CoverMaxWidth, CoverMaxHeight), Mode = ResizeMode.Max })); // 等比例缩小封面。
                    image.SaveAsPng(outputPath); // 保存为 PNG 文件。
                } // using 结束。

                return outputPath; // 返回生成成功的封面路径。
            } // try 结束。
            catch // 捕获 EPUB 封面读取或保存失败。
            { // catch 开始。
                return string.Empty; // 返回空字符串表示生成失败。
            } // catch 结束。
        } // 方法结束。

        private string CreateGeneratedCoverPath(string bookFilePath) // 为某本书生成一个唯一的封面保存路径。
        { // 方法开始。
            string coverFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BookShelfApp", "Covers"); // 生成本地封面文件夹路径。
            Directory.CreateDirectory(coverFolderPath); // 确保 Covers 文件夹存在，不存在就自动创建。
            string safeFileName = Path.GetFileNameWithoutExtension(bookFilePath); // 取书籍文件名作为封面文件名基础。
            string uniqueName = safeFileName + "_" + Guid.NewGuid().ToString("N") + ".png"; // 加 Guid 防止同名书籍封面互相覆盖。
            return Path.Combine(coverFolderPath, uniqueName); // 返回完整封面文件路径。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
