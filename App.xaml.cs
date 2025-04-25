using BookSteward.Data;
using BookSteward.Services;
using BookSteward.ViewModels;
using BookSteward.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Windows;
using System.IO;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace BookSteward;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? host;
    
    /// <summary>
    /// 提供对应用程序服务的访问
    /// </summary>
    public IServiceProvider? ServiceProvider => host?.Services;

    public App()
    {
        LogConfig.Init();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();

        // 数据库初始化
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BookStewardDbContext>();
            try
            {
                // 检查数据库文件是否存在
                bool dbExists = File.Exists(dbContext.DbPath);
                Log.Information("数据库文件{Exists}", dbExists ? "已存在" : "不存在");

                //首次创建数据库
                if (!dbExists)
                {
                    Log.Information("创建新数据库");
                    await dbContext.Database.EnsureCreatedAsync();
                    Log.Information("数据库创建成功");
                }
                //数据库存在，则执行迁移
                else
                {
                    Log.Information("使用自定义迁移服务更新现有数据库");

                    var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
                    var success = await migrationService.MigrateAsync();

                    if (!success)
                    {
                        MessageBox.Show("数据库迁移失败", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown(-1);
                        return;
                    }
                }
                
                // 验证Categories表是否存在并可访问
                try
                {
                    var categoriesCount = await dbContext.Categories.CountAsync();
                    Log.Information("Categories表验证成功，当前有{Count}条记录", categoriesCount);
                    
                    // 如果没有默认分类，创建一个
                    if (categoriesCount == 0)
                    {
                        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                        await categoryService.GetOrCreateDefaultCategoryAsync();
                        Log.Information("已创建默认分类");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Categories表验证失败");
                    throw;
                }
                
                Log.Information("数据库 {DbPath} 初始化成功", dbContext.DbPath);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "数据库初始化失败.");
                MessageBox.Show($"数据库初始化失败: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }
        }

        // await _host.StartAsync();

        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        Log.Information("程序启动.");
    }

    private void ConfigureServices(IServiceCollection services)
    {

        services.AddDbContext<BookStewardDbContext>(options =>
        {
            
        });

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<BookViewModel>();
        services.AddSingleton<CategoryViewModel>();

        services.AddSingleton<MainWindow>();

        services.AddTransient<IBookService, BookService>();
        services.AddTransient<ICategoryService, CategoryService>();
        services.AddTransient<BookImportService>();
        services.AddTransient<DatabaseMigrationService>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("程序关闭.");

        if (host != null)
        {
            using (host)
            {
                await host.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

