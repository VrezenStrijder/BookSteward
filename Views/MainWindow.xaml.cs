using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using BookSteward.Services;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookSteward.ViewModels;
using BookSteward.Models;
using BookSteward.Utility;
using System.Collections.ObjectModel;

namespace BookSteward;


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly BookImportService bookImportService;

    public MainWindow(BookImportService bookImportService, ViewModels.MainWindowViewModel viewModel)
    {
        InitializeComponent();

        // 确保导航栏初始状态为展开状态
        NavToggleButton.IsChecked = true;
        NavTitle.Visibility = Visibility.Visible;

        // 注册拖放事件
        this.AllowDrop = true;
        this.Drop += MainWindow_Drop;
        this.DragOver += MainWindow_DragOver;

        // 获取服务实例
        this.bookImportService = bookImportService ?? throw new ArgumentNullException(nameof(bookImportService));

        // 设置DataContext
        this.DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <summary>
    /// 处理导航栏折叠按钮的选中事件（折叠导航栏）
    /// </summary>
    private void NavToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        // 播放展开动画
        Storyboard expandStoryboard = (Storyboard)FindResource("ExpandNavigation");
        expandStoryboard.Begin();

        // 显示标题文本
        NavTitle.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 处理导航栏折叠按钮的取消选中事件（展开导航栏）
    /// </summary>
    private void NavToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        // 播放折叠动画
        Storyboard collapseStoryboard = (Storyboard)FindResource("CollapseNavigation");
        collapseStoryboard.Begin();

        // 隐藏标题文本
        NavTitle.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 处理搜索框获取焦点事件
    /// </summary>
    private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox != null)
        {
            // 如果当前文本是默认提示文本，则清空
            if (textBox == TopSearchTextBox && textBox.Text.Equals(MainWindowViewModel.DefaultSearchText))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }
            else if (textBox == NavSearchTextBox && textBox.Equals(MainWindowViewModel.DefaultMenuSearchText))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }

    /// <summary>
    /// 处理搜索框失去焦点事件
    /// </summary>
    private void CategoryNameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is CategoryViewModel category)
        {
            if (textBox.Visibility != Visibility.Visible)
            {
                return;
            }

            category.IsEditing = false;

            // 如果名称为空，恢复为原始名称
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                category.Name = "新类别";
            }

            // 触发重命名命令
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.RenameCategoryCommand.Execute(category);
            }
        }
    }

    /// <summary>
    /// 处理搜索框按键事件，按下Enter键时触发搜索
    /// </summary>
    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SearchBooksCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    //private void CategoryNameTextBox_KeyDown(object sender, KeyEventArgs e)
    //{
    //    if (sender is TextBox textBox && textBox.DataContext is CategoryViewModel category)
    //    {
    //        if (e.Key == Key.Enter)
    //        {
    //            category.IsEditing = false;

    //            // 如果名称为空，恢复为原始名称
    //            if (string.IsNullOrWhiteSpace(category.Name))
    //            {
    //                category.Name = "新类别";
    //            }

    //            // 触发重命名命令
    //            if (DataContext is MainWindowViewModel viewModel)
    //            {
    //                viewModel.RenameCategoryCommand.Execute(category);
    //            }

    //            e.Handled = true;
    //        }
    //        else if (e.Key == Key.Escape)
    //        {
    //            category.IsEditing = false;
    //            e.Handled = true;
    //        }
    //    }
    //}

    //{
    //    var textBox = (TextBox)sender;
    //    var category = (CategoryViewModel)textBox.DataContext;
    //    ((MainWindowViewModel)DataContext).RenameCategoryCommand.Execute(category);
    //}

    private void CategoryNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = (TextBox)sender;
            textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox != null)
        {
            // 如果文本框为空，则恢复默认提示文本
            if (textBox == TopSearchTextBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = MainWindowViewModel.DefaultSearchText;
                textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888"));
            }
            else if (textBox == NavSearchTextBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = MainWindowViewModel.DefaultMenuSearchText;
                textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888"));
            }
        }
    }

    /// <summary>
    /// 处理顶部搜索框鼠标进入事件
    /// </summary>
    private void TopSearchBox_MouseEnter(object sender, MouseEventArgs e)
    {
        // 播放搜索框展开动画
        Storyboard expandStoryboard = (Storyboard)FindResource("ExpandSearchBox");
        expandStoryboard.Begin();
    }

    /// <summary>
    /// 处理顶部搜索框鼠标离开事件
    /// </summary>
    private void TopSearchBox_MouseLeave(object sender, MouseEventArgs e)
    {
        // 如果搜索框没有焦点，则播放收缩动画
        if (!TopSearchTextBox.IsFocused)
        {
            Storyboard collapseStoryboard = (Storyboard)FindResource("CollapseSearchBox");
            collapseStoryboard.Begin();
        }
    }

    /// <summary>
    /// 处理拖放文件到窗口的事件
    /// </summary>
    private async void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (bookImportService == null)
        {
            MessageBox.Show("书籍导入服务不可用", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // 获取拖放的文件路径
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // 显示导入进度
                if (DataContext is ViewModels.MainWindowViewModel viewModel)
                {
                    viewModel.StatusText = $"正在导入 {files.Length} 个文件...";
                }

                // 过滤出支持的文件格式
                var supportedFiles = files.Where(f => bookImportService.IsSupportedBookFormat(f)).ToList();
                var unsupportedFiles = files.Except(supportedFiles).ToList();

                if (unsupportedFiles.Any())
                {
                    MessageBox.Show(
                        $"以下 {unsupportedFiles.Count} 个文件格式不受支持:\n{string.Join("\n", unsupportedFiles.Take(5))}{(unsupportedFiles.Count > 5 ? "\n..." : "")}",
                        "不支持的文件格式",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                if (supportedFiles.Any())
                {
                    try
                    {
                        // 导入书籍
                        var importedBooks = await bookImportService.ImportBooksAsync(supportedFiles);

                        // 更新视图模型，刷新"新加入的书籍"列表
                        if (DataContext is ViewModels.MainWindowViewModel vm)
                        {
                            // 如果当前是在"新加入的书籍"视图，则直接刷新该视图
                            // 否则提示用户查看"新加入的书籍"视图
                            if (vm.CurrentView == "新加入的书籍")
                            {
                                await vm.LoadBooksCommand.ExecuteAsync(null);
                            }
                            else if (importedBooks.Any())
                            {
                                // 切换到"新加入的书籍"视图
                                vm.ChangeViewCommand.Execute("新加入的书籍");
                            }

                            vm.StatusText = $"成功导入 {importedBooks.Count()} 本书籍";
                        }
                        else
                        {
                            // 无法更新ViewModel状态
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导入书籍时发生错误: {ex.Message}", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (DataContext is ViewModels.MainWindowViewModel vm)
                        {
                            vm.StatusText = "导入失败";
                        }
                    }
                }
                else
                {
                    if (DataContext is ViewModels.MainWindowViewModel vm)
                    {
                        vm.StatusText = "没有可导入的文件";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 处理拖动文件到窗口上方的事件
    /// </summary>
    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // 显示可以放置的视觉提示
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 处理导航项点击事件
    /// </summary>
    private void NavItem_Click(object sender, RoutedEventArgs e)
    {
        // 处理导航项点击事件
        if (sender is FrameworkElement element && element.Tag is string tag && DataContext is ViewModels.MainWindowViewModel vm)
        {
            // 直接设置当前视图为tag值
            vm.CurrentView = tag;
            
            // 根据Tag切换内容
            switch (tag)
            {
                case "AllBooks":
                    // 显示所有书籍
                    vm.ChangeViewCommand.Execute(tag);
                    break;
                case "NewBooks":
                    // 显示新书
                    vm.ChangeViewCommand.Execute(tag);
                    break;
                case "Favorites":
                    // 显示收藏
                    vm.ChangeViewCommand.Execute(tag);
                    break;
                case "RecentBooks":
                    // 显示最近阅读
                    vm.ChangeViewCommand.Execute(tag);
                    break;
                case "IncompleteInfo":
                    // 显示信息待完善区
                    vm.ChangeViewCommand.Execute(tag);
                    break;
                case "Settings":
                    // 显示设置
                    // 暂未实现
                    break;
                default:
                    // 如果是分类ID，直接执行ChangeView命令
                    vm.ChangeViewCommand.Execute(tag);
                    break;
            }
        }
    }

    /// <summary>
    /// 处理分类项点击事件
    /// </summary>
    private void CategoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int tag && DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.ChangeCategoryCommand.Execute(tag);
        }
    }
    /// <summary>
    /// 处理书籍项点击事件
    /// </summary>
    private async void BookItem_Click(object sender, RoutedEventArgs e)
    {
        // 处理书籍项点击事件
        if (sender is FrameworkElement element && element.DataContext is BookViewModel book && DataContext is ViewModels.MainWindowViewModel vm)
        {
            // 使用ViewModel的OpenBookCommand打开书籍
            await vm.OpenBookCommand.ExecuteAsync(book);
        }
    }

    #region 书籍类别区事件

    //private void CreateCategory_Click(object sender, RoutedEventArgs e)
    //{
    //    if (DataContext is ViewModels.MainWindowViewModel vm)
    //    {
    //        var tempCategory = new Category
    //        {
    //            Name = "新类别",
    //            ParentId = null,
    //            Id = -1 // 临时ID，表示尚未保存到数据库
    //        };

    //        var categoryViewModel = new CategoryViewModel(tempCategory);
    //        categoryViewModel.IsEditing = true; // 设置为编辑状态

    //        vm.Categories.Add(categoryViewModel);
    //        vm.StatusText = "请输入分类名称";


    //    }
    //}
    

    private void NewCategoryTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb != null)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private async void MenuItem_CreateSubCategory(object sender, RoutedEventArgs e)
    {
        if ((sender is FrameworkElement element) && (element.DataContext is CategoryViewModel category) && (DataContext is ViewModels.MainWindowViewModel vm))
        {
            await vm.CreateSubCategoryCommand.ExecuteAsync(category);
        }
    }

    private async void MenuItem_RenameCategory(object sender, RoutedEventArgs e)
    {
        if ((sender is FrameworkElement element) && (element.DataContext is CategoryViewModel category) && (DataContext is ViewModels.MainWindowViewModel vm))
        {
            category.IsEditing = true;

            // 使用Dispatcher延迟执行，确保UI已更新  
            await Dispatcher.InvokeAsync(() =>
            {

                var contextMenu = (element as MenuItem)?.Parent as ContextMenu;

                if (contextMenu != null)
                {
                    Button btn = contextMenu.PlacementTarget as Button;

                    if (btn != null)
                    {
                        btn.Focus();

                        var textBox = btn.FindDescendant<TextBox>();
                        if (textBox != null)
                        {
                            textBox.Focus();
                            textBox.SelectAll();
                        }
                    }
                }

            }, System.Windows.Threading.DispatcherPriority.Render);
        }
    }

    private async void MenuItem_DeleteCategory(object sender, RoutedEventArgs e)
    {
        if ((sender is FrameworkElement element) && (element.DataContext is CategoryViewModel category) && (DataContext is ViewModels.MainWindowViewModel vm))
        {
            await vm.DeleteCategoryCommand.ExecuteAsync(category);
        }
    }

    #endregion

    #region 书籍右键菜单事件
    
    /// <summary>
    /// 处理打开书籍菜单项点击事件
    /// </summary>
    private async void MenuItem_OpenBook(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm && vm.SelectedBook != null)
        {
            await vm.OpenBookCommand.ExecuteAsync(vm.SelectedBook);
        }
    }
    
    /// <summary>
    /// 处理添加到分类菜单项点击事件
    /// </summary>
    private void MenuItem_AddToCategory(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm && BookListView.SelectedItems.Count > 0)
        {
            // 创建一个包含所有分类的弹出菜单
            ContextMenu categoryMenu = new ContextMenu();
            
            // 使用分类树模板填充菜单项
            foreach (var category in vm.Categories)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = category.Name,
                    Tag = category.Id
                };
                menuItem.Click += CategoryMenuItem_Click;
                
                // 添加子分类
                AddSubCategories(menuItem, category.Children);
                
                categoryMenu.Items.Add(menuItem);
            }
            
            // 显示弹出菜单
            categoryMenu.IsOpen = true;
            categoryMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            categoryMenu.PlacementTarget = BookListView;
        }
    }
    
    /// <summary>
    /// 递归添加子分类到菜单项
    /// </summary>
    private void AddSubCategories(MenuItem parentMenuItem, ObservableCollection<CategoryViewModel> subCategories)
    {
        if (subCategories == null || subCategories.Count == 0) return;
        
        foreach (var subCategory in subCategories)
        {
            MenuItem subMenuItem = new MenuItem
            {
                Header = subCategory.Name,
                Tag = subCategory.Id
            };
            subMenuItem.Click += CategoryMenuItem_Click;
            
            // 递归添加子分类
            AddSubCategories(subMenuItem, subCategory.Children);
            
            parentMenuItem.Items.Add(subMenuItem);
        }
    }
    
    /// <summary>
    /// 处理分类菜单项点击事件
    /// </summary>
    private async void CategoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is int categoryId && DataContext is ViewModels.MainWindowViewModel vm)
        {
            // 获取选中的书籍
            var selectedBooks = new List<BookViewModel>();
            foreach (BookViewModel book in BookListView.SelectedItems)
            {
                selectedBooks.Add(book);
            }
            
            // 将选中的书籍添加到指定分类
            if (selectedBooks.Count > 0)
            {
                // 对每本选中的书籍执行添加到分类的操作
                foreach (var book in selectedBooks)
                {
                    await vm.AddBooksToCategoryCommand.ExecuteAsync((book, categoryId));
                }
                
                vm.StatusText = $"已将 {selectedBooks.Count} 本书籍添加到分类";
            }
        }
    }
    
    /// <summary>
    /// 处理编辑书籍信息菜单项点击事件
    /// </summary>
    private void MenuItem_EditBookInfo(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm && vm.SelectedBook != null)
        {
            // 打开编辑书籍信息对话框
            // 这里需要实现编辑书籍信息的功能
            vm.StatusText = "编辑书籍信息功能尚未实现";
        }
    }
    
    #endregion


}