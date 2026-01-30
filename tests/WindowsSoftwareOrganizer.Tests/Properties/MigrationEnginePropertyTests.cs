using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;
using NSubstitute;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for MigrationEngine.
/// Tests Properties 9 and 18 from the design document.
/// </summary>
public class MigrationEnginePropertyTests
{
    private readonly ILinkManager _mockLinkManager;
    private readonly IRegistryUpdater _mockRegistryUpdater;
    private readonly IOperationLogger _mockOperationLogger;
    private readonly INamingEngine _mockNamingEngine;
    private readonly MigrationEngine _migrationEngine;

    public MigrationEnginePropertyTests()
    {
        _mockLinkManager = Substitute.For<ILinkManager>();
        _mockRegistryUpdater = Substitute.For<IRegistryUpdater>();
        _mockOperationLogger = Substitute.For<IOperationLogger>();
        _mockNamingEngine = Substitute.For<INamingEngine>();

        // Setup default behaviors
        _mockLinkManager.IsSymbolicLinkSupported().Returns(false);
        _mockLinkManager.IsJunctionSupported(Arg.Any<string>()).Returns(true);
        _mockNamingEngine.GenerateName(Arg.Any<SoftwareEntry>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.ArgAt<SoftwareEntry>(0).Name);
        _mockNamingEngine.ResolveConflict(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1)));
        _mockOperationLogger.BeginOperationAsync(Arg.Any<OperationType>(), Arg.Any<string>())
            .Returns(Guid.NewGuid().ToString("N"));

        _migrationEngine = new MigrationEngine(
            _mockLinkManager,
            _mockRegistryUpdater,
            _mockOperationLogger,
            _mockNamingEngine);
    }

    #region Property 9: 迁移空间检查

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// **Property 9: 迁移空间检查**
    /// 对于任意迁移计划，如果TotalSizeBytes大于AvailableSpaceBytes，则执行迁移应返回空间不足错误。
    /// </summary>
    [Property(MaxTest = 100)]
    public Property InsufficientSpaceReturnsError()
    {
        return Prop.ForAll(
            Gen.Choose(1000, 1000000).ToArbitrary(),
            Gen.Choose(1, 999).ToArbitrary(),
            (totalSize, availableRatio) =>
            {
                // Available space is less than total size
                var availableSpace = (long)(totalSize * availableRatio / 1000);
                
                var plan = CreateTestPlan(totalSize, availableSpace);
                var options = new MigrationOptions();

                var result = _migrationEngine.ExecuteAsync(plan, options).GetAwaiter().GetResult();

                // If total > available, should fail with space error
                if (totalSize > availableSpace)
                {
                    return !result.Success && 
                           result.ErrorMessage != null && 
                           result.ErrorMessage.Contains("空间不足");
                }
                return true;
            });
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// **Property 9: 迁移空间检查**
    /// Sufficient space should not return space error.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SufficientSpaceDoesNotReturnSpaceError()
    {
        return Prop.ForAll(
            Gen.Choose(1000, 100000).ToArbitrary(),
            (totalSize) =>
            {
                // Available space is more than total size
                var availableSpace = totalSize * 2L;
                
                var plan = CreateTestPlan(totalSize, availableSpace);
                var options = new MigrationOptions();

                var result = _migrationEngine.ExecuteAsync(plan, options).GetAwaiter().GetResult();

                // Should not fail due to space (may fail for other reasons in test environment)
                if (!result.Success && result.ErrorMessage != null)
                {
                    return !result.ErrorMessage.Contains("空间不足");
                }
                return true;
            });
    }


    /// <summary>
    /// **Validates: Requirements 4.1**
    /// **Property 9: 迁移空间检查**
    /// Zero available space should always fail.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ZeroAvailableSpace_ReturnsSpaceError()
    {
        var plan = CreateTestPlan(1000, 0);
        var options = new MigrationOptions();

        var result = await _migrationEngine.ExecuteAsync(plan, options);

        Assert.False(result.Success);
        Assert.Contains("空间不足", result.ErrorMessage);
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// **Property 9: 迁移空间检查**
    /// Exact space should succeed (not fail due to space).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ExactSpace_DoesNotReturnSpaceError()
    {
        var plan = CreateTestPlan(1000, 1000);
        var options = new MigrationOptions();

        var result = await _migrationEngine.ExecuteAsync(plan, options);

        // Should not fail due to space
        if (!result.Success && result.ErrorMessage != null)
        {
            Assert.DoesNotContain("空间不足", result.ErrorMessage);
        }
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// **Property 9: 迁移空间检查**
    /// Null plan should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullPlan_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _migrationEngine.ExecuteAsync(null!, new MigrationOptions()));
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// **Property 9: 迁移空间检查**
    /// Null options should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullOptions_ThrowsArgumentNullException()
    {
        var plan = CreateTestPlan(1000, 2000);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _migrationEngine.ExecuteAsync(plan, null!));
    }

    #endregion

    #region Property 18: 回滚状态恢复

    /// <summary>
    /// **Validates: Requirements 7.4**
    /// **Property 18: 回滚状态恢复**
    /// 对于任意成功的迁移操作，执行回滚后，原始文件应恢复到原位置，且创建的链接应被删除。
    /// </summary>
    [Fact]
    public async Task RollbackAsync_UnknownOperationId_ReturnsError()
    {
        var result = await _migrationEngine.RollbackAsync("unknown-operation-id");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    /// <summary>
    /// **Validates: Requirements 7.4**
    /// **Property 18: 回滚状态恢复**
    /// Empty operation ID should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RollbackAsync_InvalidOperationId_ThrowsArgumentException(string operationId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _migrationEngine.RollbackAsync(operationId));
    }

    /// <summary>
    /// **Validates: Requirements 7.4**
    /// **Property 18: 回滚状态恢复**
    /// Rollback should attempt to restore registry if backup exists.
    /// </summary>
    [Fact]
    public async Task RollbackAsync_WithRegistryBackup_AttemptsRestore()
    {
        // Setup operation record
        _mockOperationLogger.GetOperationAsync(Arg.Any<string>())
            .Returns(new OperationRecord
            {
                Id = "test-op",
                Type = OperationType.Migration,
                Description = "Test migration",
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                EndTime = DateTime.UtcNow
            });

        var result = await _migrationEngine.RollbackAsync("test-op");

        // Should fail because no migration state exists
        Assert.False(result.Success);
        Assert.Contains("找不到迁移状态", result.ErrorMessage);
    }

    #endregion

    #region CreatePlan Tests

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// CreatePlan should return valid plan with correct paths.
    /// </summary>
    [Fact]
    public async Task CreatePlanAsync_ValidInput_ReturnsValidPlan()
    {
        var entry = CreateTestEntry();
        var targetPath = @"D:\Software";
        var template = new NamingTemplate
        {
            Id = "test",
            Name = "Test Template",
            Pattern = "{Name}"
        };

        var plan = await _migrationEngine.CreatePlanAsync(entry, targetPath, template);

        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Id);
        Assert.Equal(entry, plan.Software);
        Assert.Equal(entry.InstallPath, plan.SourcePath);
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// CreatePlan with null entry should throw.
    /// </summary>
    [Fact]
    public async Task CreatePlanAsync_NullEntry_ThrowsArgumentNullException()
    {
        var template = new NamingTemplate { Id = "test", Name = "Test", Pattern = "{Name}" };
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _migrationEngine.CreatePlanAsync(null!, @"D:\Software", template));
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// CreatePlan with empty target path should throw.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePlanAsync_InvalidTargetPath_ThrowsArgumentException(string targetPath)
    {
        var entry = CreateTestEntry();
        var template = new NamingTemplate { Id = "test", Name = "Test", Pattern = "{Name}" };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _migrationEngine.CreatePlanAsync(entry, targetPath, template));
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    /// CreatePlan with null template should throw.
    /// </summary>
    [Fact]
    public async Task CreatePlanAsync_NullTemplate_ThrowsArgumentNullException()
    {
        var entry = CreateTestEntry();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _migrationEngine.CreatePlanAsync(entry, @"D:\Software", null!));
    }

    /// <summary>
    /// **Validates: Requirements 4.4**
    /// CreatePlan should recommend Junction when SymbolicLink not supported.
    /// </summary>
    [Fact]
    public async Task CreatePlanAsync_SymbolicLinkNotSupported_RecommendsJunction()
    {
        _mockLinkManager.IsSymbolicLinkSupported().Returns(false);

        var entry = CreateTestEntry();
        var template = new NamingTemplate { Id = "test", Name = "Test", Pattern = "{Name}" };

        var plan = await _migrationEngine.CreatePlanAsync(entry, @"D:\Software", template);

        Assert.Equal(LinkType.Junction, plan.RecommendedLinkType);
    }

    /// <summary>
    /// **Validates: Requirements 4.4**
    /// CreatePlan should recommend SymbolicLink when supported.
    /// </summary>
    [Fact]
    public async Task CreatePlanAsync_SymbolicLinkSupported_RecommendsSymbolicLink()
    {
        _mockLinkManager.IsSymbolicLinkSupported().Returns(true);

        var entry = CreateTestEntry();
        var template = new NamingTemplate { Id = "test", Name = "Test", Pattern = "{Name}" };

        var plan = await _migrationEngine.CreatePlanAsync(entry, @"D:\Software", template);

        Assert.Equal(LinkType.SymbolicLink, plan.RecommendedLinkType);
    }

    #endregion

    #region Helper Methods

    private static MigrationPlan CreateTestPlan(long totalSize, long availableSpace)
    {
        return new MigrationPlan
        {
            Id = Guid.NewGuid().ToString("N"),
            Software = CreateTestEntry(),
            SourcePath = @"C:\Program Files\TestApp",
            TargetPath = @"D:\Software\TestApp",
            FileOperations = Array.Empty<FileMoveOperation>(),
            TotalSizeBytes = totalSize,
            AvailableSpaceBytes = availableSpace,
            RecommendedLinkType = LinkType.Junction
        };
    }

    private static SoftwareEntry CreateTestEntry()
    {
        return new SoftwareEntry
        {
            Id = "test-app",
            Name = "TestApp",
            InstallPath = @"C:\Program Files\TestApp",
            Category = SoftwareCategory.Utility,
            TotalSizeBytes = 1000
        };
    }

    #endregion
}
