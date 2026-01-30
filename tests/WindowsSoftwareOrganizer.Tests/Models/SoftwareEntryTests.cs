namespace WindowsSoftwareOrganizer.Tests.Models;

using WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Unit tests for SoftwareEntry and related models.
/// Validates: Requirements 1.2, 2.1
/// </summary>
public class SoftwareEntryTests
{
    [Fact]
    public void SoftwareEntry_RequiredProperties_ShouldBeSet()
    {
        // Arrange & Act
        var entry = new SoftwareEntry
        {
            Id = "test-id",
            Name = "Test Software",
            InstallPath = @"C:\Program Files\Test"
        };

        // Assert
        Assert.Equal("test-id", entry.Id);
        Assert.Equal("Test Software", entry.Name);
        Assert.Equal(@"C:\Program Files\Test", entry.InstallPath);
    }

    [Fact]
    public void SoftwareEntry_OptionalProperties_ShouldHaveDefaults()
    {
        // Arrange & Act
        var entry = new SoftwareEntry
        {
            Id = "test-id",
            Name = "Test Software",
            InstallPath = @"C:\Program Files\Test"
        };

        // Assert
        Assert.Null(entry.Version);
        Assert.Null(entry.Vendor);
        Assert.Null(entry.InstallDate);
        Assert.Equal(SoftwareCategory.Other, entry.Category);
        Assert.Empty(entry.RelatedDirectories);
        Assert.Equal(0, entry.TotalSizeBytes);
    }

    [Fact]
    public void SoftwareEntry_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var installDate = new DateTime(2024, 1, 15);
        var relatedDirs = new List<RelatedDirectory>
        {
            new RelatedDirectory { Path = @"C:\Users\Test\AppData\Local\Test", Type = DirectoryType.Config, SizeBytes = 1024 }
        };

        // Act
        var entry = new SoftwareEntry
        {
            Id = "test-id",
            Name = "Test Software",
            Version = "1.0.0",
            Vendor = "Test Vendor",
            InstallPath = @"C:\Program Files\Test",
            InstallDate = installDate,
            Category = SoftwareCategory.IDE,
            RelatedDirectories = relatedDirs,
            TotalSizeBytes = 1024000
        };

        // Assert
        Assert.Equal("1.0.0", entry.Version);
        Assert.Equal("Test Vendor", entry.Vendor);
        Assert.Equal(installDate, entry.InstallDate);
        Assert.Equal(SoftwareCategory.IDE, entry.Category);
        Assert.Single(entry.RelatedDirectories);
        Assert.Equal(1024000, entry.TotalSizeBytes);
    }

    [Theory]
    [InlineData(SoftwareCategory.IDE)]
    [InlineData(SoftwareCategory.SDK)]
    [InlineData(SoftwareCategory.Runtime)]
    [InlineData(SoftwareCategory.DevTool)]
    [InlineData(SoftwareCategory.Database)]
    [InlineData(SoftwareCategory.Browser)]
    [InlineData(SoftwareCategory.Office)]
    [InlineData(SoftwareCategory.Media)]
    [InlineData(SoftwareCategory.Utility)]
    [InlineData(SoftwareCategory.Other)]
    public void SoftwareCategory_AllValues_ShouldBeValid(SoftwareCategory category)
    {
        // Arrange & Act
        var entry = new SoftwareEntry
        {
            Id = "test",
            Name = "Test",
            InstallPath = @"C:\Test",
            Category = category
        };

        // Assert
        Assert.Equal(category, entry.Category);
    }

    [Fact]
    public void RelatedDirectory_RequiredProperties_ShouldBeSet()
    {
        // Arrange & Act
        var dir = new RelatedDirectory
        {
            Path = @"C:\Users\Test\AppData\Local\Test",
            Type = DirectoryType.Cache
        };

        // Assert
        Assert.Equal(@"C:\Users\Test\AppData\Local\Test", dir.Path);
        Assert.Equal(DirectoryType.Cache, dir.Type);
        Assert.Equal(0, dir.SizeBytes);
    }

    [Theory]
    [InlineData(DirectoryType.Install)]
    [InlineData(DirectoryType.Config)]
    [InlineData(DirectoryType.Cache)]
    [InlineData(DirectoryType.Log)]
    [InlineData(DirectoryType.Data)]
    [InlineData(DirectoryType.Temp)]
    public void DirectoryType_AllValues_ShouldBeValid(DirectoryType type)
    {
        // Arrange & Act
        var dir = new RelatedDirectory
        {
            Path = @"C:\Test",
            Type = type,
            SizeBytes = 1024
        };

        // Assert
        Assert.Equal(type, dir.Type);
    }

    [Fact]
    public void SoftwareEntry_RecordEquality_ShouldWork()
    {
        // Arrange
        var entry1 = new SoftwareEntry
        {
            Id = "test-id",
            Name = "Test Software",
            InstallPath = @"C:\Program Files\Test"
        };

        var entry2 = new SoftwareEntry
        {
            Id = "test-id",
            Name = "Test Software",
            InstallPath = @"C:\Program Files\Test"
        };

        // Assert
        Assert.Equal(entry1, entry2);
    }
}
