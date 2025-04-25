using BookSteward.Models;
using CommunityToolkit.Mvvm.ComponentModel; // Using MVVM Toolkit
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using BookSteward.Services;
using BookSteward.Utility;
using System.Threading.Tasks;

namespace BookSteward.ViewModels;

public partial class BookViewModel : ObservableObject
{
    private readonly Book book;

    public BookViewModel(Book book)
    {
        this.book = book ?? throw new ArgumentNullException(nameof(book));
        
        // 从Book模型初始化属性
        title = book.Title;
        author = book.Author;
        publisher = book.Publisher;
        description = book.Description;
        filePath = book.FilePath;
        addedDate = book.ImportDate;
        lastOpenedDate = book.LastOpenedDate;
        isFavorite = book.IsFavorite;
        
        // 初始化文件扩展名和标签集合
        FileExtensions = new ObservableCollection<string>(book.FileExtensions);
        FormatTags = new ObservableCollection<string>();
        
        // 提取格式标签
        if (book.Tags != null)
        {
            foreach (var tag in book.Tags)
            {
                if (tag.Name.StartsWith("格式:"))
                {
                    FormatTags.Add(tag.Name.Substring(3)); // 移除"格式:"前缀
                }
            }
        }
        
        // 初始化收藏命令
        ToggleFavoriteCommand = new AsyncRelayCommand(ToggleFavoriteAsync);
    }

    public int Id => book.Id;
    
    // 收藏相关属性和命令
    [ObservableProperty]
    private bool isFavorite;
    
    public IAsyncRelayCommand ToggleFavoriteCommand { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))] // Update DisplayTitle when Title changes
    private string? title;

    [ObservableProperty]
    private string? author;

    [ObservableProperty]
    private string? publisher;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private string? filePath;

    [ObservableProperty]
    private DateTime? addedDate;

    [ObservableProperty]
    private DateTime? lastOpenedDate;

    public string DisplayTitle => string.IsNullOrEmpty(Title) ? "(无标题)" : Title;

    public string TagsDisplay => book.Tags != null ? string.Join(", ", book.Tags.Select(t => t.Name)) : string.Empty;

    public ICollection<Tag>? Tags => book.Tags;
    
    // 文件扩展名集合
    public ObservableCollection<string> FileExtensions { get; }
    
    // 格式标签集合
    public ObservableCollection<string> FormatTags { get; }
    
    // 格式标签显示
    public string FormatTagsDisplay => FormatTags.Count > 0 ? string.Join(", ", FormatTags) : string.Empty;
    
    // 是否缺失信息
    public bool IsInfoIncomplete => book.IsInfoIncomplete;
    
    // 是否为新书
    public bool IsNew => book.IsNew;

    public Book GetModel() => book;

    public void UpdateModel()
    {
        book.Title = Title;
        book.Author = Author;
        book.Description = Description;
        book.FilePath = FilePath;
        book.ImportDate = AddedDate ?? DateTime.UtcNow; 
        book.LastOpenedDate = LastOpenedDate;
    }

    /// <summary>
    /// 切换书籍的收藏状态
    /// </summary>
    private async Task ToggleFavoriteAsync()
    {
        try
        {
            // 切换收藏状态
            IsFavorite = !IsFavorite;

            // 更新底层Book模型
            book.IsFavorite = IsFavorite;

            // 保存到数据库
            // 注意：这里需要通过DI获取BookService，但为简化实现，我们直接使用App.Current获取服务
            var bookService = ((App)Application.Current).ServiceProvider.GetService(typeof(IBookService)) as IBookService;
            if (bookService != null)
            {
                await bookService.UpdateBookAsync(book);
            }
        }
        catch (Exception ex)
        {
            // 记录错误
            Log.Error(ex, "切换书籍收藏状态时出错: {BookId}, {Title}", Id, Title);

            // 恢复状态
            IsFavorite = !IsFavorite;
            book.IsFavorite = IsFavorite;
        }
    }

}