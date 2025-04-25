using BookSteward.Models;
using BookSteward.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using Ookii.Dialogs.Wpf;
using BookSteward.Utility;
using BookSteward.Data;

namespace BookSteward.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IBookService bookService;
        private readonly ICategoryService categoryService;
        private readonly BookImportService bookImportService;

        public const string DefaultSearchText = "搜索书籍...";
        public const string DefaultMenuSearchText = "搜索菜单...";


        [ObservableProperty]
        private string windowTitle = "BookSteward";

        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> categories = new ObservableCollection<CategoryViewModel>();

        [ObservableProperty]
        private ObservableCollection<BookViewModel> books = new ObservableCollection<BookViewModel>();

        [ObservableProperty]
        private string currentView = "AllBooks"; // 当前选中的视图（全部书籍、新加入的书籍等）

        [ObservableProperty]
        private BookViewModel? selectedBook;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string statusText = "Ready";

        // [ObservableProperty]
        // private ObservableCollection<NavigationItemViewModel> navigationItems;
        // [ObservableProperty]
        // private object currentViewModel;
        // [ObservableProperty]
        // private string statusText = "Ready";

        public MainWindowViewModel(IBookService bookService, ICategoryService categoryService, BookImportService bookImportService)
        {
            this.bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            this.categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            this.bookImportService = bookImportService ?? throw new ArgumentNullException(nameof(bookImportService));
            //this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            LoadBooksCommand = new AsyncRelayCommand(LoadBooksAsync);
            SearchBooksCommand = new AsyncRelayCommand(SearchBooksAsync);
            ChangeViewCommand = new RelayCommand<string>(ChangeView);
            ChangeCategoryCommand = new RelayCommand<int>(ChangeCategory);
            OpenBookCommand = new AsyncRelayCommand<BookViewModel>(OpenBookAsync);
            CreateCategoryCommand = new AsyncRelayCommand<string>(CreateCategoryAsync);
            DeleteCategoryCommand = new AsyncRelayCommand<CategoryViewModel>(DeleteCategoryAsync);
            RenameCategoryCommand = new AsyncRelayCommand<CategoryViewModel>(RenameCategoryAsync);
            ImportDirectoryCommand = new AsyncRelayCommand(ImportDirectoryAsync);
            ImportFileCommand = new AsyncRelayCommand(ImportFileAsync);
            RefreshBooksCommand = new AsyncRelayCommand(LoadBooksAsync);
            CreateSubCategoryCommand = new AsyncRelayCommand<CategoryViewModel>(CreateSubCategoryAsync);
            AddBooksToCategoryCommand = new AsyncRelayCommand<(BookViewModel Book, int CategoryId)>(AddBookToCategoryAsync);

            _ = LoadBooksAsync();
            _ = LoadCategoriesAsync();
        }

        // --- Commands ---
        public IAsyncRelayCommand LoadBooksCommand { get; }
        public IAsyncRelayCommand SearchBooksCommand { get; }
        public IRelayCommand<string> ChangeViewCommand { get; }
        public IRelayCommand<int> ChangeCategoryCommand { get; }

        public IAsyncRelayCommand<BookViewModel> OpenBookCommand { get; }
        public IAsyncRelayCommand<string> CreateCategoryCommand { get; }
        public IAsyncRelayCommand<CategoryViewModel> DeleteCategoryCommand { get; }
        public IAsyncRelayCommand<CategoryViewModel> RenameCategoryCommand { get; }
        public IAsyncRelayCommand ImportDirectoryCommand { get; }
        public IAsyncRelayCommand ImportFileCommand { get; }
        public IAsyncRelayCommand RefreshBooksCommand { get; }
        public IAsyncRelayCommand<CategoryViewModel> CreateSubCategoryCommand { get; }
        public IAsyncRelayCommand<(BookViewModel Book, int CategoryId)> AddBooksToCategoryCommand { get; }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var rootCategories = await categoryService.GetRootCategoriesAsync();
                Categories.Clear();
                foreach (var category in rootCategories)
                {
                    Categories.Add(new CategoryViewModel(category));
                }
            }
            catch (Exception ex)
            {
                StatusText = "加载分类时出错";
                Log.Error(ex, "加载分类时出错");
            }
        }

        private async Task CreateCategoryAsync(string? name = null)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    // 第一步：点击加号按钮时，只创建临时分类项，设置为编辑状态
                    var tempCategory = new Category
                    {
                        Name = "新类别",
                        ParentId = null,
                        Id = -1
                    };

                    var categoryViewModel = new CategoryViewModel(tempCategory);
                    categoryViewModel.IsEditing = true; // 设置为编辑状态
                    Categories.Add(categoryViewModel);

                    StatusText = "请输入分类名称";
                }
            }
            catch (Exception ex)
            {
                StatusText = "创建分类时出错";
                Log.Error(ex, "创建分类时出错");
            }
        }

        private async Task CreateSubCategoryAsync(CategoryViewModel parentCategory)
        {
            if (parentCategory == null) return;

            try
            {
                // 创建临时子分类项，设置为编辑状态
                var tempCategory = new Category
                {
                    Name = "新子类别",
                    ParentId = parentCategory.Id,
                    Id = -1
                };

                var categoryViewModel = new CategoryViewModel(tempCategory);
                categoryViewModel.IsEditing = true; // 设置为编辑状态
                parentCategory.Children.Add(categoryViewModel);

                // 使用Dispatcher延迟执行，确保UI已更新后设置焦点
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // 等待UI更新
                    await Task.Delay(100);

                    // 查找新创建的子分类文本框并设置焦点
                    var window = Application.Current.MainWindow;
                    if (window != null)
                    {
                        var textBoxes = window.FindVisualChildren<System.Windows.Controls.TextBox>();
                        foreach (var textBox in textBoxes)
                        {
                            if (textBox.DataContext == categoryViewModel && textBox.IsVisible)
                            {
                                textBox.Focus();
                                textBox.SelectAll();
                                break;
                            }
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);

                StatusText = "请输入子分类名称";
            }
            catch (Exception ex)
            {
                StatusText = "创建子分类时出错";
                Log.Error(ex, "创建子分类时出错");
            }
        }

        private async Task DeleteCategoryAsync(CategoryViewModel category)
        {
            if (category == null) return;

            try
            {
                await categoryService.DeleteCategoryAsync(category.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadCategoriesAsync();
                });
                StatusText = "已删除分类";
            }
            catch (Exception ex)
            {
                StatusText = "删除分类时出错";
                Log.Error(ex, "删除分类时出错");
            }
        }

        private async Task RenameCategoryAsync(CategoryViewModel category)
        {
            if (category == null)
            {
                return;
            }
            try
            {
                if (category.Id == -1)
                {
                    var newCategory = await categoryService.CreateCategoryAsync(category.Name, category.ParentId);
                    var categoryViewModel = new CategoryViewModel(newCategory);
                    StatusText = "已创建新分类";
                }
                else
                {
                    var item = await categoryService.UpdateCategoryAsync(category.Id, category.Name);
                    if (item != null)
                    {
                        StatusText = "已重命名分类";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "重命名分类时出错";
                Log.Error(ex, "重命名分类时出错");
            }
        }

        /// <summary>
        /// 导入目录中的书籍文件
        /// </summary>
        private async Task ImportDirectoryAsync()
        {
            try
            {
                var dialog = new VistaFolderBrowserDialog()
                {
                    Description = "选择包含书籍的文件夹",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    StatusText = $"正在扫描文件夹: {dialog.SelectedPath}";

                    // 获取目录中所有文件
                    var files = Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => bookImportService.IsSupportedBookFormat(file))
                        .ToList();

                    if (files.Count == 0)
                    {
                        StatusText = "未找到支持的书籍文件";
                        return;
                    }

                    StatusText = $"找到 {files.Count} 个书籍文件，正在导入...";
                    var importedBooks = await bookImportService.ImportBooksAsync(files);

                    // 刷新当前视图
                    await LoadBooksAsync();

                    StatusText = $"成功导入 {importedBooks.Count()} 本书籍";
                    Log.Information("从目录导入完成: {Directory}, 成功导入: {Count}本", dialog.SelectedPath, importedBooks.Count());
                }
            }
            catch (Exception ex)
            {
                StatusText = "导入目录时出错";
                Log.Error(ex, "导入目录时出错");
                MessageBox.Show($"导入目录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入选择的书籍文件
        /// </summary>
        private async Task ImportFileAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "选择要导入的书籍文件",
                    Filter = "书籍文件|*.pdf;*.epub;*.mobi;*.txt;*.azw3|所有文件|*.*",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true)
                {
                    var files = dialog.FileNames;
                    if (files.Length == 0) return;

                    StatusText = $"正在导入 {files.Length} 个文件...";
                    var importedBooks = await bookImportService.ImportBooksAsync(files);

                    // 刷新当前视图
                    await LoadBooksAsync();

                    StatusText = $"成功导入 {importedBooks.Count()} 本书籍";
                    Log.Information("文件导入完成, 成功导入: {Count}本", importedBooks.Count());
                }
            }
            catch (Exception ex)
            {
                StatusText = "导入文件时出错";
                Log.Error(ex, "导入文件时出错");
                MessageBox.Show($"导入文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 将书籍添加到指定分类
        /// </summary>
        private async Task AddBookToCategoryAsync((BookViewModel Book, int CategoryId) param)
        {
            try
            {
                if (param.Book == null)
                {
                    return;
                }
                StatusText = "正在添加书籍到分类...";

                // 获取书籍和分类
                var book = await bookService.GetBookByIdAsync(param.Book.Id);
                var category = await categoryService.GetCategoryByIdAsync(param.CategoryId);

                if (book == null || category == null)
                {
                    StatusText = "添加书籍到分类失败：找不到书籍或分类";
                    return;
                }

                // 将书籍添加到分类
                if (!category.Books.Any(b => b.Id == book.Id))
                {
                    category.Books.Add(book);
                    await categoryService.UpdateCategoryBooksAsync(category.Id, category.Books.ToList());
                    StatusText = $"已将《{book.Title}》添加到分类 '{category.Name}'";
                }
                else
                {
                    StatusText = $"书籍《{book.Title}》已在分类 '{category.Name}' 中";
                }
            }
            catch (Exception ex)
            {
                StatusText = "添加书籍到分类时出错";
                Log.Error(ex, "添加书籍到分类时出错");
            }
        }

        private async Task LoadBooksAsync()
        {
            try
            {
                StatusText = "正在加载书籍...";
                IEnumerable<Book> books;

                // 清空当前书籍列表，修复重复显示问题
                Books.Clear();

                // 根据当前视图加载不同的书籍集合
                switch (CurrentView)
                {
                    case "NewBooks":
                        books = await bookService.GetAllBooksAsync();
                        books = books.Where(b => b.IsNew);
                        StatusText = "新加入的书籍";
                        break;
                    case "Favorites":
                        // 显示收藏的书籍
                        books = await bookService.GetAllBooksAsync();
                        books = books.Where(b => b.IsFavorite);
                        StatusText = "收藏书籍";
                        break;
                    case "RecentBooks":
                        books = await bookService.GetAllBooksAsync();
                        books = books.Where(b => b.LastOpenedDate != null)
                                     .OrderByDescending(b => b.LastOpenedDate);
                        StatusText = "最近阅读的书籍";
                        break;
                    case "IncompleteInfo":
                        books = await bookService.GetAllBooksAsync();
                        books = books.Where(b => b.IsInfoIncomplete);
                        StatusText = "信息待完善的书籍";
                        break;
                    case "AllBooks":
                        books = await bookService.GetAllBooksAsync();
                        StatusText = "所有书籍";
                        break;
                    default:
                        // 检查是否是分类ID
                        if (int.TryParse(CurrentView, out int categoryId))
                        {
                            // 加载指定分类的书籍
                            var category = await categoryService.GetCategoryByIdAsync(categoryId);
                            if (category != null)
                            {
                                books = category.Books;
                                StatusText = $"分类 '{category.Name}' 中的书籍";
                            }
                            else
                            {
                                books = await bookService.GetAllBooksAsync();
                                StatusText = "所有书籍";
                            }
                        }
                        else
                        {
                            books = await bookService.GetAllBooksAsync();
                            StatusText = "所有书籍";
                        }
                        break;
                }

                // 处理同名不同格式的书籍合并显示
                var bookGroups = new Dictionary<string, List<Book>>();
                foreach (var book in books)
                {
                    string key = book.Title.ToLower(); // 使用标题作为分组键
                    if (!bookGroups.ContainsKey(key))
                    {
                        bookGroups[key] = new List<Book>();
                    }
                    bookGroups[key].Add(book);
                }

                // 为每组书籍创建一个BookViewModel
                foreach (var group in bookGroups)
                {
                    if (group.Value.Count == 1)
                    {
                        // 只有一本书，直接添加
                        Books.Add(new BookViewModel(group.Value[0]));
                    }
                    else
                    {
                        // 多本同名书籍，合并显示
                        var primaryBook = group.Value[0]; // 使用第一本作为主要书籍

                        // 合并所有扩展名
                        foreach (var book in group.Value.Skip(1))
                        {
                            foreach (var ext in book.FileExtensions)
                            {
                                if (!primaryBook.FileExtensions.Contains(ext))
                                {
                                    primaryBook.FileExtensions.Add(ext);
                                }
                            }

                            // 合并标签
                            foreach (var tag in book.Tags)
                            {
                                if (!primaryBook.Tags.Any(t => t.Name == tag.Name))
                                {
                                    primaryBook.Tags.Add(tag);
                                }
                            }
                        }

                        Books.Add(new BookViewModel(primaryBook));
                    }
                }

                StatusText = $"已加载 {Books.Count} 本书籍";
            }
            catch (Exception ex)
            {
                StatusText = "加载书籍时出错";
                Log.Error(ex, "加载书籍时出错");
            }
        }

        private async Task SearchBooksAsync()
        {
            StatusText = $"正在搜索 '{SearchText}'...";
            try
            {
                IEnumerable<Book> results;
                if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Equals(DefaultSearchText))
                {
                    // 如果搜索文本为空，则根据当前视图加载书籍
                    await LoadBooksAsync();
                    return;
                }
                else
                {
                    results = await bookService.SearchBooksAsync(SearchText);

                    // 如果当前视图是"新加入的书籍"，则只显示IsNew为true的搜索结果
                    if (CurrentView == "新加入的书籍")
                    {
                        results = results.Where(b => b.IsNew);
                    }
                }

                Books.Clear();
                foreach (var book in results)
                {
                    Books.Add(new BookViewModel(book));
                }
                StatusText = $"找到 {Books.Count} 本匹配 '{SearchText}' 的书籍";
                Log.Information("搜索完成. 找到 {BookCount} 本书.(查询语句: {Query})", Books.Count, SearchText);
            }
            catch (Exception ex)
            {
                StatusText = "搜索书籍时出错";
                Log.Error(ex, "搜索书籍时出错.(查询语句: {Query})", SearchText);
            }
        }

        // 切换视图（全部书籍、新加入的书籍等）
        private void ChangeView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName)) return;

            CurrentView = viewName;

            // 根据视图名称加载相应的数据
            _ = LoadBooksAsync();
        }

        private void ChangeCategory(int categoryId)
        {
            // 将当前视图设置为分类ID（字符串形式）
            CurrentView = categoryId.ToString();

            // 加载该分类下的书籍
            _ = LoadBooksAsync();

            StatusText = $"正在加载分类ID: {categoryId} 的书籍...";
        }

        // 打开书籍并更新IsNew状态
        private async Task OpenBookAsync(BookViewModel bookViewModel)
        {
            if (bookViewModel == null) return;

            try
            {
                var book = await bookService.GetBookByIdAsync(bookViewModel.Id);
                if (book != null)
                {
                    // 更新最后打开时间
                    book.LastOpenedDate = DateTime.Now;

                    // 如果是新书，则将IsNew设置为false
                    if (book.IsNew)
                    {
                        book.IsNew = false;
                        Log.Information("更新书籍IsNew状态: {BookId}, {Title}", book.Id, book.Title);
                    }

                    // 保存更改
                    await bookService.UpdateBookAsync(book);

                    // 如果当前视图是"新加入的书籍"，并且书籍已不再是新书，则从列表中移除
                    if (CurrentView == "新加入的书籍" && !book.IsNew)
                    {
                        // 从当前视图中移除该书籍
                        var bookToRemove = Books.FirstOrDefault(b => b.Id == book.Id);
                        if (bookToRemove != null)
                        {
                            Books.Remove(bookToRemove);
                        }
                    }

                    // 这里可以添加打开书籍的逻辑，例如启动默认应用程序打开文件
                    try
                    {
                        // 使用系统默认程序打开文件
                        var process = new System.Diagnostics.Process();
                        process.StartInfo.FileName = book.FilePath;
                        process.StartInfo.UseShellExecute = true;
                        process.Start();

                        StatusText = $"已打开: {book.Title}";
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "无法打开书籍: {FilePath}", book.FilePath);
                        StatusText = "无法打开文件";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "打开书籍 {BookId} 时出错.", bookViewModel.Id);
                StatusText = "打开书籍时出错";
            }
        }
    }
}