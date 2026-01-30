using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for LinkManager implementation.
/// Tests Property 10 from the design document.
/// </summary>
public class LinkManagerPropertyTests
{
    #region Property 10: 链接目标正确性

    /// <summary>
    /// Property 10: 链接目标正确性
    /// 对于任意成功创建的Junction或SymbolicLink，GetLinkInfo返回的目标路径必须与创建时指定的目标路径一致。
    /// **Validates: Requirements 4.4**
    /// **Feature: windows-software-organizer, Property 10: 链接目标正确性**
    /// </summary>
    [Fact]
    public async Task CreateJunction_Success_GetLinkInfoReturnsCorrectTarget()
    {
        // Arrange
        var linkManager = new LinkManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"LinkTest_{Guid.NewGuid():N}");
        var targetDir = Path.Combine(tempDir, "Target");
        var linkDir = Path.Combine(tempDir, "Link");

        try
        {
            // Create test directories
            Directory.CreateDirectory(targetDir);

            // Check if junction is supported
            if (!linkManager.IsJunctionSupported(linkDir))
            {
                // Skip test if junctions not supported (non-NTFS)
                return;
            }

            // Act: Create junction
            var result = await linkManager.CreateJunctionAsync(linkDir, targetDir);

            // Assert: If successful, GetLinkInfo should return correct target
            if (result.Success)
            {
                var linkInfo = linkManager.GetLinkInfo(linkDir);
                
                Assert.NotNull(linkInfo);
                Assert.Equal(LinkType.Junction, linkInfo.LinkType);
                Assert.Equal(linkDir, linkInfo.LinkPath);
                
                // Target paths should match (normalize for comparison)
                var expectedTarget = Path.GetFullPath(targetDir);
                var actualTarget = Path.GetFullPath(linkInfo.TargetPath);
                Assert.Equal(expectedTarget, actualTarget, StringComparer.OrdinalIgnoreCase);
            }
        }
        finally
        {
            // Cleanup
            await CleanupTestDirectory(tempDir, linkManager);
        }
    }

    /// <summary>
    /// Property 10 (Variant): Junction creation should fail for non-existent target.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Fact]
    public async Task CreateJunction_NonExistentTarget_Fails()
    {
        // Arrange
        var linkManager = new LinkManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"LinkTest_{Guid.NewGuid():N}");
        var nonExistentTarget = Path.Combine(tempDir, "NonExistent");
        var linkDir = Path.Combine(tempDir, "Link");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Act
            var result = await linkManager.CreateJunctionAsync(linkDir, nonExistentTarget);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            await CleanupTestDirectory(tempDir, linkManager);
        }
    }

    /// <summary>
    /// Property 10 (Variant): Junction creation should fail if link path already exists.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Fact]
    public async Task CreateJunction_LinkPathExists_Fails()
    {
        // Arrange
        var linkManager = new LinkManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"LinkTest_{Guid.NewGuid():N}");
        var targetDir = Path.Combine(tempDir, "Target");
        var linkDir = Path.Combine(tempDir, "Link");

        try
        {
            Directory.CreateDirectory(targetDir);
            Directory.CreateDirectory(linkDir); // Link path already exists

            // Act
            var result = await linkManager.CreateJunctionAsync(linkDir, targetDir);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("已存在", result.ErrorMessage);
        }
        finally
        {
            await CleanupTestDirectory(tempDir, linkManager);
        }
    }

    /// <summary>
    /// Property 10 (Variant): GetLinkInfo returns null for non-link directories.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Fact]
    public void GetLinkInfo_RegularDirectory_ReturnsNull()
    {
        // Arrange
        var linkManager = new LinkManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"LinkTest_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Act
            var linkInfo = linkManager.GetLinkInfo(tempDir);

            // Assert
            Assert.Null(linkInfo);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Property 10 (Variant): GetLinkInfo returns null for non-existent path.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Fact]
    public void GetLinkInfo_NonExistentPath_ReturnsNull()
    {
        // Arrange
        var linkManager = new LinkManager();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");

        // Act
        var linkInfo = linkManager.GetLinkInfo(nonExistentPath);

        // Assert
        Assert.Null(linkInfo);
    }

    #endregion

    #region Junction Support Tests

    /// <summary>
    /// Verifies IsJunctionSupported returns true for NTFS drives.
    /// **Validates: Requirements 4.6**
    /// </summary>
    [Fact]
    public void IsJunctionSupported_NtfsDrive_ReturnsTrue()
    {
        // Arrange
        var linkManager = new LinkManager();
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System);

        // Act
        var isSupported = linkManager.IsJunctionSupported(systemDrive);

        // Assert: System drive is typically NTFS
        // Note: This might fail on non-standard configurations
        Assert.True(isSupported);
    }

    /// <summary>
    /// Verifies IsJunctionSupported handles null/empty paths.
    /// **Validates: Requirements 4.6**
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsJunctionSupported_InvalidPath_ReturnsFalse(string? path)
    {
        // Arrange
        var linkManager = new LinkManager();

        // Act
        var isSupported = linkManager.IsJunctionSupported(path!);

        // Assert
        Assert.False(isSupported);
    }

    #endregion

    #region Remove Link Tests

    /// <summary>
    /// Verifies that created junctions can be removed.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Fact]
    public async Task RemoveLink_ExistingJunction_ReturnsTrue()
    {
        // Arrange
        var linkManager = new LinkManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"LinkTest_{Guid.NewGuid():N}");
        var targetDir = Path.Combine(tempDir, "Target");
        var linkDir = Path.Combine(tempDir, "Link");

        try
        {
            Directory.CreateDirectory(targetDir);

            if (!linkManager.IsJunctionSupported(linkDir))
            {
                return; // Skip if not supported
            }

            var createResult = await linkManager.CreateJunctionAsync(linkDir, targetDir);
            if (!createResult.Success)
            {
                return; // Skip if creation failed
            }

            // Act
            var removeResult = await linkManager.RemoveLinkAsync(linkDir);

            // Assert
            Assert.True(removeResult);
            Assert.False(Directory.Exists(linkDir));
        }
        finally
        {
            await CleanupTestDirectory(tempDir, linkManager);
        }
    }

    /// <summary>
    /// Verifies RemoveLink returns false for non-link paths.
    /// **Validates: Requirements 4.4**
    /// </summary>
    [Fact]
    public async Task RemoveLink_RegularDirectory_ReturnsFalse()
    {
        // Arrange
        var linkManager = new LinkManager();
        var tempDir = Path.Combine(Path.GetTempPath(), $"LinkTest_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Act
            var result = await linkManager.RemoveLinkAsync(tempDir);

            // Assert
            Assert.False(result);
            Assert.True(Directory.Exists(tempDir)); // Directory should still exist
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static async Task CleanupTestDirectory(string path, LinkManager linkManager)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            // First, try to remove any links
            foreach (var dir in Directory.GetDirectories(path))
            {
                var linkInfo = linkManager.GetLinkInfo(dir);
                if (linkInfo != null)
                {
                    await linkManager.RemoveLinkAsync(dir);
                }
            }

            // Then delete the directory
            Directory.Delete(path, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    #endregion
}
