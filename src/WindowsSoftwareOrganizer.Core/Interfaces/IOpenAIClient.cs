using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// OpenAI 客户端接口 - 与 OpenAI API 通信。
/// </summary>
public interface IOpenAIClient
{
    /// <summary>
    /// 发送 Chat Completion 请求。
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应内容</returns>
    Task<string> SendChatCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送 Chat Completion 请求并获取完整响应。
    /// </summary>
    /// <param name="request">请求对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整响应</returns>
    Task<ChatCompletionResponse> SendChatCompletionRequestAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送流式 Chat Completion 请求。
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应内容流</returns>
    IAsyncEnumerable<string> StreamChatCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试 API 连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果</returns>
    Task<APITestResult> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用指定配置测试 API 连接。
    /// </summary>
    /// <param name="configuration">要测试的配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果</returns>
    Task<APITestResult> TestConnectionAsync(OpenAIConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前配置。
    /// </summary>
    OpenAIConfiguration Configuration { get; }

    /// <summary>
    /// 更新配置。
    /// </summary>
    /// <param name="configuration">新配置</param>
    void UpdateConfiguration(OpenAIConfiguration configuration);

    /// <summary>
    /// 配置客户端（UpdateConfiguration 的别名）。
    /// </summary>
    /// <param name="configuration">新配置</param>
    void Configure(OpenAIConfiguration configuration);

    /// <summary>
    /// 检查配置是否有效。
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// 获取可用的模型列表（从 API 动态获取）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型列表</returns>
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用指定配置获取可用的模型列表。
    /// </summary>
    /// <param name="configuration">API 配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型列表</returns>
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(OpenAIConfiguration configuration, CancellationToken cancellationToken = default);
}
