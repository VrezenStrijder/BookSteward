using BookSteward.Data;
using BookSteward.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static System.Formats.Asn1.AsnWriter;

namespace BookSteward.Services
{
    /// <summary>
    /// 提供数据库迁移和架构更新的服务
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly BookStewardDbContext dbContext;
        private readonly string dbPath;
        private readonly string backupPath;

        public DatabaseMigrationService(BookStewardDbContext dbContext)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            dbPath = this.dbContext.DbPath;
            backupPath = $"{dbPath}.backup";
        }

        /// <summary>
        /// 检查并更新数据库架构
        /// </summary>
        /// <returns>更新是否成功</returns>
        public async Task<bool> MigrateAsync()
        {
            try
            {
                Log.Information("开始检查数据库架构");

                // 检查数据库文件是否存在
                bool dbExists = File.Exists(dbPath);

                if (dbExists)
                {
                    // 创建数据库备份
                    await CreateBackupAsync();
                }

                // 验证Categories表是否存在
                bool categoriesTableExists = await TableExistsAsync("Categories");
                if (!categoriesTableExists)
                {
                    Log.Warning("Categories表不存在，将尝试创建");

                    // 确保数据库结构正确
                    await EnsureCategoriesTableAsync();
                }

                Log.Information("数据库架构更新完成");

                bool flag = await ExecuteOnMigrateFinished();
                return flag;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库迁移失败");

                // 如果有备份，尝试恢复
                if (File.Exists(backupPath))
                {
                    Log.Information("尝试从备份恢复数据库");
                    await RestoreFromBackupAsync();
                }

                return false;
            }
        }

        /// <summary>
        /// 检查表是否存在
        /// </summary>
        private async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                // 使用SQLite的pragma语句检查表是否存在
                var connection = dbContext.Database.GetDbConnection() as SqliteConnection;
                if (connection == null)
                {
                    Log.Error("无法获取SQLite连接");
                    return false;
                }

                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";

                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"检查表{tableName}是否存在时出错");
                return false;
            }
        }

        /// <summary>
        /// 确保Categories表存在
        /// </summary>
        private async Task EnsureCategoriesTableAsync()
        {
            try
            {
                var connection = dbContext.Database.GetDbConnection() as SqliteConnection;
                if (connection == null)
                {
                    Log.Error("无法获取SQLite连接");
                    throw new InvalidOperationException("无法获取SQLite连接");
                }

                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                // 创建Categories表
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        ParentId INTEGER NULL,
                        FOREIGN KEY (ParentId) REFERENCES Categories(Id) ON DELETE RESTRICT
                    )";

                    await command.ExecuteNonQueryAsync();
                }

                // 创建索引
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE INDEX IF NOT EXISTS IX_Categories_ParentId ON Categories(ParentId)";
                    await command.ExecuteNonQueryAsync();
                }

                // 检查是否存在默认分类
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Categories WHERE Name = '默认分类'";
                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        // 创建默认分类
                        using (var insertCommand = connection.CreateCommand())
                        {
                            insertCommand.CommandText = "INSERT INTO Categories (Name, ParentId) VALUES ('默认分类', NULL)";
                            await insertCommand.ExecuteNonQueryAsync();
                            Log.Information("已创建默认分类");
                        }
                    }
                }

                // 创建BookCategory关联表
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS BookCategory (
                        BooksId INTEGER NOT NULL,
                        CategoryId INTEGER NOT NULL,
                        PRIMARY KEY (BooksId, CategoryId),
                        FOREIGN KEY (BooksId) REFERENCES Books(Id) ON DELETE CASCADE,
                        FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
                    )";

                    await command.ExecuteNonQueryAsync();
                }

                // 创建BookCategory索引
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE INDEX IF NOT EXISTS IX_BookCategory_CategoryId ON BookCategory(CategoryId)";
                    await command.ExecuteNonQueryAsync();
                }

                Log.Information("Categories表和相关表创建成功");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建Categories表失败");
                throw;
            }
        }

        /// <summary>
        /// 创建数据库备份
        /// </summary>
        private async Task CreateBackupAsync()
        {
            try
            {
                Log.Information("创建数据库备份: {BackupPath}", backupPath);

                // 确保数据库连接已关闭
                await dbContext.Database.CloseConnectionAsync();

                // 复制数据库文件
                File.Copy(dbPath, backupPath, overwrite: true);

                Log.Information("数据库备份创建成功");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建数据库备份失败");
                throw;
            }
        }

        /// <summary>
        /// 从备份恢复数据库
        /// </summary>
        private async Task RestoreFromBackupAsync()
        {
            try
            {
                Log.Information("从备份恢复数据库");

                // 确保数据库连接已关闭
                await dbContext.Database.CloseConnectionAsync();

                // 删除损坏的数据库文件
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                // 复制备份文件
                File.Copy(backupPath, dbPath, overwrite: true);

                Log.Information("数据库恢复成功");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "从备份恢复数据库失败");
                throw;
            }
        }

        /// <summary>
        /// 执行自定义SQL迁移脚本
        /// </summary>
        /// <param name="sqlScript">SQL脚本内容</param>
        public async Task ExecuteCustomMigrationAsync(string sqlScript)
        {
            try
            {
                Log.Information("执行自定义SQL迁移脚本");

                // 创建备份
                await CreateBackupAsync();

                // 执行SQL脚本
                await dbContext.Database.ExecuteSqlRawAsync(sqlScript);

                Log.Information("自定义SQL迁移脚本执行成功");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "执行自定义SQL迁移脚本失败");

                // 尝试恢复
                await RestoreFromBackupAsync();
                throw;
            }
        }

        /// <summary>
        /// 执行迁移后的验证及初始化操作
        /// </summary>
        public async Task<bool> ExecuteOnMigrateFinished()
        {
            //成功则执行验证及其他初始化操作
            try
            {
                var categoriesCount = await dbContext.Categories.CountAsync();
                Log.Information("Categories表验证成功，当前有{Count}条记录", categoriesCount);

                // 初始化默认分类
                if (categoriesCount == 0)
                {
                    var defaultCategory = await dbContext.Categories
                        .Include(c => c.Books)
                        .FirstOrDefaultAsync(c => c.Name == "默认分类");

                    if (defaultCategory == null)
                    {
                        defaultCategory = new Category
                        {
                            Name = "默认分类",
                            ParentId = null
                        };
                        dbContext.Categories.Add(defaultCategory);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Categories表验证失败");
                return false;
            }

            Log.Information("已完成迁移后的验证及初始化工作.");

            return true;
        }

    }
}