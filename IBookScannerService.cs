using System.Collections.Generic; // 引入泛型集合命名空间，用于使用 List<T> 类型。
using BookShelfApp.Models; // 引入模型命名空间，用于使用 Book 类型。

namespace BookShelfApp.Services // 定义当前接口所在命名空间，表示这是服务层代码。
{ // 命名空间开始。
    public interface IBookScannerService // 定义图书扫描服务接口，用于约束扫描导入功能的方法。
    { // 接口开始。
        List<Book> ScanAndImportBooks(string folderPath); // 定义扫描并导入方法：输入要扫描的文件夹路径，返回本次新导入的图书列表。
    } // 接口结束。
} // 命名空间结束。
