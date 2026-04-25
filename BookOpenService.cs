using System; // 引入 System 命名空间，提供 Exception 等基础类型。
using System.Diagnostics; // 引入诊断命名空间，提供 ProcessStartInfo 和 Process。
using System.IO; // 引入 IO 命名空间，提供 File.Exists 方法。

namespace BookShelfApp.Services // 定义当前类所在命名空间，表示这是服务层实现代码。
{ // 命名空间开始。
    public class BookOpenService : IBookOpenService // 定义打开服务实现类，并实现 IBookOpenService 接口。
    { // 类开始。
        public bool OpenBookFile(string filePath) // 实现打开文件方法，输入文件路径，返回是否成功。
        { // 方法开始。
            if (string.IsNullOrWhiteSpace(filePath)) // 先判断路径是否为空或空白，避免传入非法参数。
            { // if 代码块开始。
                return false; // 路径无效时直接返回 false。
            } // if 代码块结束。

            if (!File.Exists(filePath)) // 判断目标文件是否存在，防止打开不存在的文件。
            { // if 代码块开始。
                return false; // 文件不存在时返回 false。
            } // if 代码块结束。

            try // 使用 try-catch 捕获系统调用异常，避免程序崩溃。
            { // try 代码块开始。
                ProcessStartInfo processStartInfo = new ProcessStartInfo(); // 创建进程启动配置对象。
                processStartInfo.FileName = filePath; // 指定要打开的目标文件路径。
                processStartInfo.UseShellExecute = true; // 设为 true 表示使用 Windows Shell，让系统按默认关联程序打开文件。
                Process.Start(processStartInfo); // 发起打开操作。
                return true; // 如果没有抛异常，返回 true 表示调用成功。
            } // try 代码块结束。
            catch (Exception) // 捕获所有异常，第一版先统一返回失败。
            { // catch 代码块开始。
                return false; // 发生异常时返回 false。
            } // catch 代码块结束。
        } // 方法结束。
    } // 类结束。
} // 命名空间结束。
