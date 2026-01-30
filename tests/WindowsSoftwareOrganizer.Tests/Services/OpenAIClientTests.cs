using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for OpenAIClient.
/// </summary>
public class OpenAIClientTests
{
    #region Configuration Tests

    [Fact]
    public void Constructor_WithNullConfig_UsesDefaults()
    {
        using var client = new OpenAIClient();

        Assert.NotNull(client.Configuration);
        Assert.Equal("gpt-4o-mini", client.Configuration.Model);
        Assert.Equal("https://api.openai.com/v1", client.Configuration.BaseUrl);
    }

    [Fact]
    public void Constructor_WithConfig_UsesProvidedConfig()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "test-key",
            Model = "gpt-4",
            BaseUrl = "https://custom.api.com/v1"
        };

        using var client = new OpenAIClient(config);

        Assert.Equal("test-key", client.Configuration.ApiKey);
        Assert.Equal("gpt-4", client.Configuration.Model);
        Assert.Equal("https://custom.api.com/v1", client.Configuration.BaseUrl);
    }

    [Fact]
    public void IsConfigured_WithApiKey_ReturnsTrue()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1"
        };

        using var client = new OpenAIClient(config);

        Assert.True(client.IsConfigured);
    }

    [Fact]
    public void IsConfigured_WithoutApiKey_ReturnsFalse()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "",
            BaseUrl = "https://api.openai.com/v1"
        };

        using var client = new OpenAIClient(config);

        Assert.False(client.IsConfigured);
    }

    [Fact]
    public void UpdateConfiguration_UpdatesConfig()
    {
        using var client = new OpenAIClient();

        var newConfig = new OpenAIConfiguration
        {
            ApiKey = "new-key",
            Model = "gpt-4-turbo"
        };

        client.UpdateConfiguration(newConfig);

        Assert.Equal("new-key", client.Configuration.ApiKey);
        Assert.Equal("gpt-4-turbo", client.Configuration.Model);
    }

    #endregion

    #region OpenAIConfiguration Tests

    [Fact]
    public void OpenAIConfiguration_IsValid_WithApiKeyAndBaseUrl_ReturnsTrue()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1"
        };

        Assert.True(config.IsValid);
    }

    [Fact]
    public void OpenAIConfiguration_IsValid_WithoutApiKey_ReturnsFalse()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "",
            BaseUrl = "https://api.openai.com/v1"
        };

        Assert.False(config.IsValid);
    }

    [Fact]
    public void OpenAIConfiguration_IsValid_WithoutBaseUrl_ReturnsFalse()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = ""
        };

        Assert.False(config.IsValid);
    }

    [Fact]
    public void OpenAIConfiguration_DefaultValues_AreCorrect()
    {
        var config = new OpenAIConfiguration();

        Assert.Equal("gpt-4o-mini", config.Model);
        Assert.Equal("https://api.openai.com/v1", config.BaseUrl);
        Assert.Equal(4096, config.MaxTokens);
        Assert.Equal(0.7, config.Temperature);
        Assert.Equal(60, config.TimeoutSeconds);
        Assert.False(config.IsEnabled);
    }

    #endregion

    #region ChatMessage Tests

    [Fact]
    public void ChatMessage_System_CreatesSystemMessage()
    {
        var message = ChatMessage.System("You are a helpful assistant.");

        Assert.Equal("system", message.Role);
        Assert.Equal("You are a helpful assistant.", message.Content);
    }

    [Fact]
    public void ChatMessage_User_CreatesUserMessage()
    {
        var message = ChatMessage.User("Hello!");

        Assert.Equal("user", message.Role);
        Assert.Equal("Hello!", message.Content);
    }

    [Fact]
    public void ChatMessage_Assistant_CreatesAssistantMessage()
    {
        var message = ChatMessage.Assistant("Hi there!");

        Assert.Equal("assistant", message.Role);
        Assert.Equal("Hi there!", message.Content);
    }

    #endregion

    #region APITestResult Tests

    [Fact]
    public void APITestResult_Success_HasCorrectProperties()
    {
        var result = new APITestResult
        {
            Success = true,
            Message = "Connection successful",
            ResponseTimeMs = 150,
            Model = "gpt-4"
        };

        Assert.True(result.Success);
        Assert.Equal("Connection successful", result.Message);
        Assert.Equal(150, result.ResponseTimeMs);
        Assert.Equal("gpt-4", result.Model);
    }

    [Fact]
    public void APITestResult_Failure_HasErrorDetails()
    {
        var result = new APITestResult
        {
            Success = false,
            Message = "Connection failed",
            ErrorDetails = "Invalid API key"
        };

        Assert.False(result.Success);
        Assert.Equal("Connection failed", result.Message);
        Assert.Equal("Invalid API key", result.ErrorDetails);
    }

    #endregion

    #region OpenAIModels Tests

    [Fact]
    public void OpenAIModels_AvailableModels_ContainsExpectedModels()
    {
        var models = OpenAIModels.AvailableModels;

        Assert.NotEmpty(models);
        Assert.Contains(models, m => m.Id == "gpt-4o");
        Assert.Contains(models, m => m.Id == "gpt-4o-mini");
        Assert.Contains(models, m => m.Id == "gpt-4-turbo");
    }

    [Fact]
    public void OpenAIModels_ModelInfo_HasRequiredProperties()
    {
        var model = OpenAIModels.AvailableModels.First();

        Assert.False(string.IsNullOrEmpty(model.Id));
        Assert.False(string.IsNullOrEmpty(model.DisplayName));
        Assert.False(string.IsNullOrEmpty(model.Description));
        Assert.True(model.MaxContextTokens > 0);
        Assert.True(model.MaxOutputTokens > 0);
    }

    #endregion

    #region GetAvailableModelsAsync Tests

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsModels()
    {
        using var client = new OpenAIClient();

        var models = await client.GetAvailableModelsAsync();

        Assert.NotEmpty(models);
        Assert.Contains("gpt-4o-mini", models);
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_NotConfigured_ReturnsFailure()
    {
        using var client = new OpenAIClient();

        var result = await client.TestConnectionAsync();

        Assert.False(result.Success);
        Assert.Contains("未配置", result.Message);
    }

    #endregion

    #region SendChatCompletionAsync Tests

    [Fact]
    public async Task SendChatCompletionAsync_NotConfigured_ThrowsException()
    {
        using var client = new OpenAIClient();

        var messages = new[] { ChatMessage.User("Hello") };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendChatCompletionAsync(messages));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var client = new OpenAIClient();

        client.Dispose();
        client.Dispose(); // Should not throw
    }

    #endregion
}
