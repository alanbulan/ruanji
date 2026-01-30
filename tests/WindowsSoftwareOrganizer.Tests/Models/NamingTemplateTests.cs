namespace WindowsSoftwareOrganizer.Tests.Models;

using WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Unit tests for NamingTemplate and ValidationResult models.
/// Validates: Requirements 3.1, 3.2
/// </summary>
public class NamingTemplateTests
{
    [Fact]
    public void NamingTemplate_RequiredProperties_ShouldBeSet()
    {
        // Arrange & Act
        var template = new NamingTemplate
        {
            Id = "simple",
            Name = "Simple Mode",
            Pattern = "{Category}/{Name}"
        };

        // Assert
        Assert.Equal("simple", template.Id);
        Assert.Equal("Simple Mode", template.Name);
        Assert.Equal("{Category}/{Name}", template.Pattern);
    }

    [Fact]
    public void NamingTemplate_OptionalProperties_ShouldHaveDefaults()
    {
        // Arrange & Act
        var template = new NamingTemplate
        {
            Id = "test",
            Name = "Test",
            Pattern = "{Name}"
        };

        // Assert
        Assert.Null(template.Description);
        Assert.False(template.IsPreset);
    }

    [Fact]
    public void NamingTemplate_AllProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var template = new NamingTemplate
        {
            Id = "detailed",
            Name = "Detailed Mode",
            Pattern = "{Category}/{Vendor}_{Name}_{Version}",
            Description = "Includes vendor and version in the name",
            IsPreset = true
        };

        // Assert
        Assert.Equal("detailed", template.Id);
        Assert.Equal("Detailed Mode", template.Name);
        Assert.Equal("{Category}/{Vendor}_{Name}_{Version}", template.Pattern);
        Assert.Equal("Includes vendor and version in the name", template.Description);
        Assert.True(template.IsPreset);
    }

    [Fact]
    public void ValidationResult_Success_ShouldBeValid()
    {
        // Arrange & Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_Failure_ShouldBeInvalid()
    {
        // Arrange & Act
        var result = ValidationResult.Failure("Error 1", "Error 2");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
    }

    [Fact]
    public void ValidationResult_ManualCreation_ShouldWork()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { "Custom error" }
        };

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Custom error", result.Errors[0]);
    }

    [Fact]
    public void NamingTemplate_RecordEquality_ShouldWork()
    {
        // Arrange
        var template1 = new NamingTemplate
        {
            Id = "test",
            Name = "Test",
            Pattern = "{Name}"
        };

        var template2 = new NamingTemplate
        {
            Id = "test",
            Name = "Test",
            Pattern = "{Name}"
        };

        // Assert
        Assert.Equal(template1, template2);
    }
}
