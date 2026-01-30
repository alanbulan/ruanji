using Microsoft.Extensions.DependencyInjection;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Windows Software Organizer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWindowsSoftwareOrganizerServices(this IServiceCollection services)
    {
        // Register configuration service first (singleton - shared configuration)
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register operation logger (singleton - maintains operation history)
        services.AddSingleton<IOperationLogger, OperationLogger>();

        // Register core engines (transient - stateless operations)
        services.AddTransient<ISoftwareScanner, SoftwareScanner>();
        services.AddTransient<ISoftwareClassifier, SoftwareClassifier>();
        services.AddTransient<INamingEngine, NamingEngine>();
        services.AddTransient<ILinkManager, LinkManager>();
        services.AddTransient<IRegistryUpdater, RegistryUpdater>();
        services.AddTransient<ICleanupEngine, CleanupEngine>();
        services.AddTransient<IMigrationEngine, MigrationEngine>();

        // Register application service (coordinates all operations)
        services.AddTransient<SoftwareOrganizerService>();

        // ===== 文件管理器服务 =====
        // 文件系统服务 (singleton - 无状态但频繁使用)
        services.AddSingleton<IFileSystemService, FileSystemService>();

        // OpenAI 客户端 (singleton - 维护 HTTP 客户端)
        services.AddSingleton<IOpenAIClient, OpenAIClient>();

        // 大小分析器 (transient - 无状态)
        services.AddTransient<ISizeAnalyzer, SizeAnalyzer>();

        // 类型统计服务 (transient - 无状态)
        services.AddTransient<ITypeStatisticsService, TypeStatisticsService>();

        // 文件搜索服务 (transient - 无状态)
        services.AddTransient<IFileSearchService, FileSearchService>();

        // 批量文件操作 (scoped - 维护撤销栈)
        services.AddScoped<IBatchFileOperator, BatchFileOperator>();

        // AI 文件分析器 (transient - 无状态)
        services.AddTransient<IAIFileAnalyzer, AIFileAnalyzer>();

        return services;
    }
}
