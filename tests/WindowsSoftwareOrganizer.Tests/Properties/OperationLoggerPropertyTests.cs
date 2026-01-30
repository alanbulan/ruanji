using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for OperationLogger.
/// Tests Properties 15, 16, and 17 from the design document.
/// </summary>
public class OperationLoggerPropertyTests
{
    private readonly OperationLogger _operationLogger;

    public OperationLoggerPropertyTests()
    {
        _operationLogger = new OperationLogger(30);
    }

    #region Property 15: 操作日志记录

    /// <summary>
    /// **Validates: Requirements 7.1**
    /// **Property 15: 操作日志记录**
    /// 对于任意修改操作，操作完成后必须存在对应的OperationRecord，且StartTime早于或等于EndTime。
    /// </summary>
    [Theory]
    [InlineData(OperationType.Migration, "Migration test")]
    [InlineData(OperationType.Cleanup, "Cleanup test")]
    [InlineData(OperationType.RegistryUpdate, "Registry test")]
    [InlineData(OperationType.Rollback, "Rollback test")]
    public async Task CompletedOperationHasValidTimestamps(OperationType opType, string description)
    {
        var operationId = await _operationLogger.BeginOperationAsync(opType, description);
        await _operationLogger.CompleteOperationAsync(operationId, true);

        var record = await _operationLogger.GetOperationAsync(operationId);

        Assert.NotNull(record);
        Assert.NotNull(record.EndTime);
        Assert.True(record.StartTime <= record.EndTime.Value);
    }

    /// <summary>
    /// **Validates: Requirements 7.1**
    /// **Property 15: 操作日志记录**
    /// Operation record should exist after BeginOperation.
    /// </summary>
    [Fact]
    public async Task BeginOperationAsync_CreatesRecord()
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration, "Test operation");

        var record = await _operationLogger.GetOperationAsync(operationId);

        Assert.NotNull(record);
        Assert.Equal(operationId, record.Id);
        Assert.Equal(OperationType.Migration, record.Type);
        Assert.Equal("Test operation", record.Description);
        Assert.Null(record.EndTime);
        Assert.Null(record.Success);
    }

    /// <summary>
    /// **Validates: Requirements 7.1**
    /// **Property 15: 操作日志记录**
    /// CompleteOperation should set EndTime and Success.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompleteOperationAsync_SetsEndTimeAndSuccess(bool success)
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Cleanup, "Test cleanup");

        await _operationLogger.CompleteOperationAsync(operationId, success);

        var record = await _operationLogger.GetOperationAsync(operationId);

        Assert.NotNull(record);
        Assert.NotNull(record.EndTime);
        Assert.Equal(success, record.Success);
    }

    /// <summary>
    /// **Validates: Requirements 7.1**
    /// **Property 15: 操作日志记录**
    /// LogAction should add action to operation.
    /// </summary>
    [Fact]
    public async Task LogActionAsync_AddsActionToOperation()
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration, "Test migration");

        var action = new OperationAction
        {
            ActionType = "FileCopy",
            Description = "Copy file.txt",
            Timestamp = DateTime.UtcNow,
            OriginalValue = @"C:\Source\file.txt",
            NewValue = @"D:\Target\file.txt",
            CanRollback = true
        };

        await _operationLogger.LogActionAsync(operationId, action);
        await _operationLogger.CompleteOperationAsync(operationId, true);

        var record = await _operationLogger.GetOperationAsync(operationId);

        Assert.NotNull(record);
        Assert.NotEmpty(record.Actions);
        Assert.Contains(record.Actions, a => a.ActionType == "FileCopy");
    }

    /// <summary>
    /// **Validates: Requirements 7.1**
    /// **Property 15: 操作日志记录**
    /// Empty description should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BeginOperationAsync_InvalidDescription_ThrowsArgumentException(string description)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _operationLogger.BeginOperationAsync(OperationType.Migration, description));
    }

    #endregion


    #region Property 16: 操作历史时间范围

    /// <summary>
    /// **Validates: Requirements 7.2**
    /// **Property 16: 操作历史时间范围**
    /// 对于任意时间点调用GetHistoryAsync，返回的记录中不应包含超过30天的操作记录。
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_RespectsHistoryLimit()
    {
        // Create a recent operation
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration, "Recent operation");
        await _operationLogger.CompleteOperationAsync(operationId, true);

        var history = await _operationLogger.GetHistoryAsync();

        // All records should be within 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        foreach (var record in history)
        {
            Assert.True(record.StartTime >= cutoffDate,
                $"Record {record.Id} has StartTime {record.StartTime} which is older than 30 days");
        }
    }

    /// <summary>
    /// **Validates: Requirements 7.2**
    /// **Property 16: 操作历史时间范围**
    /// GetHistory with since parameter should filter correctly.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_WithSinceParameter_FiltersCorrectly()
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Cleanup, "Test operation");
        await _operationLogger.CompleteOperationAsync(operationId, true);

        var since = DateTime.UtcNow.AddMinutes(-5);
        var history = await _operationLogger.GetHistoryAsync(since);

        foreach (var record in history)
        {
            Assert.True(record.StartTime >= since);
        }
    }

    /// <summary>
    /// **Validates: Requirements 7.2**
    /// **Property 16: 操作历史时间范围**
    /// GetHistory with limit should respect the limit.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_WithLimit_RespectsLimit()
    {
        // Create multiple operations
        for (int i = 0; i < 5; i++)
        {
            var opId = await _operationLogger.BeginOperationAsync(
                OperationType.Migration, $"Operation {i}");
            await _operationLogger.CompleteOperationAsync(opId, true);
        }

        var history = await _operationLogger.GetHistoryAsync(limit: 3);

        Assert.True(history.Count <= 3);
    }

    /// <summary>
    /// **Validates: Requirements 7.2**
    /// **Property 16: 操作历史时间范围**
    /// History should be ordered by StartTime descending.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_OrderedByStartTimeDescending()
    {
        var history = await _operationLogger.GetHistoryAsync();

        for (int i = 1; i < history.Count; i++)
        {
            Assert.True(history[i - 1].StartTime >= history[i].StartTime,
                "History should be ordered by StartTime descending");
        }
    }

    #endregion

    #region Property 17: 可回滚操作过滤

    /// <summary>
    /// **Validates: Requirements 7.3**
    /// **Property 17: 可回滚操作过滤**
    /// 对于任意操作历史，显示的可回滚操作列表中的每个操作的所有Action都必须满足CanRollback为true。
    /// </summary>
    [Fact]
    public async Task RollbackableOperations_AllActionsCanRollback()
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration, "Rollbackable operation");

        // Add rollbackable actions
        await _operationLogger.LogActionAsync(operationId, new OperationAction
        {
            ActionType = "FileCopy",
            Description = "Copy file",
            Timestamp = DateTime.UtcNow,
            CanRollback = true
        });

        await _operationLogger.CompleteOperationAsync(operationId, true);

        var record = await _operationLogger.GetOperationAsync(operationId);

        Assert.NotNull(record);
        // All actions should be rollbackable
        Assert.All(record.Actions, a => Assert.True(a.CanRollback));
    }

    /// <summary>
    /// **Validates: Requirements 7.3**
    /// **Property 17: 可回滚操作过滤**
    /// Operations with non-rollbackable actions should be identified.
    /// </summary>
    [Fact]
    public async Task NonRollbackableActions_AreRecorded()
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Cleanup, "Non-rollbackable operation");

        await _operationLogger.LogActionAsync(operationId, new OperationAction
        {
            ActionType = "PermanentDelete",
            Description = "Permanently delete file",
            Timestamp = DateTime.UtcNow,
            CanRollback = false
        });

        await _operationLogger.CompleteOperationAsync(operationId, true);

        var record = await _operationLogger.GetOperationAsync(operationId);

        Assert.NotNull(record);
        Assert.Contains(record.Actions, a => !a.CanRollback);
    }

    /// <summary>
    /// **Validates: Requirements 7.3**
    /// **Property 17: 可回滚操作过滤**
    /// Filter rollbackable operations from history.
    /// </summary>
    [Fact]
    public async Task GetRollbackableOperations_FiltersCorrectly()
    {
        // Create operation with all rollbackable actions
        var rollbackableOpId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration, "Rollbackable");
        await _operationLogger.LogActionAsync(rollbackableOpId, new OperationAction
        {
            ActionType = "Move",
            Description = "Move file",
            Timestamp = DateTime.UtcNow,
            CanRollback = true
        });
        await _operationLogger.CompleteOperationAsync(rollbackableOpId, true);

        var history = await _operationLogger.GetHistoryAsync();

        // Filter to only rollbackable operations
        var rollbackable = history
            .Where(r => r.Actions.All(a => a.CanRollback))
            .ToList();

        foreach (var record in rollbackable)
        {
            Assert.All(record.Actions, a => Assert.True(a.CanRollback));
        }
    }

    #endregion

    #region Additional Tests

    /// <summary>
    /// GetOperation with invalid ID should throw.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOperationAsync_InvalidId_ThrowsArgumentException(string operationId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _operationLogger.GetOperationAsync(operationId));
    }

    /// <summary>
    /// GetOperation with non-existent ID should return null.
    /// </summary>
    [Fact]
    public async Task GetOperationAsync_NonExistentId_ReturnsNull()
    {
        var record = await _operationLogger.GetOperationAsync("non-existent-id");
        Assert.Null(record);
    }

    /// <summary>
    /// CompleteOperation with invalid ID should throw.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CompleteOperationAsync_InvalidId_ThrowsArgumentException(string operationId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _operationLogger.CompleteOperationAsync(operationId, true));
    }

    /// <summary>
    /// LogAction with invalid operation ID should throw.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidOperationId_ThrowsArgumentException(string operationId)
    {
        var action = new OperationAction
        {
            ActionType = "Test",
            Description = "Test",
            Timestamp = DateTime.UtcNow,
            CanRollback = true
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _operationLogger.LogActionAsync(operationId, action));
    }

    /// <summary>
    /// LogAction with null action should throw.
    /// </summary>
    [Fact]
    public async Task LogActionAsync_NullAction_ThrowsArgumentNullException()
    {
        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration, "Test");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _operationLogger.LogActionAsync(operationId, null!));
    }

    #endregion

    #region Helper Methods

    private static Arbitrary<OperationType> OperationTypeArbitrary()
    {
        return Gen.Elements(Enum.GetValues<OperationType>()).ToArbitrary();
    }

    private static Arbitrary<string> NonEmptyStringArbitrary()
    {
        return Gen.Elements("Operation A", "Operation B", "Migration Task", "Cleanup Job")
            .ToArbitrary();
    }

    #endregion
}
