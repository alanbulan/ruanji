using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for OpenAIClient.
/// </summary>
public class OpenAIClientTests
{
    /// <summary>
    /// 简单的 Mock ConfigurationService 用于测试。
    /// </summary>
    private class MockConfigurationService : IConfigurationService
    {
        private readonly AppConfiguration _config;

        public MockConfigurationService(OpenAIConfiguration? openAIConfig = null)
        {
            _config = new AppConfiguration
            {
                OpenAIConfiguration = openAIConfig ?? new OpenAIConfiguration()
            };
        }

        public string ConfigFilePath => "test-config.json";

        public event EventHandler<ConfigurationCorruptedEventArgs>? ConfigurationCorrupted;

        public Task<AppConfiguration> GetConfigurationAsync() => Task.FromResult(_config);

        public Task SaveConfigurationAsync(AppConfiguration configuration) => Task.CompletedTask;

        public Task<AppConfiguration> ResetToDefaultAsync() => Task.FromResult(new AppConfiguration());

        public ValidationResult ValidateConfiguration(string json) => ValidationResult.Success();
    }

    private static IConfigurationService CreateMockConfigService(OpenAIConfiguration? openAIConfig = null)
    {
        return new MockConfigurationService(openAIConfig);
    }

    #region Configuration Tests

    [Fact]
    public void Constructor_WithEmptyConfig_UsesDefaults()
    {
        var mockConfigService = CreateMockConfigService();
        using var client = new OpenAIClient(mockConfigService);

        Assert.NotNull(client.Configuration);
        Assert.Equal("gpt-4o-mini", client.Configuration.Model);
        Assert.Equal("https://api.openai.com/v1", client.Configuration.BaseUrl);
    }

    [Fact]
    public async Task EnsureConfiguredAsync_WithApiKey_ReturnsTrue()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1"
        };
        var mockConfigService = CreateMockConfigService(config);

        using var client = new OpenAIClient(mockConfigService);
        var isConfigured = await client.EnsureConfiguredAsync();

        Assert.True(isConfigured);
    }

    [Fact]
    public async Task EnsureConfiguredAsync_WithoutApiKey_ReturnsFalse()
    {
        var config = new OpenAIConfiguration
        {
            ApiKey = "",
            BaseUrl = "https://api.openai.com/v1"
        };
        var mockConfigService = CreateMockConfigService(config);

        using var client = new OpenAIClient(mockConfigService);
        var isConfigured = await client.EnsureConfiguredAsync();

        Assert.False(isConfigured);
    }

    [Fact]
    public void UpdateConfiguration_UpdatesConfig()
    {
        var mockConfigService = CreateMockConfigService();
        using var client = new OpenAIClient(mockConfigService);

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

    #region ModelInfo Tests

    [Fact]
    public void ModelInfo_Constructor_SetsProperties()
    {
        var model = new ModelInfo("gpt-4o", "GPT-4o");

        Assert.Equal("gpt-4o", model.Id);
        Assert.Equal("GPT-4o", model.DisplayName);
    }

    [Fact]
    public void ModelInfo_WithSameId_AreEqual()
    {
        var model1 = new ModelInfo("gpt-4o", "GPT-4o");
        var model2 = new ModelInfo("gpt-4o", "GPT-4o Display");

        // ModelInfo 是 record，相同 Id 和 DisplayName 才相等
        Assert.NotEqual(model1, model2);
        Assert.Equal(model1.Id, model2.Id);
    }

    #endregion

    #region GetAvailableModelsAsync Tests

    [Fact]
    public async Task GetAvailableModelsAsync_NotConfigured_ReturnsEmptyList()
    {
        var mockConfigService = CreateMockConfigService();
        using var client = new OpenAIClient(mockConfigService);

        var models = await client.GetAvailableModelsAsync();

        // 未配置时返回空列表
        Assert.Empty(models);
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_NotConfigured_ReturnsFailure()
    {
        var mockConfigService = CreateMockConfigService();
        using var client = new OpenAIClient(mockConfigService);

        var result = await client.TestConnectionAsync();

        Assert.False(result.Success);
        Assert.Contains("未配置", result.Message);
    }

    #endregion

    #region SendChatCompletionAsync Tests

    [Fact]
    public async Task SendChatCompletionAsync_NotConfigured_ThrowsException()
    {
        var mockConfigService = CreateMockConfigService();
        using var client = new OpenAIClient(mockConfigService);

        var messages = new[] { ChatMessage.User("Hello") };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendChatCompletionAsync(messages));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var mockConfigService = CreateMockConfigService();
        var client = new OpenAIClient(mockConfigService);

        client.Dispose();
        client.Dispose(); // Should not throw
    }

    #endregion
}
