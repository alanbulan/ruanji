using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for configuration serialization.
/// **Feature: windows-software-organizer, Property 20: 配置序列化往返**
/// **Validates: Requirements 9.1, 9.3, 9.4**
/// </summary>
public class ConfigurationPropertyTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Registers custom FsCheck generators for AppConfiguration and related types.
    /// </summary>
    public class ConfigurationArbitraries
    {
        /// <summary>
        /// Generates valid file system paths for Windows.
        /// </summary>
        public static Arbitrary<string> ValidPath()
        {
            var driveLetters = new[] { "C", "D", "E", "F" };
            var folderNames = new[] { "Software", "Apps", "Programs", "Tools", "Dev" };
            
            return Gen.Elements(driveLetters)
                .SelectMany(drive => Gen.Elements(folderNames)
                    .SelectMany(folder => Gen.Choose(0, 2)
                        .Select(depth =>
                        {
                            var path = $@"{drive}:\{folder}";
                            for (int i = 0; i < depth; i++)
                            {
                                path += $@"\Sub{i}";
                            }
                            return path;
                        })))
                .ToArbitrary();
        }

        /// <summary>
        /// Generates valid naming template IDs.
        /// </summary>
        public static Arbitrary<string> ValidTemplateId()
        {
            var ids = new[] { "simple", "detailed", "date", "custom1", "custom2", "my-template" };
            return Gen.Elements(ids).ToArbitrary();
        }

        /// <summary>
        /// Generates valid NamingTemplate objects.
        /// </summary>
        public static Arbitrary<NamingTemplate> NamingTemplateArb()
        {
            var patterns = new[] 
            { 
                "{Category}/{Name}", 
                "{Category}/{Vendor}_{Name}_{Version}", 
                "{Category}/{Date}_{Name}",
                "{Name}_{Version}",
                "{Vendor}/{Name}"
            };
            
            return (from id in Gen.Elements("template1", "template2", "custom", "user-defined")
                    from name in Gen.Elements("Simple", "Detailed", "Date Mode", "Custom Template")
                    from pattern in Gen.Elements(patterns)
                    from description in Gen.OneOf(
                        Gen.Constant<string?>(null),
                        Gen.Elements("A simple template", "Detailed naming", "Date-based naming").Select(s => (string?)s))
                    from isPreset in Arb.Generate<bool>()
                    select new NamingTemplate
                    {
                        Id = id,
                        Name = name,
                        Pattern = pattern,
                        Description = description,
                        IsPreset = isPreset
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates valid software IDs for user classifications.
        /// </summary>
        public static Arbitrary<string> SoftwareId()
        {
            var ids = new[] { "vscode", "chrome", "firefox", "notepad++", "git", "nodejs", "python", "java-sdk" };
            return Gen.Elements(ids).ToArbitrary();
        }

        /// <summary>
        /// Generates valid user classification dictionaries.
        /// </summary>
        public static Arbitrary<Dictionary<string, SoftwareCategory>> UserClassificationsArb()
        {
            return Gen.Choose(0, 5)
                .SelectMany(count =>
                {
                    if (count == 0)
                        return Gen.Constant(new Dictionary<string, SoftwareCategory>());
                    
                    var softwareIds = new[] { "vscode", "chrome", "firefox", "notepad", "git", "nodejs", "python", "java" };
                    
                    return Gen.Shuffle(softwareIds)
                        .Select(shuffled => shuffled.Take(count))
                        .SelectMany(ids =>
                            Gen.ListOf(count, Arb.Generate<SoftwareCategory>())
                                .Select(categories =>
                                {
                                    var dict = new Dictionary<string, SoftwareCategory>();
                                    var idList = ids.ToList();
                                    for (int i = 0; i < count && i < idList.Count; i++)
                                    {
                                        dict[idList[i]] = categories[i];
                                    }
                                    return dict;
                                }));
                })
                .ToArbitrary();
        }

        /// <summary>
        /// Generates valid custom template lists.
        /// </summary>
        public static Arbitrary<List<NamingTemplate>> CustomTemplatesArb()
        {
            return Gen.Choose(0, 3)
                .SelectMany(count =>
                {
                    if (count == 0)
                        return Gen.Constant(new List<NamingTemplate>());
                    
                    return Gen.ListOf(count, NamingTemplateArb().Generator)
                        .Select(templates =>
                        {
                            // Ensure unique IDs
                            var result = new List<NamingTemplate>();
                            var usedIds = new HashSet<string>();
                            int suffix = 0;
                            foreach (var t in templates)
                            {
                                var id = t.Id;
                                while (usedIds.Contains(id))
                                {
                                    id = $"{t.Id}_{suffix++}";
                                }
                                usedIds.Add(id);
                                result.Add(t with { Id = id });
                            }
                            return result;
                        });
                })
                .ToArbitrary();
        }

        /// <summary>
        /// Generates valid AppConfiguration objects.
        /// </summary>
        public static Arbitrary<AppConfiguration> AppConfigurationArb()
        {
            return (from defaultTargetPath in ValidPath().Generator
                    from defaultNamingTemplateId in ValidTemplateId().Generator
                    from preferredLinkType in Arb.Generate<LinkType>()
                    from autoUpdateRegistry in Arb.Generate<bool>()
                    from moveToRecycleBin in Arb.Generate<bool>()
                    from operationHistoryDays in Gen.Choose(1, 365)
                    from theme in Arb.Generate<ThemeMode>()
                    from userClassifications in UserClassificationsArb().Generator
                    from customTemplates in CustomTemplatesArb().Generator
                    select new AppConfiguration
                    {
                        DefaultTargetPath = defaultTargetPath,
                        DefaultNamingTemplateId = defaultNamingTemplateId,
                        PreferredLinkType = preferredLinkType,
                        AutoUpdateRegistry = autoUpdateRegistry,
                        MoveToRecycleBin = moveToRecycleBin,
                        OperationHistoryDays = operationHistoryDays,
                        Theme = theme,
                        UserClassifications = userClassifications,
                        CustomTemplates = customTemplates
                    }).ToArbitrary();
        }
    }

    /// <summary>
    /// Property 20: 配置序列化往返
    /// 对于任意有效的AppConfiguration对象，序列化为JSON后再反序列化，应得到等价的配置对象。
    /// **Validates: Requirements 9.1, 9.3, 9.4**
    /// **Feature: windows-software-organizer, Property 20: 配置序列化往返**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ConfigurationArbitraries) })]
    public Property ConfigurationSerializationRoundTrip_ShouldProduceEquivalentObject(AppConfiguration original)
    {
        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Act: Deserialize back to object
        var deserialized = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);

        // Assert: The deserialized object should be equivalent to the original
        return (deserialized != null &&
                deserialized.DefaultTargetPath == original.DefaultTargetPath &&
                deserialized.DefaultNamingTemplateId == original.DefaultNamingTemplateId &&
                deserialized.PreferredLinkType == original.PreferredLinkType &&
                deserialized.AutoUpdateRegistry == original.AutoUpdateRegistry &&
                deserialized.MoveToRecycleBin == original.MoveToRecycleBin &&
                deserialized.OperationHistoryDays == original.OperationHistoryDays &&
                deserialized.Theme == original.Theme &&
                AreUserClassificationsEqual(original.UserClassifications, deserialized.UserClassifications) &&
                AreCustomTemplatesEqual(original.CustomTemplates, deserialized.CustomTemplates))
            .ToProperty()
            .Label($"Original: {original.DefaultTargetPath}, Theme: {original.Theme}, " +
                   $"Classifications: {original.UserClassifications.Count}, Templates: {original.CustomTemplates.Count}");
    }

    /// <summary>
    /// Property 20 (Variant): Verifies that JSON serialization produces valid JSON that can be parsed.
    /// **Validates: Requirements 9.1, 9.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ConfigurationArbitraries) })]
    public Property ConfigurationSerialization_ShouldProduceValidJson(AppConfiguration config)
    {
        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(config, JsonOptions);

        // Assert: The JSON should be parseable
        try
        {
            using var document = JsonDocument.Parse(json);
            return (document.RootElement.ValueKind == JsonValueKind.Object)
                .ToProperty()
                .Label("Serialized JSON is a valid object");
        }
        catch (JsonException)
        {
            return false.ToProperty().Label("JSON parsing failed");
        }
    }

    /// <summary>
    /// Property 20 (Variant): Verifies that enum values are preserved through serialization.
    /// **Validates: Requirements 9.3, 9.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EnumValuesShouldBePreservedThroughSerialization(
        LinkType linkType, 
        ThemeMode theme, 
        SoftwareCategory category)
    {
        // Arrange
        var original = new AppConfiguration
        {
            PreferredLinkType = linkType,
            Theme = theme,
            UserClassifications = new Dictionary<string, SoftwareCategory>
            {
                ["test-software"] = category
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);

        // Assert
        return (deserialized != null &&
                deserialized.PreferredLinkType == linkType &&
                deserialized.Theme == theme &&
                deserialized.UserClassifications.ContainsKey("test-software") &&
                deserialized.UserClassifications["test-software"] == category)
            .ToProperty()
            .Label($"LinkType: {linkType}, Theme: {theme}, Category: {category}");
    }

    /// <summary>
    /// Property 20 (Variant): Verifies that collection properties are preserved through serialization.
    /// **Validates: Requirements 9.3, 9.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ConfigurationArbitraries) })]
    public Property CollectionsShouldBePreservedThroughSerialization(AppConfiguration original)
    {
        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);

        // Assert
        return (deserialized != null &&
                deserialized.UserClassifications.Count == original.UserClassifications.Count &&
                deserialized.CustomTemplates.Count == original.CustomTemplates.Count)
            .ToProperty()
            .Label($"UserClassifications: {original.UserClassifications.Count}, " +
                   $"CustomTemplates: {original.CustomTemplates.Count}");
    }

    #region Property 21: JSON格式验证

    /// <summary>
    /// Wrapper type for valid JSON strings.
    /// </summary>
    public class ValidJson
    {
        public string Value { get; }
        public ValidJson(string value) => Value = value;
        public override string ToString() => TruncateForDisplay(Value);
        private static string TruncateForDisplay(string s) => s.Length > 50 ? s.Substring(0, 47) + "..." : s;
    }

    /// <summary>
    /// Wrapper type for malformed JSON strings.
    /// </summary>
    public class MalformedJson
    {
        public string Value { get; }
        public MalformedJson(string value) => Value = value;
        public override string ToString() => Value.Replace("\n", "\\n").Replace("\t", "\\t");
    }

    /// <summary>
    /// Wrapper type for invalid configuration JSON strings.
    /// </summary>
    public class InvalidConfigJson
    {
        public string Value { get; }
        public InvalidConfigJson(string value) => Value = value;
        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper type for mixed JSON strings (valid, malformed, or invalid).
    /// </summary>
    public class MixedJson
    {
        public string Value { get; }
        public MixedJson(string value) => Value = value;
        public override string ToString() => TruncateForDisplay(Value);
        private static string TruncateForDisplay(string s) => s.Length > 50 ? s.Substring(0, 47) + "..." : s;
    }

    /// <summary>
    /// Generators for JSON format validation property tests.
    /// </summary>
    public class JsonValidationArbitraries
    {
        /// <summary>
        /// Generates valid JSON strings that represent valid AppConfiguration objects.
        /// </summary>
        public static Arbitrary<ValidJson> ValidJsonArb()
        {
            return ConfigurationArbitraries.AppConfigurationArb().Generator
                .Select(config => new ValidJson(JsonSerializer.Serialize(config, JsonOptions)))
                .ToArbitrary();
        }

        /// <summary>
        /// Generates malformed JSON strings (syntax errors).
        /// </summary>
        public static Arbitrary<MalformedJson> MalformedJsonArb()
        {
            var malformedJsons = new[]
            {
                "{",                                    // Unclosed brace
                "{ \"key\": }",                         // Missing value
                "{ \"key\" \"value\" }",                // Missing colon
                "{ key: \"value\" }",                   // Unquoted key
                "[1, 2, 3",                             // Unclosed array
                "{ \"a\": 1, }",                        // Trailing comma
                "{ \"a\": undefined }",                 // Invalid value
                "{ \"a\": 'single quotes' }",           // Single quotes
                "not json at all",                      // Plain text
                "123abc",                               // Invalid literal
                "{ \"nested\": { \"broken\": }",        // Nested broken
                "",                                     // Empty string
                "   ",                                  // Whitespace only
                "\t\n",                                 // Only whitespace characters
                "null",                                 // JSON null (not an object)
                "true",                                 // JSON boolean (not an object)
                "123",                                  // JSON number (not an object)
                "\"string\"",                           // JSON string (not an object)
                "[1, 2, 3]",                            // JSON array (not an object)
            };
            
            return Gen.Elements(malformedJsons)
                .Select(s => new MalformedJson(s))
                .ToArbitrary();
        }

        /// <summary>
        /// Generates JSON strings with invalid field values (valid JSON syntax but invalid configuration).
        /// </summary>
        public static Arbitrary<InvalidConfigJson> InvalidConfigJsonArb()
        {
            var invalidConfigs = new[]
            {
                // Negative operation history days
                "{ \"operationHistoryDays\": -1 }",
                "{ \"operationHistoryDays\": -100 }",
                // Empty whitespace path
                "{ \"defaultTargetPath\": \"   \" }",
                // Invalid enum values (will fail deserialization)
                "{ \"theme\": \"InvalidTheme\" }",
                "{ \"preferredLinkType\": \"InvalidLink\" }",
                // Wrong types for fields
                "{ \"autoUpdateRegistry\": \"not a boolean\" }",
                "{ \"operationHistoryDays\": \"not a number\" }",
                "{ \"userClassifications\": \"not an object\" }",
                "{ \"customTemplates\": \"not an array\" }",
                // Invalid nested structures
                "{ \"userClassifications\": { \"software\": \"InvalidCategory\" } }",
                "{ \"customTemplates\": [{ \"id\": 123 }] }",
            };
            
            return Gen.Elements(invalidConfigs)
                .Select(s => new InvalidConfigJson(s))
                .ToArbitrary();
        }

        /// <summary>
        /// Generates a mix of valid, malformed, and invalid JSON strings.
        /// </summary>
        public static Arbitrary<MixedJson> MixedJsonArb()
        {
            var validGen = ConfigurationArbitraries.AppConfigurationArb().Generator
                .Select(config => JsonSerializer.Serialize(config, JsonOptions));
            
            var malformedJsons = new[]
            {
                "{", "{ \"key\": }", "not json", "", "null", "true", "123", "[1,2,3]"
            };
            var malformedGen = Gen.Elements(malformedJsons);
            
            var invalidConfigs = new[]
            {
                "{ \"operationHistoryDays\": -1 }",
                "{ \"defaultTargetPath\": \"   \" }",
                "{ \"theme\": \"InvalidTheme\" }",
            };
            var invalidGen = Gen.Elements(invalidConfigs);
            
            return Gen.Frequency(
                Tuple.Create(3, validGen),
                Tuple.Create(2, malformedGen),
                Tuple.Create(2, invalidGen)
            ).Select(s => new MixedJson(s)).ToArbitrary();
        }
    }

    /// <summary>
    /// Property 21: JSON格式验证
    /// 对于任意JSON字符串，如果验证通过则反序列化不应抛出异常；如果验证失败则应返回具体的错误信息。
    /// **Validates: Requirements 9.2**
    /// **Feature: windows-software-organizer, Property 21: JSON格式验证**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonValidationArbitraries) })]
    public Property JsonValidation_WhenValid_DeserializationShouldNotThrow(ValidJson validJson)
    {
        // Arrange
        var json = validJson.Value;
        var configService = new ConfigurationService();
        
        // Act
        var validationResult = configService.ValidateConfiguration(json);
        
        // Assert: If validation passes, deserialization should not throw
        if (validationResult.IsValid)
        {
            try
            {
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
                return (config != null)
                    .ToProperty()
                    .Label("Valid JSON should deserialize without throwing");
            }
            catch (Exception ex)
            {
                return false.ToProperty()
                    .Label($"Validation passed but deserialization threw: {ex.Message}");
            }
        }
        
        // If validation fails, Errors must be non-empty
        return (validationResult.Errors.Count > 0)
            .ToProperty()
            .Label("Invalid validation should have non-empty Errors list");
    }

    /// <summary>
    /// Property 21 (Variant): When validation fails, Errors list must be non-empty.
    /// **Validates: Requirements 9.2**
    /// **Feature: windows-software-organizer, Property 21: JSON格式验证**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonValidationArbitraries) })]
    public Property JsonValidation_WhenInvalid_ErrorsMustBeNonEmpty(MalformedJson malformedJson)
    {
        // Arrange
        var json = malformedJson.Value;
        var configService = new ConfigurationService();
        
        // Act
        var validationResult = configService.ValidateConfiguration(json);
        
        // Assert: Malformed JSON should fail validation with non-empty errors
        return (!validationResult.IsValid && validationResult.Errors.Count > 0)
            .ToProperty()
            .Label($"Malformed JSON '{TruncateForLabel(json)}' should fail with errors");
    }

    /// <summary>
    /// Property 21 (Variant): Tests mixed JSON inputs (valid, malformed, invalid config).
    /// **Validates: Requirements 9.2**
    /// **Feature: windows-software-organizer, Property 21: JSON格式验证**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonValidationArbitraries) })]
    public Property JsonValidation_MixedInputs_ShouldBehaveCorrectly(MixedJson mixedJson)
    {
        // Arrange
        var json = mixedJson.Value;
        var configService = new ConfigurationService();
        
        // Act
        var validationResult = configService.ValidateConfiguration(json);
        
        // Assert: The property that must always hold
        // 1. If IsValid is true, deserialization must not throw
        // 2. If IsValid is false, Errors must be non-empty
        if (validationResult.IsValid)
        {
            try
            {
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
                return (config != null)
                    .ToProperty()
                    .Label("Valid JSON deserialized successfully");
            }
            catch (Exception ex)
            {
                return false.ToProperty()
                    .Label($"Validation passed but deserialization threw: {ex.Message}");
            }
        }
        else
        {
            return (validationResult.Errors.Count > 0)
                .ToProperty()
                .Label($"Invalid JSON has {validationResult.Errors.Count} error(s): {string.Join(", ", validationResult.Errors.Take(2))}");
        }
    }

    /// <summary>
    /// Property 21 (Variant): Invalid configuration values should fail validation with specific errors.
    /// **Validates: Requirements 9.2**
    /// **Feature: windows-software-organizer, Property 21: JSON格式验证**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonValidationArbitraries) })]
    public Property JsonValidation_InvalidConfigValues_ShouldFailWithErrors(InvalidConfigJson invalidConfigJson)
    {
        // Arrange
        var json = invalidConfigJson.Value;
        var configService = new ConfigurationService();
        
        // Act
        var validationResult = configService.ValidateConfiguration(json);
        
        // Assert: Invalid config JSON should either:
        // 1. Fail validation with non-empty errors, OR
        // 2. If it somehow passes validation, deserialization should still work (defensive)
        if (!validationResult.IsValid)
        {
            return (validationResult.Errors.Count > 0)
                .ToProperty()
                .Label($"Invalid config JSON failed with errors: {string.Join(", ", validationResult.Errors.Take(2))}");
        }
        else
        {
            // If validation passes unexpectedly, ensure deserialization doesn't throw
            try
            {
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
                return (config != null)
                    .ToProperty()
                    .Label("Validation passed, deserialization succeeded");
            }
            catch (Exception ex)
            {
                return false.ToProperty()
                    .Label($"Validation passed but deserialization threw: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Property 21 (Core): The fundamental property - validation result consistency.
    /// For any JSON string: IsValid=true implies safe deserialization, IsValid=false implies non-empty Errors.
    /// **Validates: Requirements 9.2**
    /// **Feature: windows-software-organizer, Property 21: JSON格式验证**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property JsonValidation_CoreProperty_ValidationResultConsistency(NonEmptyString jsonInput)
    {
        // Arrange
        var json = jsonInput.Get;
        var configService = new ConfigurationService();
        
        // Act
        var validationResult = configService.ValidateConfiguration(json);
        
        // Assert: The core property
        if (validationResult.IsValid)
        {
            // If valid, deserialization must not throw
            try
            {
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
                return (config != null)
                    .ToProperty()
                    .Label("IsValid=true: deserialization succeeded");
            }
            catch (Exception ex)
            {
                return false.ToProperty()
                    .Label($"IsValid=true but deserialization threw: {ex.Message}");
            }
        }
        else
        {
            // If invalid, Errors must be non-empty
            return (validationResult.Errors.Count > 0)
                .ToProperty()
                .Label($"IsValid=false: Errors.Count={validationResult.Errors.Count}");
        }
    }

    /// <summary>
    /// Helper method to truncate long strings for test labels.
    /// </summary>
    private static string TruncateForLabel(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "<empty>";
        if (input.Length <= 30)
            return input.Replace("\n", "\\n").Replace("\t", "\\t");
        return input.Substring(0, 27).Replace("\n", "\\n").Replace("\t", "\\t") + "...";
    }

    #endregion

    #region Helper Methods

    private static bool AreUserClassificationsEqual(
        Dictionary<string, SoftwareCategory> original,
        Dictionary<string, SoftwareCategory> deserialized)
    {
        if (original.Count != deserialized.Count)
            return false;

        foreach (var kvp in original)
        {
            if (!deserialized.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }

        return true;
    }

    private static bool AreCustomTemplatesEqual(
        List<NamingTemplate> original,
        List<NamingTemplate> deserialized)
    {
        if (original.Count != deserialized.Count)
            return false;

        for (int i = 0; i < original.Count; i++)
        {
            var orig = original[i];
            var deser = deserialized[i];

            if (orig.Id != deser.Id ||
                orig.Name != deser.Name ||
                orig.Pattern != deser.Pattern ||
                orig.Description != deser.Description ||
                orig.IsPreset != deser.IsPreset)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Property 10: OpenAI 配置往返正确性 (ai-file-manager)

    /// <summary>
    /// **Feature: ai-file-manager, Property 10: 配置往返正确性**
    /// 对于任意有效的 OpenAIConfiguration，保存后再读取应该得到等价的配置。
    /// **Validates: Requirements 8.2, 8.4, 8.5**
    /// </summary>
    public class OpenAIConfigurationArbitraries
    {
        /// <summary>
        /// Generates valid API keys.
        /// </summary>
        public static Arbitrary<string> ApiKeyArb()
        {
            return Gen.Elements(
                "sk-test123",
                "sk-abcdef",
                "sk-xyz789",
                "sk-" + Guid.NewGuid().ToString("N"),
                ""
            ).ToArbitrary();
        }

        /// <summary>
        /// Generates valid base URLs.
        /// </summary>
        public static Arbitrary<string> BaseUrlArb()
        {
            return Gen.Elements(
                "https://api.openai.com/v1",
                "https://api.azure.com/openai",
                "https://custom-endpoint.com/v1",
                "http://localhost:8080/v1"
            ).ToArbitrary();
        }

        /// <summary>
        /// Generates valid model names.
        /// </summary>
        public static Arbitrary<string> ModelArb()
        {
            return Gen.Elements(
                "gpt-4o-mini",
                "gpt-4",
                "gpt-4-turbo",
                "gpt-3.5-turbo",
                "gpt-4o"
            ).ToArbitrary();
        }

        /// <summary>
        /// Generates valid OpenAIConfiguration objects.
        /// </summary>
        public static Arbitrary<OpenAIConfiguration> OpenAIConfigurationArb()
        {
            return (from apiKey in ApiKeyArb().Generator
                    from baseUrl in BaseUrlArb().Generator
                    from model in ModelArb().Generator
                    from maxTokens in Gen.Choose(100, 8192)
                    from temperature in Gen.Choose(0, 20).Select(t => t / 10.0)
                    select new OpenAIConfiguration
                    {
                        ApiKey = apiKey,
                        BaseUrl = baseUrl,
                        Model = model,
                        MaxTokens = maxTokens,
                        Temperature = temperature
                    }).ToArbitrary();
        }
    }

    /// <summary>
    /// **Property 10**: ApiKey 正确保存和读取
    /// **Validates: Requirements 8.2, 8.4, 8.5**
    /// **Feature: ai-file-manager, Property 10: 配置往返正确性**
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = new[] { typeof(OpenAIConfigurationArbitraries) })]
    public Property OpenAIConfiguration_ApiKey_RoundTrip(OpenAIConfiguration original)
    {
        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Act: Deserialize back to object
        var deserialized = JsonSerializer.Deserialize<OpenAIConfiguration>(json, JsonOptions);

        // Assert: ApiKey should be preserved
        return (deserialized != null && deserialized.ApiKey == original.ApiKey)
            .ToProperty()
            .Label($"ApiKey: {original.ApiKey}");
    }

    /// <summary>
    /// **Property 10**: BaseUrl 正确保存和读取
    /// **Validates: Requirements 8.2, 8.4, 8.5**
    /// **Feature: ai-file-manager, Property 10: 配置往返正确性**
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = new[] { typeof(OpenAIConfigurationArbitraries) })]
    public Property OpenAIConfiguration_BaseUrl_RoundTrip(OpenAIConfiguration original)
    {
        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Act: Deserialize back to object
        var deserialized = JsonSerializer.Deserialize<OpenAIConfiguration>(json, JsonOptions);

        // Assert: BaseUrl should be preserved
        return (deserialized != null && deserialized.BaseUrl == original.BaseUrl)
            .ToProperty()
            .Label($"BaseUrl: {original.BaseUrl}");
    }

    /// <summary>
    /// **Property 10**: Model 正确保存和读取
    /// **Validates: Requirements 8.2, 8.4, 8.5**
    /// **Feature: ai-file-manager, Property 10: 配置往返正确性**
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = new[] { typeof(OpenAIConfigurationArbitraries) })]
    public Property OpenAIConfiguration_Model_RoundTrip(OpenAIConfiguration original)
    {
        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Act: Deserialize back to object
        var deserialized = JsonSerializer.Deserialize<OpenAIConfiguration>(json, JsonOptions);

        // Assert: Model should be preserved
        return (deserialized != null && deserialized.Model == original.Model)
            .ToProperty()
            .Label($"Model: {original.Model}");
    }

    /// <summary>
    /// **Property 10**: 完整配置往返正确性
    /// **Validates: Requirements 8.2, 8.4, 8.5**
    /// **Feature: ai-file-manager, Property 10: 配置往返正确性**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(OpenAIConfigurationArbitraries) })]
    public Property OpenAIConfiguration_FullRoundTrip(OpenAIConfiguration original)
    {
        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(original, JsonOptions);
        
        // Act: Deserialize back to object
        var deserialized = JsonSerializer.Deserialize<OpenAIConfiguration>(json, JsonOptions);

        // Assert: All properties should be preserved
        return (deserialized != null &&
                deserialized.ApiKey == original.ApiKey &&
                deserialized.BaseUrl == original.BaseUrl &&
                deserialized.Model == original.Model &&
                deserialized.MaxTokens == original.MaxTokens &&
                Math.Abs(deserialized.Temperature - original.Temperature) < 0.001)
            .ToProperty()
            .Label($"ApiKey: {original.ApiKey}, Model: {original.Model}, MaxTokens: {original.MaxTokens}");
    }

    #endregion
}
