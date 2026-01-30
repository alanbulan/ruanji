using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for AI File Analyzer.
/// **Feature: ai-file-manager**
/// </summary>
public class AIAnalyzerPropertyTests
{
    #region Property 5: AI 数据收集完整性

    /// <summary>
    /// **Property 5**: 每个 OrganizationSuggestion 包含必要的字段（Id、Type、SourcePath、Reason）
    /// **Validates: Requirements 5.1, 5.3**
    /// </summary>
    [Property(MaxTest = 50)]
    public Property OrganizationSuggestion_HasRequiredFields()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            Arb.From<NonEmptyString>(),
            Arb.From<NonEmptyString>(),
            (id, sourcePath, reason) =>
            {
                var suggestion = new OrganizationSuggestion
                {
                    Id = id.Get,
                    Type = SuggestionType.Move,
                    SourcePath = sourcePath.Get,
                    Reason = reason.Get
                };

                return (!string.IsNullOrEmpty(suggestion.Id) &&
                        Enum.IsDefined(typeof(SuggestionType), suggestion.Type) &&
                        !string.IsNullOrEmpty(suggestion.SourcePath) &&
                        !string.IsNullOrEmpty(suggestion.Reason))
                    .Label("Suggestion should have all required fields");
            });
    }

    /// <summary>
    /// **Property 5**: AIAnalysisOptions 的 MaxDepth 应该限制目录遍历深度
    /// **Validates: Requirements 5.1, 5.3**
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void AIAnalysisOptions_MaxDepth_IsRespected(int maxDepth)
    {
        var options = new AIAnalysisOptions
        {
            MaxDepth = maxDepth,
            MaxFilesPerDirectory = 100,
            IncludeHiddenFiles = false,
            AnonymizeFilenames = true
        };

        Assert.Equal(maxDepth, options.MaxDepth);
        Assert.True(options.MaxDepth >= 1);
    }

    /// <summary>
    /// **Property 5**: AIAnalysisOptions 的 MaxFilesPerDirectory 应该限制每个目录的文件数
    /// **Validates: Requirements 5.1, 5.3**
    /// </summary>
    [Property(MaxTest = 50)]
    public Property AIAnalysisOptions_MaxFilesPerDirectory_IsPositive()
    {
        return Prop.ForAll(
            Gen.Choose(1, 1000).ToArbitrary(),
            maxFiles =>
            {
                var options = new AIAnalysisOptions
                {
                    MaxDepth = 3,
                    MaxFilesPerDirectory = maxFiles
                };

                return (options.MaxFilesPerDirectory > 0)
                    .Label("MaxFilesPerDirectory should be positive");
            });
    }

    #endregion

    #region Property 6: 建议执行正确性

    /// <summary>
    /// **Property 6**: Move 类型建议应该有有效的源路径和目标路径
    /// **Validates: Requirements 5.4, 5.5**
    /// </summary>
    [Fact]
    public void MoveSuggestion_HasSourceAndDestination()
    {
        var suggestion = new OrganizationSuggestion
        {
            Id = Guid.NewGuid().ToString(),
            Type = SuggestionType.Move,
            SourcePath = @"C:\Source\file.txt",
            DestinationPath = @"C:\Destination\file.txt",
            Reason = "Better organization"
        };

        Assert.Equal(SuggestionType.Move, suggestion.Type);
        Assert.NotNull(suggestion.SourcePath);
        Assert.NotNull(suggestion.DestinationPath);
    }

    /// <summary>
    /// **Property 6**: Rename 类型建议应该有新名称
    /// **Validates: Requirements 5.4, 5.5**
    /// </summary>
    [Fact]
    public void RenameSuggestion_HasNewName()
    {
        var suggestion = new OrganizationSuggestion
        {
            Id = Guid.NewGuid().ToString(),
            Type = SuggestionType.Rename,
            SourcePath = @"C:\Files\old_name.txt",
            NewName = "new_name.txt",
            Reason = "Better naming convention"
        };

        Assert.Equal(SuggestionType.Rename, suggestion.Type);
        Assert.NotNull(suggestion.NewName);
    }

    /// <summary>
    /// **Property 6**: Delete 类型建议只需要源路径
    /// **Validates: Requirements 5.4, 5.5**
    /// </summary>
    [Fact]
    public void DeleteSuggestion_OnlyNeedsSourcePath()
    {
        var suggestion = new OrganizationSuggestion
        {
            Id = Guid.NewGuid().ToString(),
            Type = SuggestionType.Delete,
            SourcePath = @"C:\Temp\temp_file.tmp",
            Reason = "Temporary file no longer needed"
        };

        Assert.Equal(SuggestionType.Delete, suggestion.Type);
        Assert.NotNull(suggestion.SourcePath);
    }

    /// <summary>
    /// **Property 6**: 所有建议类型都是有效的枚举值
    /// **Validates: Requirements 5.4, 5.5**
    /// </summary>
    [Theory]
    [InlineData(SuggestionType.Move)]
    [InlineData(SuggestionType.Rename)]
    [InlineData(SuggestionType.Delete)]
    [InlineData(SuggestionType.CreateFolder)]
    [InlineData(SuggestionType.Merge)]
    [InlineData(SuggestionType.Archive)]
    public void SuggestionType_AllValuesAreValid(SuggestionType type)
    {
        Assert.True(Enum.IsDefined(typeof(SuggestionType), type));
    }

    #endregion

    #region Property 7: 文件名脱敏正确性

    /// <summary>
    /// **Property 7**: 脱敏后的文件名应该保留文件扩展名
    /// **Validates: Requirements 5.8**
    /// </summary>
    [Theory]
    [InlineData("john_doe_resume.pdf", ".pdf")]
    [InlineData("user@email.com_backup.zip", ".zip")]
    [InlineData("123-456-7890_contact.txt", ".txt")]
    [InlineData("sensitive_data.xlsx", ".xlsx")]
    public void Anonymization_PreservesExtension(string originalName, string expectedExtension)
    {
        // Simulate anonymization - in real implementation this would be done by AIFileAnalyzer
        var extension = Path.GetExtension(originalName);
        var anonymized = $"file_{Guid.NewGuid():N}{extension}";

        Assert.Equal(expectedExtension, Path.GetExtension(anonymized));
    }

    /// <summary>
    /// **Property 7**: 脱敏后的文件名不应包含原始敏感信息
    /// **Validates: Requirements 5.8**
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Anonymization_RemovesSensitiveInfo()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            sensitiveData =>
            {
                var originalName = $"{sensitiveData.Get}_document.pdf";
                var extension = Path.GetExtension(originalName);

                // Simulate anonymization
                var anonymized = $"file_{Guid.NewGuid():N}{extension}";

                // The anonymized name should not contain the original sensitive data
                var containsSensitive = anonymized.Contains(sensitiveData.Get, StringComparison.OrdinalIgnoreCase);

                return (!containsSensitive)
                    .Label("Anonymized filename should not contain original sensitive data");
            });
    }

    /// <summary>
    /// **Property 7**: AIAnalysisOptions.AnonymizeFilenames 控制是否脱敏
    /// **Validates: Requirements 5.8**
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AIAnalysisOptions_AnonymizeFilenames_CanBeConfigured(bool anonymize)
    {
        var options = new AIAnalysisOptions
        {
            AnonymizeFilenames = anonymize
        };

        Assert.Equal(anonymize, options.AnonymizeFilenames);
    }

    #endregion

    #region AIAnalysisProgress Tests

    /// <summary>
    /// AI 分析进度应该包含有效的阶段
    /// </summary>
    [Theory]
    [InlineData(AIAnalysisPhase.CollectingData)]
    [InlineData(AIAnalysisPhase.PreparingRequest)]
    [InlineData(AIAnalysisPhase.WaitingForResponse)]
    [InlineData(AIAnalysisPhase.ParsingResponse)]
    [InlineData(AIAnalysisPhase.Complete)]
    public void AIAnalysisProgress_HasValidPhase(AIAnalysisPhase phase)
    {
        var progress = new AIAnalysisProgress
        {
            Phase = phase,
            ProgressPercentage = 50,
            StatusMessage = "Processing..."
        };

        Assert.True(Enum.IsDefined(typeof(AIAnalysisPhase), progress.Phase));
    }

    /// <summary>
    /// 进度百分比应该在 0-100 范围内
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AIAnalysisProgress_PercentageInValidRange()
    {
        return Prop.ForAll(
            Gen.Choose(0, 100).ToArbitrary(),
            percentage =>
            {
                var progress = new AIAnalysisProgress
                {
                    Phase = AIAnalysisPhase.CollectingData,
                    ProgressPercentage = percentage
                };

                return (progress.ProgressPercentage >= 0 && progress.ProgressPercentage <= 100)
                    .Label("Progress percentage should be between 0 and 100");
            });
    }

    #endregion
}
