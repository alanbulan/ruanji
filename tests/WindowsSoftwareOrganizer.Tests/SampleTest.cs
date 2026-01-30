namespace WindowsSoftwareOrganizer.Tests;

/// <summary>
/// Sample test to verify the test project is configured correctly.
/// </summary>
public class SampleTest
{
    [Fact]
    public void TestProjectConfiguration_ShouldWork()
    {
        // Arrange & Act & Assert
        Assert.True(true, "Test project is configured correctly");
    }

    [Fact]
    public void CoreProjectReference_ShouldBeAccessible()
    {
        // Arrange & Act
        var category = Core.Models.SoftwareCategory.IDE;

        // Assert
        Assert.Equal(Core.Models.SoftwareCategory.IDE, category);
    }
}
