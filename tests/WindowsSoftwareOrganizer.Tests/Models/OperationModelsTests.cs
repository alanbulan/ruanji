namespace WindowsSoftwareOrganizer.Tests.Models;

using WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Unit tests for operation-related models.
/// Validates: Requirements 7.1
/// </summary>
public class OperationModelsTests
{
    [Fact]
    public void OperationRecord_RequiredProperties_ShouldBeSet()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var record = new OperationRecord
        {
            Id = "op-1",
            Type = OperationType.Migration,
            Description = "Migrating Test Software to D:\\Software",
            StartTime = startTime
        };

        // Assert
        Assert.Equal("op-1", record.Id);
        Assert.Equal(OperationType.Migration, record.Type);
        Assert.Equal("Migrating Test Software to D:\\Software", record.Description);
        Assert.Equal(startTime, record.StartTime);
    }

    [Fact]
    public void OperationRecord_OptionalProperties_ShouldHaveDefaults()
    {
        // Arrange & Act
        var record = new OperationRecord
        {
            Id = "op-1",
            Type = OperationType.Cleanup,
            Description = "Cleanup operation",
            StartTime = DateTime.UtcNow
        };

        // Assert
        Assert.Null(record.EndTime);
        Assert.Null(record.Success);
        Assert.Empty(record.Actions);
    }

    [Fact]
    public void OperationRecord_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);
        var actions = new List<OperationAction>
        {
            new OperationAction
            {
                ActionType = "FileCopy",
                Description = "Copied app.exe",
                Timestamp = startTime.AddSeconds(10),
                OriginalValue = @"C:\Source\app.exe",
                NewValue = @"D:\Target\app.exe",
                CanRollback = true
            }
        };

        // Act
        var record = new OperationRecord
        {
            Id = "op-1",
            Type = OperationType.Migration,
            Description = "Migration operation",
            StartTime = startTime,
            EndTime = endTime,
            Success = true,
            Actions = actions
        };

        // Assert
        Assert.Equal(endTime, record.EndTime);
        Assert.True(record.Success);
        Assert.Single(record.Actions);
    }

    [Theory]
    [InlineData(OperationType.Migration)]
    [InlineData(OperationType.Cleanup)]
    [InlineData(OperationType.RegistryUpdate)]
    [InlineData(OperationType.Rollback)]
    public void OperationType_AllValues_ShouldBeValid(OperationType type)
    {
        // Arrange & Act
        var record = new OperationRecord
        {
            Id = "test",
            Type = type,
            Description = "Test operation",
            StartTime = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(type, record.Type);
    }

    [Fact]
    public void OperationAction_RequiredProperties_ShouldBeSet()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var action = new OperationAction
        {
            ActionType = "FileCopy",
            Description = "Copied file.txt",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("FileCopy", action.ActionType);
        Assert.Equal("Copied file.txt", action.Description);
        Assert.Equal(timestamp, action.Timestamp);
    }

    [Fact]
    public void OperationAction_OptionalProperties_ShouldHaveDefaults()
    {
        // Arrange & Act
        var action = new OperationAction
        {
            ActionType = "Test",
            Description = "Test action",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Null(action.OriginalValue);
        Assert.Null(action.NewValue);
        Assert.False(action.CanRollback);
    }

    [Fact]
    public void OperationAction_AllProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var action = new OperationAction
        {
            ActionType = "RegistryUpdate",
            Description = "Updated registry path",
            Timestamp = DateTime.UtcNow,
            OriginalValue = @"C:\OldPath",
            NewValue = @"D:\NewPath",
            CanRollback = true
        };

        // Assert
        Assert.Equal(@"C:\OldPath", action.OriginalValue);
        Assert.Equal(@"D:\NewPath", action.NewValue);
        Assert.True(action.CanRollback);
    }

    [Fact]
    public void OperationRecord_RecordEquality_ShouldWork()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var record1 = new OperationRecord
        {
            Id = "op-1",
            Type = OperationType.Migration,
            Description = "Test",
            StartTime = startTime
        };

        var record2 = new OperationRecord
        {
            Id = "op-1",
            Type = OperationType.Migration,
            Description = "Test",
            StartTime = startTime
        };

        // Assert
        Assert.Equal(record1, record2);
    }

    [Fact]
    public void OperationAction_RecordEquality_ShouldWork()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var action1 = new OperationAction
        {
            ActionType = "Test",
            Description = "Test action",
            Timestamp = timestamp
        };

        var action2 = new OperationAction
        {
            ActionType = "Test",
            Description = "Test action",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(action1, action2);
    }
}
