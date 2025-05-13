using BookSteward.Models;
using BookSteward.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BookSteward.ViewModels
{
    public partial class DirectoryCompareViewModel : ObservableObject
    {
        private readonly BookImportService bookImportService;

        [ObservableProperty]
        private ObservableCollection<BookViewModel> leftDirectoryBooks = new ObservableCollection<BookViewModel>();

        [ObservableProperty]
        private ObservableCollection<BookViewModel> rightDirectoryBooks = new ObservableCollection<BookViewModel>();

        [ObservableProperty]
        private ObservableCollection<ComparisonResultGroup> comparisonResults = new ObservableCollection<ComparisonResultGroup>();

        [ObservableProperty]
        private string leftDirectoryPath = string.Empty;

        [ObservableProperty]
        private string rightDirectoryPath = string.Empty;

        [ObservableProperty]
        private string leftStatusText = "未导入目录";

        [ObservableProperty]
        private string rightStatusText = "未导入目录";

        [ObservableProperty]
        private bool isComparing = false;

        [ObservableProperty]
        private int exactMatchCount = 0;

        [ObservableProperty]
        private int titleMatchCount = 0;

        [ObservableProperty]
        private int authorMatchCount = 0;

        [ObservableProperty]
        private int similarTitleCount = 0;

        [ObservableProperty]
        private int leftTotalCount = 0;

        [ObservableProperty]
        private int rightTotalCount = 0;

        public DirectoryCompareViewModel(BookImportService bookImportService)
        {
            this.bookImportService = bookImportService ?? throw new ArgumentNullException(nameof(bookImportService));
            
            ImportLeftDirectoryCommand = new AsyncRelayCommand(ImportLeftDirectoryAsync);
            ImportRightDirectoryCommand = new AsyncRelayCommand(ImportRightDirectoryAsync);
            CompareDirectoriesCommand = new AsyncRelayCommand(CompareDirectoriesAsync);
            ClearComparisonCommand = new RelayCommand(ClearComparison);
        }

        public IAsyncRelayCommand ImportLeftDirectoryCommand { get; }
        public IAsyncRelayCommand ImportRightDirectoryCommand { get; }
        public IAsyncRelayCommand CompareDirectoriesCommand { get; }
        public IRelayCommand ClearComparisonCommand { get; }

        private async Task ImportLeftDirectoryAsync()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "选择要比较的左侧目录",
                UseDescriptionForTitle = true
            };

            if (folderDialog.ShowDialog() == true)
            {
                LeftDirectoryPath = folderDialog.SelectedPath;
                LeftStatusText = "正在导入目录...";
                
                try
                {
                    // 获取目录中的所有支持格式的书籍文件
                    var bookFiles = Directory.GetFiles(LeftDirectoryPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => bookImportService.IsSupportedBookFormat(file))
                        .ToList();

                    // 清空当前列表
                    LeftDirectoryBooks.Clear();
                    
                    // 导入书籍（仅解析元数据，不保存到数据库）
                    var books = await bookImportService.ParseBooksMetadataAsync(bookFiles);
                    
                    // 转换为视图模型并添加到列表
                    foreach (var book in books)
                    {
                        LeftDirectoryBooks.Add(new BookViewModel(book));
                    }

                    LeftTotalCount = LeftDirectoryBooks.Count;
                    LeftStatusText = $"已导入 {LeftDirectoryBooks.Count} 本书籍";

                    // 如果两边都已导入，自动进行比较
                    if (LeftDirectoryBooks.Count > 0 && RightDirectoryBooks.Count > 0)
                    {
                        await CompareDirectoriesAsync();
                    }
                }
                catch (Exception ex)
                {
                    LeftStatusText = "导入失败";
                    MessageBox.Show($"导入目录时发生错误: {ex.Message}", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ImportRightDirectoryAsync()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "选择要比较的右侧目录",
                UseDescriptionForTitle = true
            };

            if (folderDialog.ShowDialog() == true)
            {
                RightDirectoryPath = folderDialog.SelectedPath;
                RightStatusText = "正在导入目录...";
                
                try
                {
                    // 获取目录中的所有支持格式的书籍文件
                    var bookFiles = Directory.GetFiles(RightDirectoryPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => bookImportService.IsSupportedBookFormat(file))
                        .ToList();

                    // 清空当前列表
                    RightDirectoryBooks.Clear();
                    
                    // 导入书籍（仅解析元数据，不保存到数据库）
                    var books = await bookImportService.ParseBooksMetadataAsync(bookFiles);
                    
                    // 转换为视图模型并添加到列表
                    foreach (var book in books)
                    {
                        RightDirectoryBooks.Add(new BookViewModel(book));
                    }

                    RightTotalCount = RightDirectoryBooks.Count;
                    RightStatusText = $"已导入 {RightDirectoryBooks.Count} 本书籍";

                    // 如果两边都已导入，自动进行比较
                    if (LeftDirectoryBooks.Count > 0 && RightDirectoryBooks.Count > 0)
                    {
                        await CompareDirectoriesAsync();
                    }
                }
                catch (Exception ex)
                {
                    RightStatusText = "导入失败";
                    MessageBox.Show($"导入目录时发生错误: {ex.Message}", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task CompareDirectoriesAsync()
        {
            if (LeftDirectoryBooks.Count == 0 || RightDirectoryBooks.Count == 0)
            {
                MessageBox.Show("请先导入左右两侧的目录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsComparing = true;
            ComparisonResults.Clear();

            try
            {
                // 创建比较结果分组
                var exactMatches = new ComparisonResultGroup("完全匹配", "书籍名称和作者完全相同", "#e3f2fd");
                var titleMatches = new ComparisonResultGroup("书名匹配", "仅书籍名称相同", "#e8f5e9");
                var authorMatches = new ComparisonResultGroup("作者匹配", "仅作者相同", "#fff8e1");
                var similarTitles = new ComparisonResultGroup("书名相似", "书籍名称相似但不完全相同", "#f3e5f5");
                var leftOnly = new ComparisonResultGroup("仅左侧", "仅在左侧目录中存在", "#ffebee");
                var rightOnly = new ComparisonResultGroup("仅右侧", "仅在右侧目录中存在", "#e0f7fa");

                // 用于跟踪已匹配的书籍
                var matchedLeftBooks = new HashSet<BookViewModel>();
                var matchedRightBooks = new HashSet<BookViewModel>();

                // 1. 查找完全匹配（书名和作者都相同）
                await Task.Run(() =>
                {
                    foreach (var leftBook in LeftDirectoryBooks)
                    {
                        foreach (var rightBook in RightDirectoryBooks)
                        {
                            if (string.Equals(leftBook.Title?.Trim(), rightBook.Title?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(leftBook.Author?.Trim(), rightBook.Author?.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                exactMatches.AddPair(leftBook, rightBook);
                                matchedLeftBooks.Add(leftBook);
                                matchedRightBooks.Add(rightBook);
                            }
                        }
                    }
                });

                // 2. 查找仅书名匹配的
                await Task.Run(() =>
                {
                    foreach (var leftBook in LeftDirectoryBooks.Except(matchedLeftBooks))
                    {
                        foreach (var rightBook in RightDirectoryBooks.Except(matchedRightBooks))
                        {
                            if (string.Equals(leftBook.Title?.Trim(), rightBook.Title?.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                titleMatches.AddPair(leftBook, rightBook);
                                matchedLeftBooks.Add(leftBook);
                                matchedRightBooks.Add(rightBook);
                            }
                        }
                    }
                });

                // 3. 查找仅作者匹配的
                await Task.Run(() =>
                {
                    foreach (var leftBook in LeftDirectoryBooks.Except(matchedLeftBooks))
                    {
                        foreach (var rightBook in RightDirectoryBooks.Except(matchedRightBooks))
                        {
                            if (!string.IsNullOrWhiteSpace(leftBook.Author) && 
                                !string.IsNullOrWhiteSpace(rightBook.Author) &&
                                string.Equals(leftBook.Author?.Trim(), rightBook.Author?.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                authorMatches.AddPair(leftBook, rightBook);
                                matchedLeftBooks.Add(leftBook);
                                matchedRightBooks.Add(rightBook);
                            }
                        }
                    }
                });

                // 4. 查找书名相似的（使用简单的相似度算法）
                await Task.Run(() =>
                {
                    foreach (var leftBook in LeftDirectoryBooks.Except(matchedLeftBooks))
                    {
                        foreach (var rightBook in RightDirectoryBooks.Except(matchedRightBooks))
                        {
                            if (!string.IsNullOrWhiteSpace(leftBook.Title) && 
                                !string.IsNullOrWhiteSpace(rightBook.Title))
                            {
                                double similarity = CalculateSimilarity(leftBook.Title, rightBook.Title);
                                if (similarity > 0.7) // 相似度阈值
                                {
                                    similarTitles.AddPair(leftBook, rightBook);
                                    matchedLeftBooks.Add(leftBook);
                                    matchedRightBooks.Add(rightBook);
                                }
                            }
                        }
                    }
                });

                // 5. 剩余的书籍（仅在一侧存在）
                foreach (var leftBook in LeftDirectoryBooks.Except(matchedLeftBooks).OrderBy(b => b.Title))
                {
                    leftOnly.AddLeft(leftBook);
                }

                foreach (var rightBook in RightDirectoryBooks.Except(matchedRightBooks).OrderBy(b => b.Title))
                {
                    rightOnly.AddRight(rightBook);
                }

                // 添加所有分组到结果集合
                ComparisonResults.Add(exactMatches);
                ComparisonResults.Add(titleMatches);
                ComparisonResults.Add(authorMatches);
                ComparisonResults.Add(similarTitles);
                ComparisonResults.Add(leftOnly);
                ComparisonResults.Add(rightOnly);

                // 更新统计数据
                ExactMatchCount = exactMatches.Count;
                TitleMatchCount = titleMatches.Count;
                AuthorMatchCount = authorMatches.Count;
                SimilarTitleCount = similarTitles.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"比较目录时发生错误: {ex.Message}", "比较错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsComparing = false;
            }
        }

        private void ClearComparison()
        {
            LeftDirectoryBooks.Clear();
            RightDirectoryBooks.Clear();
            ComparisonResults.Clear();
            LeftDirectoryPath = string.Empty;
            RightDirectoryPath = string.Empty;
            LeftStatusText = "未导入目录";
            RightStatusText = "未导入目录";
            ExactMatchCount = 0;
            TitleMatchCount = 0;
            AuthorMatchCount = 0;
            SimilarTitleCount = 0;
            LeftTotalCount = 0;
            RightTotalCount = 0;
        }

        // 计算两个字符串的相似度（Levenshtein距离的归一化版本）
        private double CalculateSimilarity(string s, string t)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t))
                return 0.0;

            s = s.ToLower();
            t = t.ToLower();

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // 初始化距离矩阵
            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            // 计算编辑距离
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            // 归一化距离为相似度（0-1之间）
            return 1.0 - ((double)d[n, m] / Math.Max(n, m));
        }
    }

    // 比较结果分组类
    public partial class ComparisonResultGroup : ObservableObject
    {
        public string Name { get; }
        public string Description { get; }
        public string BackgroundColor { get; }
        
        [ObservableProperty]
        private ObservableCollection<ComparisonResultItem> items = new ObservableCollection<ComparisonResultItem>();

        public int Count => Items.Count;

        public ComparisonResultGroup(string name, string description, string backgroundColor)
        {
            Name = name;
            Description = description;
            BackgroundColor = backgroundColor;
        }

        public void AddPair(BookViewModel leftBook, BookViewModel rightBook)
        {
            Items.Add(new ComparisonResultItem(leftBook, rightBook));
        }

        public void AddLeft(BookViewModel leftBook)
        {
            Items.Add(new ComparisonResultItem(leftBook, null));
        }

        public void AddRight(BookViewModel rightBook)
        {
            Items.Add(new ComparisonResultItem(null, rightBook));
        }
    }

    // 比较结果项类
    public class ComparisonResultItem : ObservableObject
    {
        public BookViewModel? LeftBook { get; }
        public BookViewModel? RightBook { get; }

        public ComparisonResultItem(BookViewModel? leftBook, BookViewModel? rightBook)
        {
            LeftBook = leftBook;
            RightBook = rightBook;
        }
    }
}