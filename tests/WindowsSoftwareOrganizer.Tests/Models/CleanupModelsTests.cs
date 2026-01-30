namespace WindowsSoftwareOrganizer.Tests.Models;

using WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Unit tests for cleanup-related models.
/// Validates: Requirements 6.3
/// </summary>
public class CleanupModelsTests
{
    [Fact]
    public void CleanupItem_RequiredProperties_ShouldBeSet()
    {
        // Arrange & Act
        var item = new CleanupItem
        {
            Id = "cleanup-1",
            Path = @"C:\Users\Test\AppData\Local\OldSoftware",
            Type = CleanupItemType.OrphanedDirectory,
            Risk = RiskLevel.Safe
        };

        // Assert
        Assert.Equal("cleanup-1", item.Id);
        Assert.Equal(@"C:\Users\Test\AppData\Local\OldSoftware", item.Path);
        Assert.Equal(CleanupItemType.OrphanedDirectory, item.Type);
        Assert.Equal(RiskLevel.Safe, item.Risk);
    }

    [Fact]
    public void CleanupItem_OptionalProperties_ShouldHaveDefaults()
    {
        // Arrange & Act
        var item = new CleanupItem
        {
            Id = "cleanup-1",
            Path = @"C:\Test",
            Type = CleanupItemType.CacheDirectory,
            Risk = RiskLevel.Safe
        };

        // Assert
        Assert.Equal(0, item.SizeBytes);
        Assert.Null(item.Description);
        Assert.Null(item.RelatedSoftware);
    }

    [Fact]
    public void CleanupItem_AllProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var item = new CleanupItem
        {
            Id = "cleanup-1",
            Path = @"C:\Users\Test\AppData\Local\OldSoftware\Cache",
            Type = CleanupItemType.CacheDirectory,
            Risk = RiskLevel.Caution,
            SizeBytes = 1024000,
            Description = "Cache files from old software",
            RelatedSoftware = "Old Software v1.0"
        };

        // Assert
        Assert.Equal(1024000, item.SizeBytes);
        Assert.Equal("Cache files from old software", item.Description);
        Assert.Equal("Old Software v1.0", item.RelatedSoftware);
    }

    [Theory]
    [InlineData(CleanupItemType.OrphanedDirectory)]
    [InlineData(CleanupItemType.OrphanedRegistryKey)]
    [InlineData(CleanupItemType.CacheDirectory)]
    [InlineData(CleanupItemType.TempFile)]
    [InlineData(CleanupItemType.LogFile)]
    public void CleanupItemType_AllValues_ShouldBeValid(CleanupItemType type)
    {
        // Arrange & Act
        var item = new CleanupItem
        {
            Id = "test",
            Path = @"C:\Test",
            Type = type,
            Risk = RiskLevel.Safe
        };

        // Assert
        Assert.Equal(type, item.Type);
    }

    [Theory]
    [InlineData(RiskLevel.Safe)]
    [InlineData(RiskLevel.Caution)]
    [InlineData(RiskLevel.Dangerous)]
    public void RiskLevel_AllValues_ShouldBeValid(RiskLevel risk)
    {
        // Arrange & Act
        var item = new CleanupItem
        {
            Id = "test",
            Path = @"C:\Test",
            Type = CleanupItemType.CacheDirectory,
            Risk = risk
        };

        // Assert
        Assert.Equal(risk, item.Risk);
    }

    [Fact]
    public void CleanupResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new CleanupResult { Success = true };

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(0, result.BytesFreed);
        Assert.Equal(0, result.ItemsCleaned);
        Assert.Equal(0, result.ItemsFailed);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void CleanupResult_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var failures = new List<CleanupFailure>
        {
            new CleanupFailure { Path = @"C:\Locked\file.txt", ErrorMessage = "File is locked" }
        };

        // Act
        var result = new CleanupResult
        {
            Success = false,
            ErrorMessage = "Some items failed to clean",
            BytesFreed = 1024000,
            ItemsCleaned = 5,
            ItemsFailed = 1,
            Failures = failures
        };

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Some items failed to clean", result.ErrorMessage);
        Assert.Equal(1024000, result.BytesFreed);
        Assert.Equal(5, result.ItemsCleaned);
        Assert.Equal(1, result.ItemsFailed);
        Assert.Single(result.Failures);
    }

    [Fact]
    public void CleanupFailure_RequiredProperties_ShouldBeSet()
    {
        // Arrange & Act
        var failure = new CleanupFailure
        {
            Path = @"C:\Test\file.txt",
            ErrorMessage = "Access denied"
        };

        // Assert
        Assert.Equal(@"C:\Test\file.txt", failure.Path);
        Assert.Equal("Access denied", failure.ErrorMessage);
    }

    [Fact]
    public void CleanupItem_RecordEquality_ShouldWork()
    {
        // Arrange
        var item1 = new CleanupItem
        {
            Id = "test",
            Path = @"C:\Test",
            Type = CleanupItemType.CacheDirectory,
            Risk = RiskLevel.Safe
        };

        var item2 = new CleanupItem
        {
            Id = "test",
            Path = @"C:\Test",
            Type = CleanupItemType.CacheDirectory,
            Risk = RiskLevel.Safe
        };

        // Assert
        Assert.Equal(item1, item2);
    }
}
