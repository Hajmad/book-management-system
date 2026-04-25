using System; // 引入基础类型，例如 object、Type、Uri。
using System.Globalization; // 引入 CultureInfo，IValueConverter 接口需要用到。
using System.IO; // 引入 File 和 Path，用于检查图片文件是否存在。
using System.Windows.Data; // 引入 IValueConverter，用于实现 WPF 绑定转换器。
using System.Windows.Media; // 引入 ImageSource，表示 WPF 图片源类型。
using System.Windows.Media.Imaging; // 引入 BitmapImage，用于加载本地图片文件。

namespace BookShelfApp.Converters // 定义转换器所在命名空间。
{ // 命名空间开始。
    public class CoverImageConverter : IValueConverter // 定义封面图片转换器，把 CoverPath 字符串转换为 ImageSource。
    { // 类开始。
        private const string DefaultCoverRelativePath = "Assets/Covers/default.png"; // 定义默认封面相对路径，封面缺失时使用它。

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) // 正向转换：CoverPath 字符串转 ImageSource。
        { // 方法开始。
            string coverPath = value as string ?? string.Empty; // 从绑定值中取出 CoverPath，如果为空则使用空字符串。
            string imagePath = GetExistingImagePathOrDefault(coverPath); // 获取真实存在的图片路径；如果原路径不存在，则返回默认封面路径。
            return LoadBitmapImage(imagePath); // 把图片路径加载成 BitmapImage 返回给 Image.Source。
        } // 方法结束。

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) // 反向转换：本项目不需要从 ImageSource 转回路径。
        { // 方法开始。
            throw new NotSupportedException(); // 明确表示不支持反向转换。
        } // 方法结束。

        private string GetExistingImagePathOrDefault(string coverPath) // 获取可用图片路径，优先用 CoverPath，失败则用默认封面。
        { // 方法开始。
            if (!string.IsNullOrWhiteSpace(coverPath)) // 判断数据库里的 CoverPath 是否有值。
            { // if 开始。
                if (File.Exists(coverPath)) // 如果 CoverPath 指向的图片真实存在。
                { // if 开始。
                    return coverPath; // 返回真实封面路径。
                } // if 结束。
            } // if 结束。

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory; // 获取程序运行目录，例如 bin\\x64\\Debug\\net10.0-windows。
            string defaultCoverPath = Path.Combine(baseDirectory, DefaultCoverRelativePath); // 拼接默认封面的完整路径。
            if (File.Exists(defaultCoverPath)) // 判断默认封面文件是否存在。
            { // if 开始。
                return defaultCoverPath; // 默认封面存在时返回默认封面路径。
            } // if 结束。

            return string.Empty; // 如果默认封面也不存在，则返回空字符串，后续会显示空白。
        } // 方法结束。

        private ImageSource LoadBitmapImage(string imagePath) // 把图片路径加载成 WPF 可显示的 ImageSource。
        { // 方法开始。
            if (string.IsNullOrWhiteSpace(imagePath)) // 判断图片路径是否为空。
            { // if 开始。
                return null; // 路径为空时返回 null，Image 控件会显示为空。
            } // if 结束。

            BitmapImage bitmapImage = new BitmapImage(); // 创建 BitmapImage 对象。
            bitmapImage.BeginInit(); // 开始初始化图片对象。
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 立即加载图片，避免文件被长期占用。
            bitmapImage.UriSource = new Uri(imagePath, UriKind.Absolute); // 设置图片文件绝对路径。
            bitmapImage.EndInit(); // 结束初始化，图片开始加载。
            bitmapImage.Freeze(); // 冻结图片对象，提高 WPF 跨线程和绑定稳定性。
            return bitmapImage; // 返回加载完成的图片对象。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
