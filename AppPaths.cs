using System; // 引入 System 命名空间，用于访问 Environment 等基础功能。
using System.IO; // 引入 IO 命名空间，用于路径拼接和文件夹操作。

namespace BookShelfApp.Config // 定义当前文件所属命名空间，表示这是项目的配置模块。
{ // 命名空间开始。
    public static class AppPaths // 定义静态类 AppPaths，用来统一管理项目里会用到的路径。
    { // 类开始。
        public static string DatabaseFilePath // 定义只读属性，返回 SQLite 数据库文件的完整路径。
        { // 属性开始。
            get // 定义属性的读取逻辑，每次读取都会执行这里的代码。
            { // get 访问器开始。
                string appDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BookShelfApp"); // 把系统“本地应用数据目录”和“BookShelfApp”拼接成项目专用目录。
                if (!Directory.Exists(appDataFolderPath)) // 判断这个目录是否已经存在，不存在才需要创建。
                { // if 代码块开始。
                    Directory.CreateDirectory(appDataFolderPath); // 创建目录，确保后续数据库文件有地方保存。
                } // if 代码块结束。
                string databaseFilePath = Path.Combine(appDataFolderPath, "bookshelf.db"); // 把目录路径和数据库文件名拼接成完整数据库路径。
                return databaseFilePath; // 返回数据库路径给调用方使用。
            } // get 访问器结束。
        } // 属性结束。
    } // 类结束。
} // 命名空间结束。
