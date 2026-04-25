using System; // 引入 System 命名空间，用于使用 DateTime 类型和 ToString 格式化功能。

namespace BookShelfApp.Helpers // 定义当前类所在命名空间，表示这是通用帮助类模块。
{ // 命名空间开始。
    public static class DateTimeHelper // 定义静态帮助类，专门处理时间格式相关逻辑。
    { // 类开始。
        public static string GetNowIsoString() // 定义公共方法，用于返回“当前时间”的 ISO 字符串格式。
        { // 方法开始。
            DateTime now = DateTime.Now; // 读取当前本地系统时间并保存到变量。
            string isoTimeString = now.ToString("yyyy-MM-ddTHH:mm:ss"); // 把时间格式化为统一字符串（示例：2026-04-18T20:30:45）。
            return isoTimeString; // 返回格式化后的时间字符串，供数据库写入使用。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
