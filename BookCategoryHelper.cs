using System; // 引入 System 命名空间，用于使用 StringComparison 等基础功能。

namespace BookShelfApp.Helpers // 定义当前文件所属命名空间，表示这是帮助类模块。
{ // 命名空间开始。
    public static class BookCategoryHelper // 定义静态帮助类，专门负责“按书名自动判断内容分类”。
    { // 类开始。
        private static readonly string[] ElectronicsKeywords = new string[] // 定义“电子信息类”关键词数组。
        { // 数组开始。
            "单片机", // 关键词：单片机。
            "STM32", // 关键词：STM32。
            "51单片机", // 关键词：51单片机。
            "模电", // 关键词：模电。
            "数电", // 关键词：数电。
            "电路", // 关键词：电路。
            "电子", // 关键词：电子。
            "电工", // 关键词：电工。
            "通信", // 关键词：通信。
            "信号", // 关键词：信号。
            "嵌入式", // 关键词：嵌入式。
            "FPGA", // 关键词：FPGA。
            "传感器", // 关键词：传感器。
            "自动控制", // 关键词：自动控制。
            "PLC" // 关键词：PLC。
        }; // 数组结束。

        private static readonly string[] ComputerKeywords = new string[] // 定义“计算机类”关键词数组。
        { // 数组开始。
            "C语言", // 关键词：C语言。
            "C++", // 关键词：C++。
            "Java", // 关键词：Java。
            "Python", // 关键词：Python。
            "算法", // 关键词：算法。
            "数据结构", // 关键词：数据结构。
            "操作系统", // 关键词：操作系统。
            "数据库", // 关键词：数据库。
            "计算机", // 关键词：计算机。
            "编程", // 关键词：编程。
            "网络", // 关键词：网络。
            "前端", // 关键词：前端。
            "后端", // 关键词：后端。
            "Android", // 关键词：Android。
            "软件工程" // 关键词：软件工程。
        }; // 数组结束。

        private static readonly string[] LiteratureKeywords = new string[] // 定义“文学类”关键词数组。
        { // 数组开始。
            "小说", // 关键词：小说。
            "散文", // 关键词：散文。
            "诗歌", // 关键词：诗歌。
            "文学", // 关键词：文学。
            "名著", // 关键词：名著。
            "故事", // 关键词：故事。
            "随笔", // 关键词：随笔。
            "诗词", // 关键词：诗词。
            "红楼梦", // 关键词：红楼梦。
            "西游记", // 关键词：西游记。
            "三国演义", // 关键词：三国演义。
            "水浒传" // 关键词：水浒传。
        }; // 数组结束。

        private static readonly string[] HistoryPoliticsLawKeywords = new string[] // 定义“历史政治法律类”关键词数组。
        { // 数组开始。
            "历史", // 关键词：历史。
            "政治", // 关键词：政治。
            "法律", // 关键词：法律。
            "法学", // 关键词：法学。
            "宪法", // 关键词：宪法。
            "民法", // 关键词：民法。
            "刑法", // 关键词：刑法。
            "中国史", // 关键词：中国史。
            "世界史", // 关键词：世界史。
            "党史", // 关键词：党史。
            "马克思", // 关键词：马克思。
            "哲学", // 关键词：哲学。
            "国际关系" // 关键词：国际关系。
        }; // 数组结束。

        public static string GetCategoryByTitle(string title) // 定义公共方法：输入书名，输出内容分类名称。
        { // 方法开始。
            if (string.IsNullOrWhiteSpace(title)) // 判断书名是否为空或空白。
            { // if 代码块开始。
                return "未分类"; // 如果书名无效，直接返回“未分类”。
            } // if 代码块结束。

            string normalizedTitle = title.Trim(); // 去除书名首尾空格，避免匹配误差。

            if (ContainsAnyKeyword(normalizedTitle, ElectronicsKeywords)) // 按规则第 1 优先级匹配“电子信息类”。
            { // if 代码块开始。
                return "电子信息类"; // 匹配成功返回“电子信息类”。
            } // if 代码块结束。

            if (ContainsAnyKeyword(normalizedTitle, ComputerKeywords)) // 按规则第 2 优先级匹配“计算机类”。
            { // if 代码块开始。
                return "计算机类"; // 匹配成功返回“计算机类”。
            } // if 代码块结束。

            if (ContainsAnyKeyword(normalizedTitle, LiteratureKeywords)) // 按规则第 3 优先级匹配“文学类”。
            { // if 代码块开始。
                return "文学类"; // 匹配成功返回“文学类”。
            } // if 代码块结束。

            if (ContainsAnyKeyword(normalizedTitle, HistoryPoliticsLawKeywords)) // 按规则第 4 优先级匹配“历史政治法律类”。
            { // if 代码块开始。
                return "历史政治法律类"; // 匹配成功返回“历史政治法律类”。
            } // if 代码块结束。

            return "未分类"; // 如果全部都不匹配，按规则返回“未分类”。
        } // 方法结束。

        private static bool ContainsAnyKeyword(string text, string[] keywords) // 定义私有方法：判断文本是否包含数组中任一关键词。
        { // 方法开始。
            foreach (string keyword in keywords) // 循环遍历每一个关键词。
            { // foreach 代码块开始。
                if (string.IsNullOrWhiteSpace(keyword)) // 判断当前关键词是否为空或空白。
                { // if 代码块开始。
                    continue; // 关键词无效时跳过当前项，继续下一个。
                } // if 代码块结束。

                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase)) // 忽略大小写判断文本是否包含该关键词。
                { // if 代码块开始。
                    return true; // 只要命中一个关键词，就返回 true。
                } // if 代码块结束。
            } // foreach 代码块结束。

            return false; // 全部关键词都没命中时返回 false。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
