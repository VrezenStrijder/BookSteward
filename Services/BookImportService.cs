using BookSteward.Models;
using System.IO;

namespace BookSteward.Services
{
    /// <summary>
    /// 提供书籍导入相关功能的服务
    /// </summary>
    public class BookImportService
    {
        private readonly IBookService bookService;
        
        // 支持的文件格式
        private readonly HashSet<string> supportedFormats = new HashSet<string>(
            new[] { ".pdf", ".mobi", ".epub", ".txt", ".azw3" },
            StringComparer.OrdinalIgnoreCase
        );

        public BookImportService(IBookService bookService)
        {
            this.bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        }

        /// <summary>
        /// 检查文件是否为支持的书籍格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>如果文件格式受支持则返回true</returns>
        public bool IsSupportedBookFormat(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            
            string extension = Path.GetExtension(filePath);
            return supportedFormats.Contains(extension);
        }

        /// <summary>
        /// 导入书籍文件
        /// </summary>
        /// <param name="filePaths">要导入的文件路径列表</param>
        /// <returns>成功导入的书籍列表</returns>
        public async Task<IEnumerable<Book>> ImportBooksAsync(IEnumerable<string> filePaths)
        {
            var importedBooks = new List<Book>();
            var validFiles = filePaths.Where(IsSupportedBookFormat).ToList();

            Log.Information("开始导入{Count}个书籍文件", validFiles.Count);
            
            // 获取所有现有书籍的文件路径，用于检查重复
            var existingFilePaths = (await bookService.GetAllBooksAsync())
                .Select(b => b.FilePath.ToLowerInvariant())
                .ToHashSet();
                
            Log.Debug("当前数据库中已有{Count}本书籍", existingFilePaths.Count);

            foreach (var filePath in validFiles)
            {
                try
                {
                    // 检查是否已存在相同路径的书籍（不区分大小写）
                    if (existingFilePaths.Contains(filePath.ToLowerInvariant()))
                    {
                        Log.Information("跳过已存在的书籍: {FilePath}", filePath);
                        continue;
                    }
                    
                    var book = await CreateBookFromFileAsync(filePath);
                    var savedBook = await bookService.AddBookAsync(book);
                    importedBooks.Add(savedBook);
                    Log.Information("成功导入书籍: {Title}", book.Title);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "导入书籍失败: {FilePath}", filePath);
                }
            }

            Log.Information("完成导入，成功: {SuccessCount}/{TotalCount}，跳过重复: {SkippedCount}", 
                importedBooks.Count, validFiles.Count, validFiles.Count - importedBooks.Count);
            return importedBooks;
        }

        /// <summary>
        /// 从文件创建书籍对象
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>创建的书籍对象</returns>
        private async Task<Book> CreateBookFromFileAsync(string filePath)
        {
            // 这里可以添加从文件中提取元数据的逻辑
            // 例如使用第三方库解析PDF、EPUB等格式的元数据
            // 目前简单地从文件名提取信息

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            var book = new Book
            {
                Title = fileName, // 使用文件名作为标题
                FilePath = filePath,
                FileExtensions = new List<string> { extension },
                ImportDate = DateTime.Now,
                IsNew = true,
                Tags = new List<Tag>() // 初始化标签列表
            };

            // 根据扩展名添加类型标签
            var formatTag = new Tag { Name = $"格式:{extension.TrimStart('.')}" };
            book.Tags.Add(formatTag);

            // 检查是否缺失关键信息，并标记
            bool isMissingInfo = string.IsNullOrWhiteSpace(book.Title) || 
                                string.IsNullOrWhiteSpace(book.Author) || 
                                string.IsNullOrWhiteSpace(book.Publisher);
            
            book.IsInfoIncomplete = isMissingInfo;
            
            // 如果缺失信息，添加对应标签
            if (isMissingInfo)
            {
                book.Tags.Add(new Tag { Name = "信息待完善" });
            }

            // 这里可以添加异步元数据提取逻辑
            await Task.CompletedTask;

            return book;
        }
    }
}