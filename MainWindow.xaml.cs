using BookShelfApp.Helpers;
using BookShelfApp.Models; // 引入模型命名空间，用于使用 Book 实体类。
using BookShelfApp.Repositories; // 引入仓储命名空间，用于使用 IBookRepository 与 BookRepository。
using BookShelfApp.Services; // 引入服务命名空间，用于使用扫描服务与打开服务。
using System; // 引入 System 命名空间，提供 EventArgs、StringComparison、DateTime 等基础类型和功能。
using System.Collections.Generic; // 引入泛型集合命名空间，提供 List<T> 类型。
using System.Diagnostics;
using System.Windows; // 引入 WPF 核心命名空间，提供 Window、MessageBox、RoutedEventArgs 等类型。
using System.Windows.Controls; // 引入 WPF 控件命名空间，提供 ListBoxItem、ComboBoxItem、SelectionChangedEventArgs 等类型。
using System.Windows.Input; // 引入输入命名空间，提供 KeyEventArgs、MouseButtonEventArgs、Key 枚举等类型。
using WinForms = System.Windows.Forms; // 给 Windows Forms 命名空间起别名，专门用于 FolderBrowserDialog 文件夹选择对话框。
using System.IO; // 引入文件与路径相关 API，File.Exists 需要它。
using System.IO; // 引入 IO 命名空间，用于 File.Exists、Path.GetDirectoryName、Directory.Exists。



namespace BookShelfApp // 定义当前主窗口类所在命名空间。
{ // 命名空间开始。
    public partial class MainWindow : Window // 定义主窗口类，继承 WPF 的 Window。
    { // 类开始。
        private readonly IBookRepository _bookRepository; // 定义只读字段：图书仓储对象，用于数据库查询和更新。
        private readonly IBookScannerService _bookScannerService; // 定义只读字段：扫描服务对象，用于扫描目录并导入图书。
        private readonly IBookOpenService _bookOpenService; // 定义只读字段：打开服务对象，用于调用系统默认程序打开书籍文件。
        private readonly CategoryRepository _categoryRepository; // 定义树形分类仓储字段，用于从 Categories 表读取主分类和子分类。
        private List<Book> _currentBookList; // 定义字段：保存当前界面正在显示的图书列表。
        private readonly IBookCoverService _bookCoverService; // 定义封面服务字段，用于手动修复缺失封面。


        public MainWindow() // 定义主窗口构造函数，窗口创建时会执行这里。
        { // 构造函数开始。
            InitializeComponent(); // 先初始化 XAML 控件，这样界面元素对象才会创建出来。

            _bookRepository = new BookRepository(); // 先创建仓储对象，后续动态分类加载会依赖它。
            _bookScannerService = new BookScannerService(_bookRepository); // 创建扫描服务并注入仓储对象。
            _bookOpenService = new BookOpenService(); // 创建打开文件服务对象。
            _currentBookList = new List<Book>(); // 初始化当前图书列表，避免空引用。
            _categoryRepository = new CategoryRepository(); // 创建树形分类仓储对象，用于加载左侧内容分类树。
            _bookCoverService = new BookCoverService(); // 创建封面服务对象，用于重新生成 PDF/EPUB/TXT 封面。



            InitializeDynamicCategoryUi(); // 在仓储初始化完成后再加载动态分类 UI，避免空引用异常。
            BindEvents(); // 绑定按钮、列表等事件处理函数。

            LoadContentCategoryTree(); // 从 Categories 表读取内容分类，并显示到左侧 TreeView。
            LoadAllBooks(); // 加载全部图书并刷新界面。
        } // 构造函数结束。

        private void LoadContentCategoryTree() // 加载左侧内容分类 TreeView。
        { // 方法开始。
            ContentCategoryTreeView.Items.Clear(); // 清空旧的树节点，避免重复加载。

            List<Category> rootCategoryList = _categoryRepository.GetAllCategories(); // 从数据库读取所有主分类和子分类。

            foreach (Category rootCategory in rootCategoryList) // 遍历每一个主分类。
            { // foreach 开始。
                TreeViewItem rootTreeViewItem = new TreeViewItem(); // 创建主分类树节点。
                int rootBookCount = _categoryRepository.GetBookCountByCategoryName(rootCategory.Name); // 先统计直接放在主分类下面的书籍数量。
                foreach (Category childCategoryForCount in rootCategory.Children) // 遍历主分类下面的每一个子分类，用于累加子分类书籍数量。
                { // foreach 开始。
                    rootBookCount += _categoryRepository.GetBookCountByCategoryName(childCategoryForCount.Name); // 把子分类中的书籍数量加到主分类总数里。
                } // foreach 结束。
                rootTreeViewItem.Header = rootCategory.Name + "(" + rootBookCount + ")"; // 主分类显示总数量：主分类自身数量 + 子分类数量。

                rootTreeViewItem.Tag = rootCategory; // 把完整 Category 对象存到 Tag，点击时方便取出分类名。
                rootTreeViewItem.IsExpanded = true; // 默认展开主分类，方便初学者看到子分类。

                foreach (Category childCategory in rootCategory.Children) // 遍历当前主分类下面的子分类。
                { // foreach 开始。
                    TreeViewItem childTreeViewItem = new TreeViewItem(); // 创建子分类树节点。
                    int childBookCount = _categoryRepository.GetBookCountByCategoryName(childCategory.Name); // 统计当前子分类下直接包含的书籍数量。
                    childTreeViewItem.Header = childCategory.Name + "(" + childBookCount + ")"; // 设置子分类显示文字，格式为“分类名(数量)”。

                    childTreeViewItem.Tag = childCategory; // 把完整 Category 对象存到 Tag。
                    rootTreeViewItem.Items.Add(childTreeViewItem); // 把子分类节点加入主分类节点下面。
                } // foreach 结束。

                ContentCategoryTreeView.Items.Add(rootTreeViewItem); // 把主分类节点加入 TreeView。
            } // foreach 结束。
        } // 方法结束。


        private void LoadBooksByContentCategory(string categoryName) // 按内容分类名称加载右侧书籍列表。
        { // 方法开始。
            string cleanCategoryName = (categoryName ?? string.Empty).Trim(); // 清理分类名首尾空格。
            if (string.IsNullOrWhiteSpace(cleanCategoryName)) // 判断分类名是否为空。
            { // if 开始。
                BooksListView.ItemsSource = new List<Book>(); // 分类名无效时显示空列表。
                return; // 结束方法。
            } // if 结束。

            string searchKeyword = SearchTextBox.Text == null ? string.Empty : SearchTextBox.Text.Trim(); // 读取当前搜索框关键字，保留搜索过滤能力。

            List<Book> allBookList = _bookRepository.GetAllBooks(); // 从数据库读取全部书籍。

            List<Book> filteredBookList = new List<Book>(); // 创建筛选后的书籍列表。
            foreach (Book book in allBookList) // 遍历每一本书。
            { // foreach 开始。
                bool categoryMatched = string.Equals(book.Category, cleanCategoryName, StringComparison.OrdinalIgnoreCase); // 判断书籍分类是否等于当前内容分类。
                if (!categoryMatched) // 如果分类不匹配。
                { // if 开始。
                    continue; // 跳过这本书。
                } // if 结束。

                if (!string.IsNullOrWhiteSpace(searchKeyword)) // 如果搜索框里有关键字。
                { // if 开始。
                    bool titleMatched = book.Title != null && book.Title.IndexOf(searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0; // 判断书名是否包含搜索关键字。
                    if (!titleMatched) // 如果书名不匹配搜索关键字。
                    { // if 开始。
                        continue; // 跳过这本书。
                    } // if 结束。
                } // if 结束。

                filteredBookList.Add(book); // 分类和搜索都匹配时加入结果列表。
            } // foreach 结束。

            BooksListView.ItemsSource = filteredBookList; // 把筛选结果显示到右侧书籍列表。
        } // 方法结束。



        private string ShowCategoryNameInputDialog(string dialogTitle) // 显示一个简单输入框，让用户输入分类名称。
        { // 方法开始。
            Window inputWindow = new Window(); // 创建一个新的小窗口作为输入对话框。
            inputWindow.Title = dialogTitle; // 设置输入窗口标题，例如“新增主分类”。
            inputWindow.Width = 360; // 设置窗口宽度。
            inputWindow.Height = 160; // 设置窗口高度。
            inputWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // 让输入窗口显示在主窗口中间。
            inputWindow.Owner = this; // 设置主窗口为输入窗口的拥有者。
            inputWindow.ResizeMode = ResizeMode.NoResize; // 禁止用户调整输入窗口大小。

            Grid rootGrid = new Grid(); // 创建输入窗口根布局。
            rootGrid.Margin = new Thickness(12); // 设置窗口内容边距。

            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 第 0 行放提示文字。
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 第 1 行放输入框。
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 第 2 行放按钮。

            TextBlock promptTextBlock = new TextBlock(); // 创建提示文字控件。
            promptTextBlock.Text = "请输入分类名称："; // 设置提示文字。
            promptTextBlock.Margin = new Thickness(0, 0, 0, 8); // 设置提示文字下方间距。
            Grid.SetRow(promptTextBlock, 0); // 把提示文字放到第 0 行。
            rootGrid.Children.Add(promptTextBlock); // 把提示文字加入窗口布局。

            TextBox categoryNameTextBox = new TextBox(); // 创建分类名称输入框。
            categoryNameTextBox.Height = 28; // 设置输入框高度。
            categoryNameTextBox.VerticalContentAlignment = VerticalAlignment.Center; // 让输入文字垂直居中。
            Grid.SetRow(categoryNameTextBox, 1); // 把输入框放到第 1 行。
            rootGrid.Children.Add(categoryNameTextBox); // 把输入框加入窗口布局。

            StackPanel buttonPanel = new StackPanel(); // 创建底部按钮容器。
            buttonPanel.Orientation = Orientation.Horizontal; // 设置按钮横向排列。
            buttonPanel.HorizontalAlignment = HorizontalAlignment.Right; // 让按钮靠右显示。
            buttonPanel.Margin = new Thickness(0, 12, 0, 0); // 设置按钮区域上方间距。
            Grid.SetRow(buttonPanel, 2); // 把按钮区域放到第 2 行。
            rootGrid.Children.Add(buttonPanel); // 把按钮区域加入窗口布局。

            Button okButton = new Button(); // 创建确定按钮。
            okButton.Content = "确定"; // 设置确定按钮文字。
            okButton.Width = 72; // 设置确定按钮宽度。
            okButton.Height = 28; // 设置确定按钮高度。
            okButton.Margin = new Thickness(0, 0, 8, 0); // 设置确定按钮右侧间距。
            buttonPanel.Children.Add(okButton); // 把确定按钮加入按钮区域。

            Button cancelButton = new Button(); // 创建取消按钮。
            cancelButton.Content = "取消"; // 设置取消按钮文字。
            cancelButton.Width = 72; // 设置取消按钮宽度。
            cancelButton.Height = 28; // 设置取消按钮高度。
            buttonPanel.Children.Add(cancelButton); // 把取消按钮加入按钮区域。

            string result = string.Empty; // 保存用户最终输入的分类名称。
            okButton.Click += delegate // 绑定确定按钮点击事件。
            { // 匿名方法开始。
                result = categoryNameTextBox.Text == null ? string.Empty : categoryNameTextBox.Text.Trim(); // 读取输入框内容并去掉首尾空格。
                inputWindow.DialogResult = true; // 设置对话框结果为确认。
                inputWindow.Close(); // 关闭输入窗口。
            }; // 匿名方法结束。

            cancelButton.Click += delegate // 绑定取消按钮点击事件。
            { // 匿名方法开始。
                inputWindow.DialogResult = false; // 设置对话框结果为取消。
                inputWindow.Close(); // 关闭输入窗口。
            }; // 匿名方法结束。

            inputWindow.Content = rootGrid; // 把根布局设置为输入窗口的内容，确保输入框真的显示在窗口里。
            inputWindow.Loaded += delegate // 等窗口加载完成后再设置输入框焦点。
            { // 匿名方法开始。
                categoryNameTextBox.Focus(); // 让输入框获得键盘输入焦点。
            }; // 匿名方法结束。

            bool? dialogResult = inputWindow.ShowDialog(); // 以模态方式显示输入窗口，等待用户点击确定或取消。

            if (dialogResult == true) // 判断用户是否点击了确定。
            { // if 开始。
                return result; // 返回用户输入的分类名。
            } // if 结束。

            return string.Empty; // 用户取消时返回空字符串。
        } // 方法结束。


        private void AddRootCategoryButton_Click(object sender, RoutedEventArgs e) // 处理“新增主分类”按钮点击事件。
        { // 方法开始。
            string categoryName = ShowCategoryNameInputDialog("新增主分类"); // 弹出输入框，让用户输入主分类名称。
            if (string.IsNullOrWhiteSpace(categoryName)) // 判断分类名是否为空。
            { // if 开始。
                System.Windows.MessageBox.Show("分类名称不能为空。"); // 提示用户分类名不能为空。
                return; // 结束方法，不继续新增。
            } // if 结束。

            categoryName = categoryName.Trim(); // 再次去掉分类名称前后空格，确保保存到数据库的是干净名称。
            if (_categoryRepository.CategoryNameExists(categoryName, null)) // 检查主分类层级下是否已经存在同名分类。
            { // if 开始。
                System.Windows.MessageBox.Show("分类名称已存在，请换一个名称"); // 提示用户换一个分类名。
                return; // 发现重复后停止新增。
            } // if 结束。


            try // 捕获数据库新增过程中的异常，例如重名。
            { // try 开始。
                _categoryRepository.AddRootCategory(categoryName); // 调用仓储方法，新增 ParentId 为空的主分类。
                LoadContentCategoryTree(); // 新增成功后重新加载 TreeView，让界面立即显示新分类。
                System.Windows.MessageBox.Show("新增主分类成功。"); // 提示用户新增成功。
            } // try 结束。
            catch (Exception ex) // 捕获新增失败异常。
            { // catch 开始。
                System.Windows.MessageBox.Show("新增主分类失败：\n" + ex.Message); // 显示失败原因，例如分类重复。
            } // catch 结束。
        } // 方法结束。


        private void AddChildCategoryButton_Click(object sender, RoutedEventArgs e) // 处理“新增子分类”按钮点击事件。
        { // 方法开始。
            TreeViewItem selectedTreeViewItem = ContentCategoryTreeView.SelectedItem as TreeViewItem; // 获取当前选中的树节点。
            if (selectedTreeViewItem == null) // 判断是否选中了 TreeView 节点。
            { // if 开始。
                System.Windows.MessageBox.Show("请先选中一个主分类。"); // 没选中时提示用户先选主分类。
                return; // 结束方法。
            } // if 结束。

            Category selectedCategory = selectedTreeViewItem.Tag as Category; // 从树节点 Tag 中取出分类对象。
            if (selectedCategory == null) // 判断分类对象是否有效。
            { // if 开始。
                System.Windows.MessageBox.Show("当前选中的分类无效。"); // 提示分类无效。
                return; // 结束方法。
            } // if 结束。

            if (selectedCategory.ParentId != null) // 如果选中的是子分类，而不是主分类。
            { // if 开始。
                System.Windows.MessageBox.Show("请选中主分类后再新增子分类。"); // 当前版本只允许主分类下面新增一级子分类。
                return; // 结束方法。
            } // if 结束。

            string childCategoryName = ShowCategoryNameInputDialog("新增子分类"); // 弹出输入框，让用户输入子分类名称。
            if (string.IsNullOrWhiteSpace(childCategoryName)) // 判断子分类名称是否为空。
            { // if 开始。
                System.Windows.MessageBox.Show("分类名称不能为空。"); // 提示分类名不能为空。
                return; // 结束方法。
            } // if 结束。

            childCategoryName = childCategoryName.Trim(); // 去掉子分类名称前后空格，保证保存和判断都使用干净名称。
            if (_categoryRepository.CategoryNameExists(childCategoryName, selectedCategory.Id)) // 检查当前主分类下面是否已有同名子分类。
            { // if 开始。
                System.Windows.MessageBox.Show("分类名称已存在，请换一个名称"); // 提示用户同级分类已存在。
                return; // 发现重复后停止新增。
            } // if 结束。


            try // 捕获数据库新增过程中的异常，例如同一主分类下重名。
            { // try 开始。
                _categoryRepository.AddChildCategory(childCategoryName, selectedCategory.Id); // 调用仓储方法，在选中的主分类下面新增子分类。
                LoadContentCategoryTree(); // 新增成功后刷新 TreeView。
                System.Windows.MessageBox.Show("新增子分类成功。"); // 提示用户新增成功。
            } // try 结束。
            catch (Exception ex) // 捕获新增失败异常。
            { // catch 开始。
                System.Windows.MessageBox.Show("新增子分类失败：\n" + ex.Message); // 显示失败原因。
            } // catch 结束。
        } // 方法结束。

        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e) // 处理“删除分类”按钮点击事件。
        { // 方法开始。
            TreeViewItem selectedTreeViewItem = ContentCategoryTreeView.SelectedItem as TreeViewItem; // 获取当前选中的 TreeView 节点。
            if (selectedTreeViewItem == null) // 判断是否选中了分类节点。
            { // if 开始。
                System.Windows.MessageBox.Show("请先选中要删除的分类。"); // 未选中时提示用户。
                return; // 结束方法。
            } // if 结束。

            Category selectedCategory = selectedTreeViewItem.Tag as Category; // 从树节点 Tag 中取出分类对象。
            if (selectedCategory == null) // 判断分类对象是否有效。
            { // if 开始。
                System.Windows.MessageBox.Show("当前选中的分类无效。"); // 分类对象无效时提示。
                return; // 结束方法。
            } // if 结束。

            if (_categoryRepository.HasChildCategories(selectedCategory.Id)) // 检查该分类下面是否还有子分类。
            { // if 开始。
                System.Windows.MessageBox.Show("请先删除子分类。"); // 有子分类时不允许直接删除。
                return; // 结束方法。
            } // if 结束。

            if (_categoryRepository.HasBooksInCategory(selectedCategory.Name)) // 检查该分类下面是否还有书籍。
            { // if 开始。
                System.Windows.MessageBox.Show("该分类下还有书籍，不能删除。"); // 有书籍时不允许删除。
                return; // 结束方法。
            } // if 结束。

            MessageBoxResult confirmResult = System.Windows.MessageBox.Show( // 弹出删除确认框，避免误删。
                "确定要删除分类吗？\n\n分类：" + selectedCategory.Name, // 提示用户当前要删除的分类名称。
                "确认删除分类", // 对话框标题。
                MessageBoxButton.YesNo, // 显示“是/否”按钮。
                MessageBoxImage.Warning); // 使用警告图标。

            if (confirmResult != MessageBoxResult.Yes) // 判断用户是否点击“是”。
            { // if 开始。
                return; // 用户取消时结束方法。
            } // if 结束。

            try // 捕获数据库删除和界面刷新过程中的异常。
            { // try 开始。
                _categoryRepository.DeleteCategory(selectedCategory.Id); // 调用仓储方法删除分类。
                LoadContentCategoryTree(); // 删除成功后刷新 TreeView。
                RefreshByCurrentCategoryAndSearch(); // 删除成功后刷新右侧书籍列表。
                System.Windows.MessageBox.Show("删除分类成功。"); // 提示删除成功。
            } // try 结束。
            catch (Exception ex) // 捕获删除失败异常。
            { // catch 开始。
                System.Windows.MessageBox.Show("删除分类失败：\n" + ex.Message); // 显示失败原因。
            } // catch 结束。
        } // 方法结束。

        private void RepairCoversButton_Click(object sender, RoutedEventArgs e) // 处理“修复封面”按钮点击事件，支持选中书籍局部修复或全部修复。
        { // 方法开始。
            try // 捕获整个修复过程中的异常，避免程序崩溃。
            { // try 开始。
                int repairedCount = 0; // 记录本次成功修复封面的书籍数量。
                int skippedCount = 0; // 记录本次跳过的书籍数量，包括已有封面或生成失败。
                List<Book> selectedBookList = GetSelectedBooks(); // 获取当前选中的书籍。
                List<Book> targetBookList; // 定义本次要处理的书籍列表。

                if (selectedBookList.Count > 0) // 如果用户选中了书籍。
                { // if 开始。
                    targetBookList = selectedBookList; // 只修复选中的书籍。
                } // if 结束。
                else // 如果用户没有选中书籍。
                { // else 开始。
                    targetBookList = _bookRepository.GetAllBooks(); // 默认扫描全部书籍。
                } // else 结束。

                foreach (Book book in targetBookList) // 遍历本次目标书籍。
                { // foreach 开始。
                    bool coverPathIsEmpty = string.IsNullOrWhiteSpace(book.CoverPath); // 判断数据库中的 CoverPath 是否为空。
                    bool coverFileMissing = !coverPathIsEmpty && !File.Exists(book.CoverPath); // 判断 CoverPath 有值但文件是否不存在。

                    if (!coverPathIsEmpty && !coverFileMissing) // 如果 CoverPath 有值并且文件存在。
                    { // if 开始。
                        skippedCount++; // 这种书封面正常，不需要重新生成。
                        continue; // 跳过当前书，继续下一本。
                    } // if 结束。

                    string newCoverPath = _bookCoverService.GenerateCoverPath(book.FilePath, book.FileType); // 重新生成封面；PDF 用第一页，EPUB 用内置封面，TXT 用默认封面。
                    if (string.IsNullOrWhiteSpace(newCoverPath)) // 判断封面服务是否返回了有效路径。
                    { // if 开始。
                        skippedCount++; // 没拿到路径时记为跳过。
                        continue; // 继续下一本书。
                    } // if 结束。

                    _bookRepository.UpdateBookCoverPath(book.Id, newCoverPath); // 把新的封面路径写回数据库 CoverPath 字段。
                    book.CoverPath = newCoverPath; // 同步更新内存对象，方便界面刷新后显示。
                    repairedCount++; // 成功修复数量加 1。
                } // foreach 结束。

                RefreshByCurrentCategoryAndSearch(); // 修复完成后刷新右侧书籍列表，让新封面立即显示。
                System.Windows.MessageBox.Show("封面修复完成。\n\n本次处理：" + targetBookList.Count + " 本\n成功修复：" + repairedCount + " 本\n跳过：" + skippedCount + " 本"); // 显示本次修复结果。
            } // try 结束。
            catch (Exception ex) // 捕获修复过程中的异常。
            { // catch 开始。
                System.Windows.MessageBox.Show("修复封面失败：\n" + ex.Message); // 显示失败原因。
            } // catch 结束。
        } // 方法结束。



        private void BindEvents() // 定义私有方法：统一绑定界面事件，避免在构造函数里写太散。
        { // 方法开始。

            if (ContentCategoryTreeView != null) // 判断 TreeView 是否已经初始化，避免窗口启动早期出现空引用。
            { // if 开始。
                TreeViewItem selectedTreeViewItem = ContentCategoryTreeView.SelectedItem as TreeViewItem; // 获取当前选中的树节点。
                if (selectedTreeViewItem != null) // 如果确实有选中的树节点。
                { // if 开始。
                    selectedTreeViewItem.IsSelected = false; // 取消 TreeView 的选中状态。
                } // if 结束。
            } // if 结束。



            ScanFolderButton.Click += ScanFolderButton_Click; // 绑定“扫描文件夹”按钮点击事件。
            SearchButton.Click += SearchButton_Click; // 绑定“搜索”按钮点击事件。
            SearchTextBox.KeyDown += SearchTextBox_KeyDown; // 绑定搜索框按键事件，用于支持回车搜索。
            CategoryListBox.SelectionChanged += CategoryListBox_SelectionChanged; // 绑定左侧分类切换事件。
            BooksListView.MouseDoubleClick += BooksListView_MouseDoubleClick; // 绑定书籍列表双击事件。
            ToggleFavoriteButton.Click += ToggleFavoriteButton_Click; // 绑定“收藏/取消收藏”按钮点击事件。
            UpdateCategoryButton.Click += UpdateCategoryButton_Click; // 绑定“修改分类”按钮点击事件。
            AddCategoryButton.Click += AddCategoryButton_Click; // 绑定“新增分类”按钮点击事件。
            DeleteBookButton.Click += DeleteBookButton_Click; // 绑定“删除书籍”按钮点击事件，点击后会执行删除逻辑方法。
            ClearSearchButton.Click += ClearSearchButton_Click; // 绑定“清空搜索”按钮点击事件，点击后会执行清空与刷新逻辑方法。
            DeleteCategoryButton.Click += DeleteCategoryButton_Click; // 绑定“删除分类”按钮点击事件。


            AddRootCategoryButton.Click += AddRootCategoryButton_Click; // 绑定“新增主分类”按钮点击事件。
            AddChildCategoryButton.Click += AddChildCategoryButton_Click; // 绑定“新增子分类”按钮点击事件。

            RepairCoversButton.Click += RepairCoversButton_Click; // 绑定“修复封面”按钮点击事件。




        } // 方法结束。

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e) // 处理“清空所有书籍”按钮点击事件。
        { // 方法开始。
            MessageBoxResult confirmResult = System.Windows.MessageBox.Show( // 弹出确认框，防止误清空。
                "确定要清空数据库中的所有书籍记录吗？\n\n此操作不可撤销。", // 确认提示文本。
                "确认清空", // 对话框标题。
                MessageBoxButton.YesNo, // 显示“是/否”按钮。
                MessageBoxImage.Warning); // 使用警告图标提示风险。

            if (confirmResult != MessageBoxResult.Yes) // 判断用户是否确认执行清空。
            { // if 代码块开始。
                return; // 用户取消则直接结束。
            } // if 代码块结束。

            try // 对数据库清空操作做异常保护。
            { // try 代码块开始。
                _bookRepository.DeleteAllBooks(); // 调用仓储层方法，删除 Books 表全部记录。
                SearchTextBox.Text = string.Empty; // 顺便清空搜索框，避免残留关键字影响后续显示。
                LoadContentCategoryTree(); // 重新加载内容分类 TreeView，让每个内容分类后面的数量重新从数据库统计并变成 0。
                UpdateLeftCategoryCounts(); // 重新刷新上方分类数量，例如 全部书籍(0)、PDF(0)、EPUB(0)、TXT(0)。
                RefreshByCurrentCategoryAndSearch(); // 刷新右侧书籍列表，让界面立即显示空数据状态。

                System.Windows.MessageBox.Show("已清空所有书籍记录。"); // 给用户成功反馈。
            } // try 代码块结束。
            catch (Exception ex) // 捕获清空过程中的异常。
            { // catch 代码块开始。
                System.Windows.MessageBox.Show("清空失败：\n" + ex.Message); // 提示失败原因，便于排查。
            } // catch 代码块结束。
        } // 方法结束。



        private void OpenBookFolderMenuItem_Click(object sender, RoutedEventArgs e) // 点击“打开文件所在位置”：定位到当前右键书籍文件。
        { // 方法开始。
            Book targetBook = null; // 定义目标书籍变量，初始为空。

            MenuItem clickedMenuItem = sender as MenuItem; // 把事件源转换为菜单项对象。
            if (clickedMenuItem != null) // 判断转换是否成功。
            { // if 代码块开始。
                ContextMenu parentContextMenu = clickedMenuItem.Parent as ContextMenu; // 获取该菜单项所在的右键菜单。
                if (parentContextMenu != null) // 判断右键菜单是否有效。
                { // if 代码块开始。
                    ListViewItem placementTargetItem = parentContextMenu.PlacementTarget as ListViewItem; // 尝试把右键目标转换为列表项。
                    if (placementTargetItem != null) // 如果转换成功，说明右键在某一行书籍上。
                    { // if 代码块开始。
                        targetBook = placementTargetItem.DataContext as Book; // 从该行的数据上下文拿到目标书籍对象。
                    } // if 代码块结束。
                } // if 代码块结束。
            } // if 代码块结束。

            if (targetBook == null) // 如果通过右键目标未获取到书籍对象。
            { // if 代码块开始。
                targetBook = BooksListView.SelectedItem as Book; // 兜底使用当前选中项。
            } // if 代码块结束。

            if (targetBook == null) // 如果仍未拿到有效书籍对象。
            { // if 代码块开始。
                System.Windows.MessageBox.Show("请先右键或选中一本书。"); // 提示用户先确定目标书籍。
                return; // 结束方法。
            } // if 代码块结束。

            string filePath = targetBook.FilePath ?? string.Empty; // 读取文件路径，null 时转为空字符串。
            if (string.IsNullOrWhiteSpace(filePath)) // 校验路径是否为空。
            { // if 代码块开始。
                System.Windows.MessageBox.Show("该书籍没有有效文件路径。"); // 提示路径无效。
                return; // 结束方法。
            } // if 代码块结束。

            if (!File.Exists(filePath)) // 打开前先检查文件是否真实存在。
            { // if 代码块开始。
                System.Windows.MessageBox.Show("文件不存在，可能已被移动或删除：\n" + filePath); // 提示文件不存在。
                return; // 结束方法。
            } // if 代码块结束。

            try // 对外部进程调用做异常保护。
            { // try 代码块开始。
                ProcessStartInfo processStartInfo = new ProcessStartInfo(); // 创建进程启动参数对象。
                processStartInfo.FileName = "explorer.exe"; // 指定启动 Windows 资源管理器。
                processStartInfo.Arguments = "/select,\"" + filePath + "\""; // 用 /select 定位并选中文件。
                processStartInfo.UseShellExecute = true; // 使用系统 Shell 执行。
                Process.Start(processStartInfo); // 打开文件所在位置。
            } // try 代码块结束。
            catch (Exception ex) // 捕获打开失败异常。
            { // catch 代码块开始。
                System.Windows.MessageBox.Show("打开文件所在位置失败：\n" + ex.Message); // 提示失败原因。
            } // catch 代码块结束。
        } // 方法结束。

        private void ContentCategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) // 处理内容分类 TreeView 选中项变化事件。
        { // 方法开始。
            TreeViewItem selectedTreeViewItem = ContentCategoryTreeView.SelectedItem as TreeViewItem; // 获取当前选中的 TreeViewItem。
            if (selectedTreeViewItem == null) // 判断是否真的选中了有效树节点。
            { // if 开始。
                return; // 没有有效节点时直接结束。
            } // if 结束。

            Category selectedCategory = selectedTreeViewItem.Tag as Category; // 从 Tag 中取出分类对象。
            if (selectedCategory == null) // 判断分类对象是否有效。
            { // if 开始。
                return; // 无效时直接结束。
            } // if 结束。

            CategoryListBox.SelectedItem = null; // 清空左侧格式分类选择，避免两个分类区域同时选中造成理解混乱。
            LoadBooksByContentCategory(selectedCategory.Name); // 按内容分类名称加载右侧书籍列表。
        } // 方法结束。


        private void InitializeDynamicCategoryUi() // 定义方法：初始化动态分类界面元素。
        { // 方法开始。
            RebuildDynamicCategoryListItems(); // 重建左侧动态内容分类项。
            RebuildManualCategoryComboBoxItems(); // 重建顶部手动分类下拉框项。
        } // 方法结束。

        private void RebuildDynamicCategoryListItems() // 定义方法：从数据库读取内容分类并重建左侧动态分类项。
        { // 方法开始。
            string selectedTagBeforeRebuild = GetSelectedCategoryTag(); // 记录重建前当前选中的分类 Tag，便于重建后恢复选中状态。

            for (int index = CategoryListBox.Items.Count - 1; index >= 0; index--) // 倒序遍历左侧列表项，准备删除旧的动态分类项。
            { // for 代码块开始。
                if (CategoryListBox.Items[index] is not ListBoxItem listBoxItem) // 判断当前项是否是 ListBoxItem。
                { // if 代码块开始。
                    continue; // 不是 ListBoxItem 时跳过。
                } // if 代码块结束。

                string currentTag = listBoxItem.Tag?.ToString() ?? string.Empty; // 读取当前项 Tag 并做空值保护。

                if (currentTag.StartsWith(DynamicCategoryTagPrefix, StringComparison.OrdinalIgnoreCase)) // 判断是否是动态内容分类项。
                { // if 代码块开始。
                    CategoryListBox.Items.RemoveAt(index); // 删除旧的动态分类项，避免重复。
                } // if 代码块结束。
            } // for 代码块结束。

            List<string> contentCategoryList = _bookRepository.GetAllContentCategories(); // 从数据库读取全部内容分类名称。

            // 内容分类现在统一显示在 ContentCategoryTreeView 中，不再添加到上方 CategoryListBox。


            bool restored = TrySelectCategoryByTag(selectedTagBeforeRebuild); // 尝试恢复重建前的选中项。

            if (!restored) // 判断是否恢复失败。
            { // if 代码块开始。
                TrySelectCategoryByTag("ALL"); // 恢复失败时默认选中“全部书籍”。
            } // if 代码块结束。
        } // 方法结束。

        private void RebuildManualCategoryComboBoxItems() // 定义方法：从数据库读取分类并重建顶部手动分类下拉框。
        { // 方法开始。
            ManualCategoryComboBox.Items.Clear(); // 先清空下拉框旧项，避免重复。

            List<string> contentCategoryList = _bookRepository.GetAllContentCategories(); // 读取全部内容分类名称。

            foreach (string categoryName in contentCategoryList) // 遍历分类列表逐个加入下拉框。
            { // foreach 代码块开始。
                ComboBoxItem comboBoxItem = new ComboBoxItem(); // 创建新的下拉项对象。
                comboBoxItem.Content = categoryName; // 设置下拉项显示文本。
                ManualCategoryComboBox.Items.Add(comboBoxItem); // 把下拉项加入下拉框。
            } // foreach 代码块结束。

            if (ManualCategoryComboBox.Items.Count > 0) // 判断下拉框是否至少有一个可选项。
            { // if 代码块开始。
                ManualCategoryComboBox.SelectedIndex = 0; // 默认选中第一项，保证按钮可立即使用。
            } // if 代码块结束。
        } // 方法结束。

        private bool TrySelectCategoryByTag(string targetTag) // 定义方法：按 Tag 查找并选中左侧分类项。
        { // 方法开始。
            foreach (object itemObject in CategoryListBox.Items) // 遍历左侧全部项。
            { // foreach 代码块开始。
                if (itemObject is not ListBoxItem listBoxItem) // 判断当前项是否是 ListBoxItem。
                { // if 代码块开始。
                    continue; // 不是则跳过。
                } // if 代码块结束。

                string currentTag = listBoxItem.Tag?.ToString() ?? string.Empty; // 读取当前项 Tag。

                if (string.Equals(currentTag, targetTag, StringComparison.OrdinalIgnoreCase)) // 判断 Tag 是否匹配目标 Tag。
                { // if 代码块开始。
                    CategoryListBox.SelectedItem = listBoxItem; // 匹配成功则选中该项。
                    return true; // 返回 true 表示选中成功。
                } // if 代码块结束。
            } // foreach 代码块结束。

            return false; // 全部遍历后仍未找到，返回 false。
        } // 方法结束。

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e) // 定义“新增分类”按钮点击事件处理方法。
        { // 方法开始。
            string newCategoryName = NewCategoryTextBox.Text?.Trim() ?? string.Empty; // 读取输入框文字并去除首尾空格。

            if (string.IsNullOrWhiteSpace(newCategoryName)) // 判断输入是否为空。
            { // if 代码块开始。
                MessageBox.Show("请输入要新增的内容分类名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 为空时提示用户输入分类名。
                return; // 结束方法，避免写入空分类。
            } // if 代码块结束。

            if (_bookRepository.ContentCategoryExists(newCategoryName)) // 判断数据库里是否已经存在同名分类。
            { // if 代码块开始。
                MessageBox.Show("该分类已存在，请输入其他名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 已存在时提示重复。
                return; // 结束方法，避免重复插入。
            } // if 代码块结束。

            _bookRepository.AddContentCategory(newCategoryName); // 把新分类写入数据库持久保存。
            NewCategoryTextBox.Text = string.Empty; // 新增成功后清空输入框文本。
            RebuildDynamicCategoryListItems(); // 重新构建左侧动态内容分类项，让新分类立刻出现。
            RebuildManualCategoryComboBoxItems(); // 重新构建顶部手动分类下拉框，让新分类立刻可选。
            RefreshByCurrentCategoryAndSearch(); // 刷新当前界面，让左侧数量和右侧数据同步更新。
            MessageBox.Show($"已新增内容分类：{newCategoryName}。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 提示新增成功。
        } // 方法结束。


        private void LoadAllBooks() // 定义加载全部图书的方法。
        { // 方法开始。
            UpdateLeftCategoryCounts(); // 先更新左侧各分类数量，保证程序启动时数量就是最新。
            _currentBookList = _bookRepository.GetAllBooks(); // 从数据库读取全部图书列表。
            BooksListView.ItemsSource = _currentBookList; // 把全部图书绑定到中间列表显示。
        } // 方法结束。


        private void ScanFolderButton_Click(object sender, RoutedEventArgs e) // 定义“扫描文件夹”按钮点击事件处理方法。
        { // 方法开始。
            using WinForms.FolderBrowserDialog folderBrowserDialog = new WinForms.FolderBrowserDialog(); // 创建文件夹选择对话框对象并交给 using 自动释放。
            folderBrowserDialog.Description = "请选择要扫描的电子书文件夹"; // 设置对话框提示文本，指导用户选择目录。
            folderBrowserDialog.ShowNewFolderButton = false; // 隐藏“新建文件夹”按钮，保持交互简洁。
            WinForms.DialogResult dialogResult = folderBrowserDialog.ShowDialog(); // 显示对话框并获取用户操作结果。

            if (dialogResult != WinForms.DialogResult.OK) // 判断用户是否点击了“确定”。
            { // if 代码块开始。
                return; // 用户取消选择时直接返回，不执行扫描。
            } // if 代码块结束。

            string selectedFolderPath = folderBrowserDialog.SelectedPath; // 读取用户选中的目录完整路径。
            List<Book> importedBookList = _bookScannerService.ScanAndImportBooks(selectedFolderPath); // 调用扫描服务执行导入，并拿到本次新导入图书列表。
            MessageBox.Show($"扫描完成，本次新导入 {importedBookList.Count} 本图书。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 弹窗提示本次导入数量。
            RefreshByCurrentCategoryAndSearch(); // 扫描完成后按当前分类和搜索条件刷新界面列表。
        } // 方法结束。

        private void SearchButton_Click(object sender, RoutedEventArgs e) // 定义“搜索”按钮点击事件处理方法。
        { // 方法开始。
            RefreshByCurrentCategoryAndSearch(); // 执行统一刷新逻辑，实现按当前分类 + 关键字过滤。
        } // 方法结束。

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e) // 定义搜索框按键事件处理方法。
        { // 方法开始。
            if (e.Key == Key.Enter) // 判断当前按下的键是否是回车键。
            { // if 代码块开始。
                RefreshByCurrentCategoryAndSearch(); // 按下回车时执行搜索刷新。
            } // if 代码块结束。
        } // 方法结束。

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) // 定义左侧分类切换事件处理方法。
        { // 方法开始。
            RefreshByCurrentCategoryAndSearch(); // 切换分类后按当前关键字再次过滤并刷新结果。
        } // 方法结束。

        private void RefreshByCurrentCategoryAndSearch() // 定义统一刷新方法：更新左侧数量 + 右侧列表。
        { // 方法开始。
            UpdateLeftCategoryCounts(); // 每次刷新时先重算左侧数量，保证“实时统计”。
            string selectedCategoryTag = GetSelectedCategoryTag(); // 获取当前左侧选中的分类 Tag。
            string keyword = SearchTextBox.Text?.Trim() ?? string.Empty; // 获取搜索关键词并做空值保护。
            List<Book> baseList = GetBooksByCategoryTag(selectedCategoryTag); // 先按分类拿到基础列表。

            if (!string.IsNullOrWhiteSpace(keyword)) // 判断是否输入了搜索关键词。
            { // if 代码块开始。
                List<Book> keywordMatchedList = new List<Book>(); // 创建关键词匹配结果列表。
                foreach (Book book in baseList) // 遍历分类基础列表中的每本书。
                { // foreach 代码块开始。
                    if (book.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) // 忽略大小写判断书名是否包含关键词。
                    { // if 代码块开始。
                        keywordMatchedList.Add(book); // 命中则加入结果列表。
                    } // if 代码块结束。
                } // foreach 代码块结束。
                _currentBookList = keywordMatchedList; // 把关键词过滤结果设为当前显示列表。
            } // if 代码块结束。
            else // 当没有关键词时直接使用分类结果。
            { // else 代码块开始。
                _currentBookList = baseList; // 当前显示列表直接等于分类基础列表。
            } // else 代码块结束。

            BooksListView.ItemsSource = null; // 先清空绑定，确保 UI 正确刷新。
            BooksListView.ItemsSource = _currentBookList; // 重新绑定当前结果列表。
        } // 方法结束。

        private const string DynamicCategoryTagPrefix = "CAT_DYNAMIC:"; // 定义动态内容分类 Tag 前缀，用于识别左侧动态分类项。




        private string GetSelectedCategoryTag() // 定义私有方法：读取当前选中分类的 Tag。
        { // 方法开始。
            if (CategoryListBox.SelectedItem is not ListBoxItem selectedItem) // 判断当前选中项是否是有效的 ListBoxItem。
            { // if 代码块开始。
                return "ALL"; // 如果没有有效选中项，默认返回 ALL。
            } // if 代码块结束。

            string tagValue = selectedItem.Tag?.ToString() ?? "ALL"; // 读取选中项的 Tag 文本，空值时回退 ALL。
            return tagValue; // 返回最终分类 Tag。
        } // 方法结束。

        private List<Book> GetBooksByCategoryTag(string categoryTag) // 定义方法：根据左侧分类 Tag 返回对应书籍列表。
        { // 方法开始。
            if (string.Equals(categoryTag, "RECENT", StringComparison.OrdinalIgnoreCase)) // 判断是否是“最近阅读”分类。
            { // if 代码块开始。
                return _bookRepository.GetRecentBooks(200); // 返回最近阅读列表。
            } // if 代码块结束。

            if (string.Equals(categoryTag, "FAVORITE", StringComparison.OrdinalIgnoreCase)) // 判断是否是“我的收藏”分类。
            { // if 代码块开始。
                return _bookRepository.GetFavoriteBooks(); // 返回收藏列表。
            } // if 代码块结束。

            if (string.Equals(categoryTag, "PDF", StringComparison.OrdinalIgnoreCase)) // 判断是否是 PDF 格式分类。
            { // if 代码块开始。
                return _bookRepository.GetBooksByFileType("PDF"); // 返回 PDF 列表。
            } // if 代码块结束。

            if (string.Equals(categoryTag, "EPUB", StringComparison.OrdinalIgnoreCase)) // 判断是否是 EPUB 格式分类。
            { // if 代码块开始。
                return _bookRepository.GetBooksByFileType("EPUB"); // 返回 EPUB 列表。
            } // if 代码块结束。

            if (string.Equals(categoryTag, "TXT", StringComparison.OrdinalIgnoreCase)) // 判断是否是 TXT 格式分类。
            { // if 代码块开始。
                return _bookRepository.GetBooksByFileType("TXT"); // 返回 TXT 列表。
            } // if 代码块结束。

            if (categoryTag.StartsWith(DynamicCategoryTagPrefix, StringComparison.OrdinalIgnoreCase)) // 判断是否是动态内容分类 Tag。
            { // if 代码块开始。
                string categoryName = categoryTag.Substring(DynamicCategoryTagPrefix.Length); // 从 Tag 中截取真实分类名称。
                return GetBooksByContentCategory(categoryName); // 按内容分类过滤图书并返回。
            } // if 代码块结束。

            return _bookRepository.GetAllBooks(); // 其他情况默认返回全部图书。
        } // 方法结束。


        private List<Book> GetBooksByContentCategory(string categoryName) // 定义私有方法：按内容分类名称过滤图书。
        { // 方法开始。
            List<Book> allBookList = _bookRepository.GetAllBooks(); // 先读取全部图书用于内存过滤。
            List<Book> filteredBookList = new List<Book>(); // 创建过滤结果列表。

            foreach (Book book in allBookList) // 遍历每一本书。
            { // foreach 代码块开始。
                string currentCategory = book.Category?.Trim() ?? string.Empty; // 读取当前书籍分类并做空值保护与去空格。

                if (string.Equals(categoryName, "未分类", StringComparison.OrdinalIgnoreCase)) // 判断目标是否是“未分类”。
                { // if 代码块开始。
                    if (string.IsNullOrWhiteSpace(currentCategory) || string.Equals(currentCategory, "未分类", StringComparison.OrdinalIgnoreCase)) // 空分类或文本“未分类”都算未分类。
                    { // if 代码块开始。
                        filteredBookList.Add(book); // 满足条件加入结果列表。
                    } // if 代码块结束。
                    continue; // 已处理未分类分支，继续处理下一本书。
                } // if 代码块结束。

                if (string.Equals(currentCategory, categoryName, StringComparison.OrdinalIgnoreCase)) // 判断分类名称是否等于目标分类。
                { // if 代码块开始。
                    filteredBookList.Add(book); // 满足条件加入结果列表。
                } // if 代码块结束。
            } // foreach 代码块结束。

            return filteredBookList; // 返回过滤后的图书列表。
        } // 方法结束。

        private void BooksListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) // 处理书籍列表双击事件。
        { // 方法开始。
            Book selectedBook = BooksListView.SelectedItem as Book; // 从书籍列表当前选中项获取 Book 对象。
            if (selectedBook == null) // 判断是否真的选中了有效书籍对象。
            { // if 代码块开始。
                return; // 没选中有效书籍时直接结束，避免空引用异常。
            } // if 代码块结束。

            string bookFilePath = selectedBook.FilePath ?? string.Empty; // 读取书籍文件路径；如果为 null 就降级为空字符串。
            if (string.IsNullOrWhiteSpace(bookFilePath)) // 判断路径是否为空或全空格。
            { // if 代码块开始。
                System.Windows.MessageBox.Show("该书籍没有有效文件路径，无法打开。"); // 提示用户该记录缺少可用路径。
                return; // 终止后续打开逻辑。
            } // if 代码块结束。

            if (!File.Exists(bookFilePath)) // 在真正打开前检查文件是否存在（安全点4核心）。
            { // if 代码块开始。
                System.Windows.MessageBox.Show("文件不存在，可能已被移动或删除：\n" + bookFilePath); // 提示用户文件不存在，并显示路径方便排查。
                return; // 文件不存在时不再继续调用打开逻辑。
            } // if 代码块结束。

            try // 对打开文件过程做异常保护，避免外部程序调用失败导致程序崩溃。
            { // try 代码块开始。
                ProcessStartInfo openInfo = new ProcessStartInfo(); // 创建系统打开文件所需的启动信息对象。
                openInfo.FileName = bookFilePath; // 指定要打开的目标文件完整路径。
                openInfo.UseShellExecute = true; // 使用系统 Shell，让 Windows 按默认程序打开文件。
                Process.Start(openInfo); // 调用系统默认程序打开书籍文件。

                selectedBook.LastOpenTime = DateTimeHelper.GetNowIsoString(); // 只有打开成功后才更新最近打开时间。
                _bookRepository.UpdateLastOpenTime(selectedBook.Id, selectedBook.LastOpenTime); // 把最近打开时间写回数据库。
                RefreshByCurrentCategoryAndSearch(); // 刷新界面，让“最近打开时间”列立即显示最新值。
            } // try 代码块结束。
            catch (Exception ex) // 捕获打开过程中的异常。
            { // catch 代码块开始。
                System.Windows.MessageBox.Show("打开书籍失败：\n" + ex.Message); // 向用户显示失败原因，便于定位问题。
            } // catch 代码块结束。
        } // 方法结束。


        private void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e) // 定义收藏按钮点击事件处理方法，支持单选和多选。
        { // 方法开始。
            List<Book> selectedBookList = GetSelectedBooks(); // 获取当前选中的所有书籍。
            if (selectedBookList.Count == 0) // 判断是否选中了至少一本书。
            { // if 代码块开始。
                MessageBox.Show("请先在书籍列表中选中要操作的书籍。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 未选中时提示用户。
                return; // 未选中时结束方法。
            } // if 代码块结束。

            int favoriteCount = 0; // 记录本次变成收藏的数量。
            int unfavoriteCount = 0; // 记录本次取消收藏的数量。

            foreach (Book selectedBook in selectedBookList) // 遍历所有选中的书籍。
            { // foreach 代码块开始。
                bool newFavoriteStatus = !selectedBook.IsFavorite; // 每本书各自取反当前收藏状态。
                _bookRepository.UpdateFavoriteStatus(selectedBook.Id, newFavoriteStatus); // 把新收藏状态写入数据库。
                selectedBook.IsFavorite = newFavoriteStatus; // 同步更新内存对象中的收藏状态。

                if (newFavoriteStatus) // 判断这本书是否被加入收藏。
                { // if 代码块开始。
                    favoriteCount++; // 加入收藏数量加 1。
                } // if 代码块结束。
                else // 否则就是取消收藏。
                { // else 代码块开始。
                    unfavoriteCount++; // 取消收藏数量加 1。
                } // else 代码块结束。
            } // foreach 代码块结束。

            RefreshByCurrentCategoryAndSearch(); // 刷新当前列表，立即看到收藏状态变化。
            MessageBox.Show("操作完成。\n\n已加入收藏：" + favoriteCount + " 本\n已取消收藏：" + unfavoriteCount + " 本", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 提示本次批量操作结果。
        } // 方法结束。


        private void ChangeCategoryMenuItem_Click(object sender, RoutedEventArgs e) // 定义右键菜单“修改分类子项”点击事件处理方法。
        { // 方法开始。
            if (sender is not MenuItem clickedMenuItem) // 判断事件源是否是 MenuItem。
            { // if 代码块开始。
                return; // 不是 MenuItem 时结束方法。
            } // if 代码块结束。

            string targetCategory = clickedMenuItem.Tag?.ToString() ?? "未分类"; // 从菜单项 Tag 读取目标分类名称，空值时回退“未分类”。

            Book? targetBook = clickedMenuItem.DataContext as Book; // 优先从菜单项 DataContext 获取当前被右键的书籍对象。

            if (targetBook is null) // 判断是否成功获取到目标书籍。
            { // if 代码块开始。
                if (clickedMenuItem.Parent is MenuItem parentMenuItem && parentMenuItem.Parent is ContextMenu contextMenu) // 尝试从父菜单的 ContextMenu 再取一次 DataContext。
                { // if 代码块开始。
                    targetBook = contextMenu.DataContext as Book; // 从 ContextMenu.DataContext 获取目标书籍对象。
                } // if 代码块结束。
            } // if 代码块结束。

            if (targetBook is null) // 再次判断目标书籍是否存在。
            { // if 代码块开始。
                MessageBox.Show("未找到要修改的书籍，请在列表中右键点击具体书籍。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 找不到目标对象时提示用户。
                return; // 结束方法，避免空对象操作。
            } // if 代码块结束。

            _bookRepository.UpdateCategory(targetBook.Id, targetCategory); // 更新数据库中该书的 Category 字段。
            targetBook.Category = targetCategory; // 同步更新内存中的书籍对象分类。
            BooksListView.SelectedItem = targetBook; // 把该书设为选中项，保持操作连贯性。
            RefreshByCurrentCategoryAndSearch(); // 刷新界面，让左侧数量和右侧数据立即同步。
        } // 方法结束。

        private List<Book> GetSelectedBooks() // 获取书籍列表中当前选中的所有书籍。
        { // 方法开始。
            List<Book> selectedBookList = new List<Book>(); // 创建结果列表，用来保存选中的 Book 对象。

            foreach (object selectedItem in BooksListView.SelectedItems) // 遍历 ListView 当前所有选中项。
            { // foreach 开始。
                Book selectedBook = selectedItem as Book; // 尝试把选中项转换为 Book 对象。
                if (selectedBook != null) // 判断转换是否成功。
                { // if 开始。
                    selectedBookList.Add(selectedBook); // 把有效的 Book 对象加入结果列表。
                } // if 结束。
            } // foreach 结束。

            return selectedBookList; // 返回当前选中的书籍列表。
        } // 方法结束。


        private void UpdateCategoryButton_Click(object sender, RoutedEventArgs e) // 处理顶部“修改分类”按钮点击事件，支持单选和多选。
        { // 方法开始。
            List<Book> selectedBookList = GetSelectedBooks(); // 获取当前选中的所有书籍。
            if (selectedBookList.Count == 0) // 判断是否选中了至少一本书。
            { // if 开始。
                MessageBox.Show("请先在书籍列表中选中要修改分类的书籍。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 未选中时提示。
                return; // 结束方法。
            } // if 结束。

            ComboBoxItem selectedCategoryItem = ManualCategoryComboBox.SelectedItem as ComboBoxItem; // 获取顶部分类下拉框当前选中项。
            if (selectedCategoryItem == null) // 判断是否选中了分类。
            { // if 开始。
                MessageBox.Show("请选择一个内容分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 未选分类时提示。
                return; // 结束方法。
            } // if 结束。

            string categoryName = selectedCategoryItem.Content as string ?? string.Empty; // 从下拉框选中项读取分类名称。
            if (string.IsNullOrWhiteSpace(categoryName)) // 判断分类名是否为空。
            { // if 开始。
                MessageBox.Show("请选择一个有效分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 分类无效时提示。
                return; // 结束方法。
            } // if 结束。

            foreach (Book selectedBook in selectedBookList) // 遍历所有选中的书籍。
            { // foreach 开始。
                _bookRepository.UpdateBookCategory(selectedBook.Id, categoryName); // 把当前书籍更新到目标分类。
                selectedBook.Category = categoryName; // 同步更新内存对象。
            } // foreach 结束。

            LoadContentCategoryTree(); // 刷新左侧内容分类数量。
            RefreshByCurrentCategoryAndSearch(); // 刷新右侧列表。
            MessageBox.Show("已将 " + selectedBookList.Count + " 本书修改为分类：" + categoryName + "。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 提示操作结果。
        } // 方法结束。


        private void UpdateLeftCategoryCounts() // 定义方法：统计各分类数量并更新左侧“分类名(数量)”显示。
        { // 方法开始。
            List<Book> allBookList = _bookRepository.GetAllBooks(); // 读取全部图书，用于统计。
            List<string> contentCategoryList = _bookRepository.GetAllContentCategories(); // 读取全部内容分类，用于动态分类统计。

            int allCount = 0; // 定义“全部书籍”计数器。
            int recentCount = 0; // 定义“最近阅读”计数器。
            int favoriteCount = 0; // 定义“我的收藏”计数器。
            int pdfCount = 0; // 定义 PDF 计数器。
            int epubCount = 0; // 定义 EPUB 计数器。
            int txtCount = 0; // 定义 TXT 计数器。

            Dictionary<string, int> contentCategoryCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 创建内容分类数量映射字典。

            foreach (string categoryName in contentCategoryList) // 先给每个分类初始化 0。
            { // foreach 代码块开始。
                if (!contentCategoryCountMap.ContainsKey(categoryName)) // 判断字典里是否已存在该分类键。
                { // if 代码块开始。
                    contentCategoryCountMap.Add(categoryName, 0); // 不存在则加入并初始化为 0。
                } // if 代码块结束。
            } // foreach 代码块结束。

            foreach (Book book in allBookList) // 遍历每本书进行统计累加。
            { // foreach 代码块开始。
                allCount++; // 全部书籍计数 +1。

                if (!string.IsNullOrWhiteSpace(book.LastOpenTime)) // 判断是否有最近打开时间。
                { // if 代码块开始。
                    recentCount++; // 有值则最近阅读计数 +1。
                } // if 代码块结束。

                if (book.IsFavorite) // 判断是否收藏。
                { // if 代码块开始。
                    favoriteCount++; // 收藏计数 +1。
                } // if 代码块结束。

                if (string.Equals(book.FileType, "PDF", StringComparison.OrdinalIgnoreCase)) // 判断是否 PDF。
                { // if 代码块开始。
                    pdfCount++; // PDF 计数 +1。
                } // if 代码块结束。

                if (string.Equals(book.FileType, "EPUB", StringComparison.OrdinalIgnoreCase)) // 判断是否 EPUB。
                { // if 代码块开始。
                    epubCount++; // EPUB 计数 +1。
                } // if 代码块结束。

                if (string.Equals(book.FileType, "TXT", StringComparison.OrdinalIgnoreCase)) // 判断是否 TXT。
                { // if 代码块开始。
                    txtCount++; // TXT 计数 +1。
                } // if 代码块结束。

                string categoryValue = book.Category?.Trim() ?? string.Empty; // 读取内容分类并做空值保护。

                if (string.IsNullOrWhiteSpace(categoryValue)) // 判断分类是否为空。
                { // if 代码块开始。
                    categoryValue = "未分类"; // 空分类统一按“未分类”处理。
                } // if 代码块结束。

                if (!contentCategoryCountMap.ContainsKey(categoryValue)) // 判断该分类是否已在分类表里。
                { // if 代码块开始。
                    contentCategoryCountMap.Add(categoryValue, 0); // 若不存在则临时加入字典，避免计数丢失。
                } // if 代码块结束。

                contentCategoryCountMap[categoryValue] = contentCategoryCountMap[categoryValue] + 1; // 对当前分类计数 +1。
            } // foreach 代码块结束。

            SetCategoryItemText("ALL", "全部书籍", allCount); // 更新全部书籍显示文本。
            SetCategoryItemText("RECENT", "最近阅读", recentCount); // 更新最近阅读显示文本。
            SetCategoryItemText("FAVORITE", "我的收藏", favoriteCount); // 更新我的收藏显示文本。
            SetCategoryItemText("PDF", "PDF", pdfCount); // 更新 PDF 显示文本。
            SetCategoryItemText("EPUB", "EPUB", epubCount); // 更新 EPUB 显示文本。
            SetCategoryItemText("TXT", "TXT", txtCount); // 更新 TXT 显示文本。

            foreach (string categoryName in contentCategoryList) // 遍历动态内容分类并更新左侧显示文本。
            { // foreach 代码块开始。
                int categoryCount = contentCategoryCountMap.ContainsKey(categoryName) ? contentCategoryCountMap[categoryName] : 0; // 读取该分类数量，不存在则按 0。
                SetCategoryItemText(DynamicCategoryTagPrefix + categoryName, categoryName, categoryCount); // 更新动态分类项显示为“分类名(数量)”。
            } // foreach 代码块结束。
        } // 方法结束。

        private void BookItemContextMenu_Opened(object sender, ContextMenuEventArgs e) // 右键菜单打开前：定位当前书籍并重建“移动到内容分类”子菜单。
        { // 方法开始。
            ListViewItem currentItem = sender as ListViewItem; // 把事件发送者转换为当前被右键的列表项。
            if (currentItem == null) // 如果转换失败，说明不是从有效列表项触发。
            { // if 代码块开始。
                return; // 直接返回，避免空引用。
            } // if 代码块结束。

            if (!currentItem.IsSelected) // 如果右键点击的是未选中的书籍。
            { // if 开始。
                BooksListView.SelectedItems.Clear(); // 清空原来的选择，模拟 Windows 文件管理器右键未选中项时切换目标。
                currentItem.IsSelected = true; // 把当前右键的书籍设为选中项。
            } // if 结束。


            ContextMenu itemContextMenu = currentItem.ContextMenu; // 获取当前列表项绑定的右键菜单。
            if (itemContextMenu == null) // 判断右键菜单是否存在。
            { // if 代码块开始。
                return; // 不存在时直接结束。
            } // if 代码块结束。

            itemContextMenu.Tag = currentItem.DataContext; // 把当前右键书籍对象保存到右键菜单 Tag，方便子菜单点击时取回。

            MenuItem moveToContentCategoryRootMenuItem = null; // 定义“移动到内容分类”根菜单变量。

            foreach (object menuObject in itemContextMenu.Items) // 遍历右键菜单中的每个顶级菜单项。
            { // foreach 代码块开始。
                MenuItem oneMenuItem = menuObject as MenuItem; // 尝试把当前对象转换为 MenuItem。
                if (oneMenuItem == null) // 如果不是菜单项，例如分隔线。
                { // if 代码块开始。
                    continue; // 跳过分隔线。
                } // if 代码块结束。

                string menuTag = oneMenuItem.Tag as string ?? string.Empty; // 读取菜单 Tag，用于识别菜单功能。
                if (menuTag == "MOVE_TO_CONTENT_CATEGORY_ROOT") // 判断是否是“移动到内容分类”菜单。
                { // if 代码块开始。
                    moveToContentCategoryRootMenuItem = oneMenuItem; // 保存该菜单项。
                    break; // 找到后不再继续遍历。
                } // if 代码块结束。
            } // foreach 代码块结束。

            if (moveToContentCategoryRootMenuItem == null) // 如果没有找到“移动到内容分类”菜单。
            { // if 代码块开始。
                return; // 直接结束，避免空引用。
            } // if 代码块结束。

            moveToContentCategoryRootMenuItem.Items.Clear(); // 清空旧子菜单，防止多次打开后重复添加。

            List<Category> rootCategoryList = _categoryRepository.GetAllCategories(); // 从数据库读取当前所有内容分类。

            foreach (Category rootCategory in rootCategoryList) // 遍历每个主分类。
            { // foreach 代码块开始。
                MenuItem rootMoveMenuItem = new MenuItem(); // 创建主分类菜单项。
                rootMoveMenuItem.Header = rootCategory.Name; // 设置主分类菜单显示文字。
                rootMoveMenuItem.Tag = rootCategory.Name; // 把主分类名称保存到 Tag。
                rootMoveMenuItem.Click += MoveBookToContentCategory_Click; // 绑定点击事件，点击后移动到主分类。

                foreach (Category childCategory in rootCategory.Children) // 遍历当前主分类下的子分类。
                { // foreach 代码块开始。
                    MenuItem childMoveMenuItem = new MenuItem(); // 创建子分类菜单项。
                    childMoveMenuItem.Header = childCategory.Name; // 设置子分类菜单显示文字。
                    childMoveMenuItem.Tag = childCategory.Name; // 把子分类名称保存到 Tag。
                    childMoveMenuItem.Click += MoveBookToContentCategory_Click; // 绑定点击事件，点击后移动到子分类。
                    rootMoveMenuItem.Items.Add(childMoveMenuItem); // 把子分类菜单项加入主分类菜单下面。
                } // foreach 代码块结束。

                moveToContentCategoryRootMenuItem.Items.Add(rootMoveMenuItem); // 把主分类菜单加入“移动到内容分类”菜单下面。
            } // foreach 代码块结束。
        } // 方法结束。



        private void MoveBookToContentCategory_Click(object sender, RoutedEventArgs e) // 处理右键菜单“移动到内容分类”的点击事件，支持单选和多选。
        { // 方法开始。
            e.Handled = true; // 阻止菜单点击事件继续向父级菜单冒泡，避免点击子分类后又触发主分类菜单。

            MenuItem clickedMenuItem = sender as MenuItem; // 把点击来源转换为菜单项对象。
            if (clickedMenuItem == null) // 判断菜单项是否有效。
            { // if 开始。
                return; // 无效时直接结束。
            } // if 结束。

            string targetCategoryName = clickedMenuItem.Tag as string ?? string.Empty; // 从菜单项 Tag 读取目标内容分类名称。
            if (string.IsNullOrWhiteSpace(targetCategoryName)) // 判断目标分类名称是否为空。
            { // if 开始。
                return; // 分类名为空时不执行更新。
            } // if 结束。

            List<Book> selectedBookList = GetSelectedBooks(); // 获取当前选中的所有书籍。
            if (selectedBookList.Count == 0) // 判断是否选中了至少一本书。
            { // if 开始。
                MessageBox.Show("请先选中要移动分类的书籍。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 未选中时提示。
                return; // 结束方法。
            } // if 结束。

            foreach (Book selectedBook in selectedBookList) // 遍历所有选中的书籍。
            { // foreach 开始。
                _bookRepository.UpdateBookCategory(selectedBook.Id, targetCategoryName); // 把书籍分类更新为目标内容分类。
                selectedBook.Category = targetCategoryName; // 同步更新内存对象。
            } // foreach 结束。

            LoadContentCategoryTree(); // 刷新左侧内容分类数量。
            RefreshByCurrentCategoryAndSearch(); // 刷新右侧列表；若当前筛选不包含新分类，书籍会自动消失。
            MessageBox.Show("已移动 " + selectedBookList.Count + " 本书到分类：" + targetCategoryName + "。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 提示操作结果。
        } // 方法结束。


        private void RemoveBookFromContentCategory_Click(object sender, RoutedEventArgs e) // 处理右键菜单“移出内容分类”的点击事件，支持单选和多选。
        { // 方法开始。
            List<Book> selectedBookList = GetSelectedBooks(); // 获取当前选中的所有书籍。
            if (selectedBookList.Count == 0) // 判断是否选中了至少一本书。
            { // if 代码块开始。
                MessageBox.Show("请先选中要移出内容分类的书籍。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 未选中时提示用户。
                return; // 结束方法，避免空操作。
            } // if 代码块结束。

            foreach (Book selectedBook in selectedBookList) // 遍历所有选中的书籍。
            { // foreach 开始。
                _bookRepository.UpdateBookCategory(selectedBook.Id, "未分类"); // 把书籍内容分类改为“未分类”，不删除书籍记录和文件。
                selectedBook.Category = "未分类"; // 同步更新内存对象，保持界面数据一致。
            } // foreach 结束。

            LoadContentCategoryTree(); // 刷新左侧内容分类树，让原分类和“未分类”的数量立即更新。
            RefreshByCurrentCategoryAndSearch(); // 刷新右侧列表；如果当前正在看原分类，书籍会从列表中消失。
            MessageBox.Show("已将 " + selectedBookList.Count + " 本书移出内容分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); // 提示操作结果。
        } // 方法结束。




        private void SetCategoryItemText(string tag, string displayName, int count) // 定义方法：按 Tag 找到左侧项并更新成“名称(数量)”文本。
        { // 方法开始。
            foreach (object itemObject in CategoryListBox.Items) // 遍历左侧分类列表中的每一项。
            { // foreach 代码块开始。
                if (itemObject is not ListBoxItem listBoxItem) // 判断当前项是否是 ListBoxItem。
                { // if 代码块开始。
                    continue; // 不是 ListBoxItem 就跳过。
                } // if 代码块结束。

                string currentTag = listBoxItem.Tag?.ToString() ?? string.Empty; // 读取当前项 Tag 并做空值保护。

                if (string.Equals(currentTag, tag, StringComparison.OrdinalIgnoreCase)) // 判断当前项 Tag 是否等于目标 Tag。
                { // if 代码块开始。
                    listBoxItem.Content = $"{displayName}({count})"; // 更新左侧显示文本为“分类名(数量)”格式。
                    break; // 找到并更新后直接结束循环，避免继续遍历。
                } // if 代码块结束。
            } // foreach 代码块结束。
        } // 方法结束。

        private void DeleteBookButton_Click(object sender, RoutedEventArgs e) // 处理“删除书籍”按钮点击事件，支持单选和多选删除。
        { // 方法开始。
            List<Book> selectedBookList = GetSelectedBooks(); // 获取当前选中的所有书籍。
            if (selectedBookList.Count == 0) // 判断是否选中了至少一本书。
            { // if 代码块开始。
                System.Windows.MessageBox.Show("请先选中要删除的书籍。"); // 未选中时提示用户。
                return; // 结束方法，避免误操作。
            } // if 代码块结束。

            MessageBoxResult confirmResult = System.Windows.MessageBox.Show( // 弹出二次确认框，防止误删多本书籍记录。
                "确定要删除选中的 " + selectedBookList.Count + " 本书籍记录吗？\n\n注意：只删除数据库记录，不删除原始电子书文件。", // 确认提示文本。
                "确认删除", // 对话框标题。
                MessageBoxButton.YesNo, // 显示“是/否”按钮。
                MessageBoxImage.Warning); // 使用警告图标强调风险操作。

            if (confirmResult != MessageBoxResult.Yes) // 判断用户是否点击了“是”。
            { // if 代码块开始。
                return; // 用户取消删除时直接结束方法。
            } // if 代码块结束。

            try // 对删除过程做异常保护，防止数据库异常导致界面崩溃。
            { // try 代码块开始。
                foreach (Book selectedBook in selectedBookList) // 遍历所有选中的书籍。
                { // foreach 开始。
                    _bookRepository.DeleteBookById(selectedBook.Id); // 调用仓储层方法，按主键删除数据库记录。
                } // foreach 结束。

                LoadContentCategoryTree(); // 删除书籍后刷新内容分类树，让分类数量立即减少。
                UpdateLeftCategoryCounts(); // 删除书籍后刷新上方固定分类数量。
                RefreshByCurrentCategoryAndSearch(); // 删除成功后立即刷新当前列表与筛选结果。
                System.Windows.MessageBox.Show("删除成功，共删除 " + selectedBookList.Count + " 本书籍记录。"); // 给用户成功反馈。
            } // try 代码块结束。
            catch (Exception ex) // 捕获删除过程中的异常。
            { // catch 代码块开始。
                System.Windows.MessageBox.Show("删除失败：\n" + ex.Message); // 提示失败原因，便于排查。
            } // catch 代码块结束。
        } // 方法结束。




    } // 类结束。
} // 命名空间结束。
