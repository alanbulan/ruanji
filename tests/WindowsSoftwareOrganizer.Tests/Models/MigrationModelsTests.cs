namespace WindowsSoftwareOrganizer.Tests.Models;

using WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Unit tests for migration-related models.
/// Validates: Requirements 4.1
/// </summary>
public class MigrationModelsTests
{
    [Fact]
    public void MigrationPlan_RequiredProperties_ShouldBeSet()
    {
        // Arrange
        var software = new SoftwareEntry
        {
            Id = "test-id",
            Name = "Test Software",
            InstallPath = @"C:\Program Files\Test"
        };

        var fileOps = new List<FileMoveOperation>
        {
            new FileMoveOperation
            {
                SourcePath = @"C:\Program Files\Test\app.exe",
                TargetPath = @"D:\Software\Test\app.exe",
                SizeBytes = 1024
            }
        };

        // Act
        var plan = new MigrationPlan
        {
            Id = "plan-1",
            Software = software,
            SourcePath = @"C:\Program Files\Test",
            TargetPath = @"D:\Software\Test",
            FileOperations = fileOps,
            TotalSizeBytes = 1024,
            AvailableSpaceBytes = 1000000
        };

        // Assert
        Assert.Equal("plan-1", plan.Id);
        Assert.Equal(software, plan.Software);
        Assert.Equal(@"C:\Program Files\Test", plan.SourcePath);
        Assert.Equal(@"D:\Software\Test", plan.TargetPath);
        Assert.Single(plan.FileOperations);
        Assert.Equal(1024, plan.TotalSizeBytes);
        Assert.Equal(1000000, plan.AvailableSpaceBytes);
    }

    [Fact]
    public void MigrationPlan_RecommendedLinkType_ShouldDefaultToJunction()
    {
        // Arrange
        var software = new SoftwareEntry
        {
            Id = "test",
            Name = "Test",
            InstallPath = @"C:\Test"
        };

        // Act
        var plan = new MigrationPlan
        {
            Id = "plan-1",
            Software = software,
            SourcePath = @"C:\Test",
            TargetPath = @"D:\Test",
            FileOperations = Array.Empty<FileMoveOperation>(),
            TotalSizeBytes = 0,
            AvailableSpaceBytes = 1000000
        };

        // Assert
        Assert.Equal(LinkType.Junction, plan.RecommendedLinkType);
    }

    [Fact]
    public void FileMoveOperation_RequiredProperties_ShouldBeSet()
    {
        // Arrange & Act
        var op = new FileMoveOperation
        {
            SourcePath = @"C:\Source\file.txt",
            TargetPath = @"D:\Target\file.txt",
            SizeBytes = 2048
        };

        // Assert
        Assert.Equal(@"C:\Source\file.txt", op.SourcePath);
        Assert.Equal(@"D:\Target\file.txt", op.TargetPath);
        Assert.Equal(2048, op.SizeBytes);
    }

    [Theory]
    [InlineData(LinkType.Junction)]
    [InlineData(LinkType.SymbolicLink)]
    public void LinkType_AllValues_ShouldBeValid(LinkType linkType)
    {
        // Arrange & Act
        var options = new MigrationOptions { LinkType = linkType };

        // Assert
        Assert.Equal(linkType, options.LinkType);
    }

    [Fact]
    public void MigrationOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.Equal(LinkType.Junction, options.LinkType);
        Assert.True(options.UpdateRegistry);
        Assert.True(options.VerifyIntegrity);
        Assert.Equal(ConflictResolution.Ask, options.OnFileConflict);
        Assert.Equal(LockedFileHandling.Ask, options.OnLockedFile);
    }

    [Theory]
    [InlineData(ConflictResolution.Ask)]
    [InlineData(ConflictResolution.Skip)]
    [InlineData(ConflictResolution.Overwrite)]
    [InlineData(ConflictResolution.Rename)]
    public void ConflictResolution_AllValues_ShouldBeValid(ConflictResolution resolution)
    {
        // Arrange & Act
        var options = new MigrationOptions { OnFileConflict = resolution };

        // Assert
        Assert.Equal(resolution, options.OnFileConflict);
    }

    [Theory]
    [InlineData(LockedFileHandling.Ask)]
    [InlineData(LockedFileHandling.Skip)]
    [InlineData(LockedFileHandling.Abort)]
    public void LockedFileHandling_AllValues_ShouldBeValid(LockedFileHandling handling)
    {
        // Arrange & Act
        var options = new MigrationOptions { OnLockedFile = handling };

        // Assert
        Assert.Equal(handling, options.OnLockedFile);
    }

    [Fact]
    public void MigrationResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new MigrationResult { Success = true };

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.OperationId);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.MigratedFiles);
        Assert.Empty(result.SkippedFiles);
        Assert.Empty(result.FailedFiles);
    }

    [Fact]
    public void RollbackResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new RollbackResult { Success = true };

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.RestoredFiles);
        Assert.Empty(result.FailedFiles);
    }
}
