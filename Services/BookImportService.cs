using BookSteward.Models;
using BookSteward.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

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
        /// 解析书籍文件的元数据
        /// </summary>
        /// <param name="bookFiles">要解析的书籍文件路径列表</param>
        /// <returns>解析后的书籍对象列表</returns>
        public async Task<List<Book>> ParseBooksMetadataAsync(List<string> bookFiles)
        {
            if (bookFiles == null || !bookFiles.Any())
            {
                Log.Warning("没有提供书籍文件进行解析");
                return new List<Book>();
            }

            var books = new List<Book>();
            var validFiles = bookFiles.Where(IsSupportedBookFormat).ToList();

            Log.Information("开始解析{Count}个书籍文件的元数据", validFiles.Count);

            foreach (var filePath in validFiles)
            {
                try
                {
                    var book = await ExtractMetadataFromFileAsync(filePath);
                    books.Add(book);
                    Log.Information("成功解析书籍元数据: {Title}", book.Title);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "解析书籍元数据失败: {FilePath}", filePath);
                }
            }

            Log.Information("完成元数据解析，成功: {SuccessCount}/{TotalCount}", 
                books.Count, validFiles.Count);
            return books;
        }

        /// <summary>
        /// 从文件中提取书籍元数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>包含元数据的书籍对象</returns>
        private async Task<Book> ExtractMetadataFromFileAsync(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileInfo = new FileInfo(filePath);

            // 创建基本书籍对象
            var book = new Book
            {
                Title = fileName, // 默认使用文件名作为标题
                FilePath = filePath,
                FileExtensions = new List<string> { extension },
                ImportDate = DateTime.Now,
                IsNew = true,
                Tags = new List<Tag>()
            };

            // 根据文件格式提取元数据
            try
            {
                switch (extension)
                {
                    case ".pdf":
                        await ExtractPdfMetadataAsync(book, filePath);
                        break;
                    case ".epub":
                        await ExtractEpubMetadataAsync(book, filePath);
                        break;
                    case ".mobi":
                    case ".azw3":
                        await ExtractMobiMetadataAsync(book, filePath);
                        break;
                    default:
                        // 对于其他格式，尝试从文件名解析信息
                        ParseTitleAndAuthorFromFileName(book, fileName);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "提取{Extension}格式元数据时出错，使用基本信息", extension);
                // 出错时使用基本信息，不中断处理
            }

            // 添加格式标签
            book.Tags.Add(new Tag { Name = $"格式:{extension.TrimStart('.')}" });

            // 检查文件大小，添加到描述
            if (string.IsNullOrEmpty(book.Description))
            {
                book.Description = $"文件大小: {FormatFileSize(fileInfo.Length)}";
            }
            else
            {
                book.Description += $"\n文件大小: {FormatFileSize(fileInfo.Length)}";
            }

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

            return book;
        }

        /// <summary>
        /// 从PDF文件中提取元数据
        /// </summary>
        private async Task ExtractPdfMetadataAsync(Book book, string filePath)
        {
            // 这里应该使用PDF解析库，如iTextSharp或PdfSharp
            // 由于当前项目未引入PDF解析库，这里使用模拟实现
            await Task.Delay(100); // 模拟异步操作
            
            // 从文件名尝试解析作者和标题
            ParseTitleAndAuthorFromFileName(book, Path.GetFileNameWithoutExtension(filePath));
        }

        /// <summary>
        /// 从EPUB文件中提取元数据
        /// </summary>
        private async Task ExtractEpubMetadataAsync(Book book, string filePath)
        {
            // 这里应该使用EPUB解析库，如EpubSharp
            // 由于当前项目未引入EPUB解析库，这里使用模拟实现
            await Task.Delay(100); // 模拟异步操作
            
            // 从文件名尝试解析作者和标题
            ParseTitleAndAuthorFromFileName(book, Path.GetFileNameWithoutExtension(filePath));
        }

        /// <summary>
        /// 从MOBI/AZW3文件中提取元数据
        /// </summary>
        private async Task ExtractMobiMetadataAsync(Book book, string filePath)
        {
            // 这里应该使用MOBI解析库
            // 由于当前项目未引入MOBI解析库，这里使用模拟实现
            await Task.Delay(100); // 模拟异步操作
            
            // 从文件名尝试解析作者和标题
            ParseTitleAndAuthorFromFileName(book, Path.GetFileNameWithoutExtension(filePath));
        }

        /// <summary>
        /// 从文件名中解析标题和作者信息
        /// </summary>
        private void ParseTitleAndAuthorFromFileName(Book book, string fileName)
        {
            // 常见的文件命名模式："作者 - 书名" 或 "书名 - 作者"
            if (fileName.Contains("-"))
            {
                var parts = fileName.Split(new[] { '-' }, 2).Select(p => p.Trim()).ToArray();
                
                // 尝试判断哪部分是作者，哪部分是标题
                // 简单规则：如果第一部分较短，可能是作者名
                if (parts[0].Length < parts[1].Length && parts[0].Length < 20)
                {
                    book.Author = parts[0];
                    book.Title = parts[1];
                }
                else
                {
                    book.Title = parts[0];
                    book.Author = parts[1];
                }
            }
            else
            {
                // 如果没有分隔符，整个文件名作为标题
                book.Title = fileName;
            }
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n2} {suffixes[counter]}";
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