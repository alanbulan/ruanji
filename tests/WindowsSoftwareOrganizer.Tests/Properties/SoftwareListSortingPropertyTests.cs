using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using Xunit;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for software list sorting correctness.
/// Feature: windows-software-organizer
/// </summary>
public class SoftwareListSortingPropertyTests
{
    /// <summary>
    /// Property 19: 软件列表排序正确性
    /// 对于任意软件列表和排序字段（类别、名称、大小、位置），排序后的列表应满足相邻元素按指定字段有序。
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SortByName_AdjacentElementsAreOrdered()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryList(),
            entries =>
            {
                if (entries.Count < 2)
                    return true.Label("List too small to test ordering");

                // Arrange & Act
                var sortedAsc = entries.OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
                var sortedDesc = entries.OrderByDescending(e => e.Name, StringComparer.Ordinal).ToList();

                // Assert - ascending order: each element should be <= next element
                for (int i = 0; i < sortedAsc.Count - 1; i++)
                {
                    var comparison = StringComparer.Ordinal.Compare(sortedAsc[i].Name, sortedAsc[i + 1].Name);
                    if (comparison > 0)
                        return false.Label($"Ascending order violated at index {i}: '{sortedAsc[i].Name}' > '{sortedAsc[i + 1].Name}'");
                }

                // Assert - descending order: each element should be >= next element
                for (int i = 0; i < sortedDesc.Count - 1; i++)
                {
                    var comparison = StringComparer.Ordinal.Compare(sortedDesc[i].Name, sortedDesc[i + 1].Name);
                    if (comparison < 0)
                        return false.Label($"Descending order violated at index {i}: '{sortedDesc[i].Name}' < '{sortedDesc[i + 1].Name}'");
                }

                return true.Label("Adjacent elements are correctly ordered by name");
            });
    }

    /// <summary>
    /// Property 19: 软件列表排序正确性 - 按类别排序
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SortByCategory_AdjacentElementsAreOrdered()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryList(),
            entries =>
            {
                // Arrange & Act
                var sortedAsc = entries.OrderBy(e => e.Category).ToList();
                var sortedDesc = entries.OrderByDescending(e => e.Category).ToList();

                // Assert - ascending order
                for (int i = 0; i < sortedAsc.Count - 1; i++)
                {
                    if (sortedAsc[i].Category > sortedAsc[i + 1].Category)
                        return false.Label($"Ascending: {sortedAsc[i].Category} should come before {sortedAsc[i + 1].Category}");
                }

                // Assert - descending order
                for (int i = 0; i < sortedDesc.Count - 1; i++)
                {
                    if (sortedDesc[i].Category < sortedDesc[i + 1].Category)
                        return false.Label($"Descending: {sortedDesc[i].Category} should come before {sortedDesc[i + 1].Category}");
                }

                return true.Label("Adjacent elements are correctly ordered by category");
            });
    }


    /// <summary>
    /// Property 19: 软件列表排序正确性 - 按大小排序
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SortBySize_AdjacentElementsAreOrdered()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryList(),
            entries =>
            {
                // Arrange & Act
                var sortedAsc = entries.OrderBy(e => e.TotalSizeBytes).ToList();
                var sortedDesc = entries.OrderByDescending(e => e.TotalSizeBytes).ToList();

                // Assert - ascending order
                for (int i = 0; i < sortedAsc.Count - 1; i++)
                {
                    if (sortedAsc[i].TotalSizeBytes > sortedAsc[i + 1].TotalSizeBytes)
                        return false.Label($"Ascending: {sortedAsc[i].TotalSizeBytes} should come before {sortedAsc[i + 1].TotalSizeBytes}");
                }

                // Assert - descending order
                for (int i = 0; i < sortedDesc.Count - 1; i++)
                {
                    if (sortedDesc[i].TotalSizeBytes < sortedDesc[i + 1].TotalSizeBytes)
                        return false.Label($"Descending: {sortedDesc[i].TotalSizeBytes} should come before {sortedDesc[i + 1].TotalSizeBytes}");
                }

                return true.Label("Adjacent elements are correctly ordered by size");
            });
    }

    /// <summary>
    /// Property 19: 软件列表排序正确性 - 按位置排序
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SortByLocation_AdjacentElementsAreOrdered()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryList(),
            entries =>
            {
                if (entries.Count < 2)
                    return true.Label("List too small to test ordering");

                // Arrange & Act
                var sortedAsc = entries.OrderBy(e => e.InstallPath, StringComparer.Ordinal).ToList();
                var sortedDesc = entries.OrderByDescending(e => e.InstallPath, StringComparer.Ordinal).ToList();

                // Assert - ascending order
                for (int i = 0; i < sortedAsc.Count - 1; i++)
                {
                    var comparison = StringComparer.Ordinal.Compare(sortedAsc[i].InstallPath, sortedAsc[i + 1].InstallPath);
                    if (comparison > 0)
                        return false.Label($"Ascending order violated at index {i}: '{sortedAsc[i].InstallPath}' > '{sortedAsc[i + 1].InstallPath}'");
                }

                // Assert - descending order
                for (int i = 0; i < sortedDesc.Count - 1; i++)
                {
                    var comparison = StringComparer.Ordinal.Compare(sortedDesc[i].InstallPath, sortedDesc[i + 1].InstallPath);
                    if (comparison < 0)
                        return false.Label($"Descending order violated at index {i}: '{sortedDesc[i].InstallPath}' < '{sortedDesc[i + 1].InstallPath}'");
                }

                return true.Label("Adjacent elements are correctly ordered by location");
            });
    }

    /// <summary>
    /// Property 19: 排序保持元素数量不变
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Sorting_PreservesElementCount()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryList(),
            entries =>
            {
                var originalCount = entries.Count;

                var sortedByName = entries.OrderBy(e => e.Name).ToList();
                var sortedByCategory = entries.OrderBy(e => e.Category).ToList();
                var sortedBySize = entries.OrderBy(e => e.TotalSizeBytes).ToList();
                var sortedByLocation = entries.OrderBy(e => e.InstallPath).ToList();

                return (sortedByName.Count == originalCount &&
                        sortedByCategory.Count == originalCount &&
                        sortedBySize.Count == originalCount &&
                        sortedByLocation.Count == originalCount)
                    .Label($"All sorted lists should have {originalCount} elements");
            });
    }

    /// <summary>
    /// Property 19: 排序保持所有元素
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Sorting_PreservesAllElements()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryList(),
            entries =>
            {
                var originalIds = entries.Select(e => e.Id).ToHashSet();

                var sortedByName = entries.OrderBy(e => e.Name).ToList();
                var sortedIds = sortedByName.Select(e => e.Id).ToHashSet();

                return originalIds.SetEquals(sortedIds)
                    .Label("Sorted list should contain all original elements");
            });
    }

    /// <summary>
    /// Property 19: 稳定排序 - 相等元素保持相对顺序
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Sorting_IsStable_ForEqualElements()
    {
        return Prop.ForAll(
            GenerateSoftwareEntryListWithDuplicateCategories(),
            entries =>
            {
                // Group by category and check that within each group, original order is preserved
                var sortedByCategory = entries.OrderBy(e => e.Category).ToList();
                
                var groupedOriginal = entries.GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList());
                
                var groupedSorted = sortedByCategory.GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList());

                foreach (var category in groupedOriginal.Keys)
                {
                    var originalOrder = groupedOriginal[category];
                    var sortedOrder = groupedSorted[category];
                    
                    if (!originalOrder.SequenceEqual(sortedOrder))
                        return false.Label($"Elements with category {category} should maintain relative order");
                }

                return true.Label("Sorting is stable for equal elements");
            });
    }

    /// <summary>
    /// Property 19: 空列表排序返回空列表
    /// Validates: Requirements 8.1
    /// </summary>
    [Fact]
    public void Sorting_EmptyList_ReturnsEmptyList()
    {
        var emptyList = new List<SoftwareEntry>();

        var sortedByName = emptyList.OrderBy(e => e.Name).ToList();
        var sortedByCategory = emptyList.OrderBy(e => e.Category).ToList();
        var sortedBySize = emptyList.OrderBy(e => e.TotalSizeBytes).ToList();
        var sortedByLocation = emptyList.OrderBy(e => e.InstallPath).ToList();

        Assert.Empty(sortedByName);
        Assert.Empty(sortedByCategory);
        Assert.Empty(sortedBySize);
        Assert.Empty(sortedByLocation);
    }

    /// <summary>
    /// Property 19: 单元素列表排序返回相同元素
    /// Validates: Requirements 8.1
    /// </summary>
    [Fact]
    public void Sorting_SingleElementList_ReturnsSameElement()
    {
        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "Test Software",
            InstallPath = @"C:\Test",
            Category = SoftwareCategory.DevTool,
            TotalSizeBytes = 1024
        };
        var singleList = new List<SoftwareEntry> { entry };

        var sortedByName = singleList.OrderBy(e => e.Name).ToList();
        var sortedByCategory = singleList.OrderBy(e => e.Category).ToList();
        var sortedBySize = singleList.OrderBy(e => e.TotalSizeBytes).ToList();
        var sortedByLocation = singleList.OrderBy(e => e.InstallPath).ToList();

        Assert.Single(sortedByName);
        Assert.Equal(entry.Id, sortedByName[0].Id);
        Assert.Single(sortedByCategory);
        Assert.Equal(entry.Id, sortedByCategory[0].Id);
        Assert.Single(sortedBySize);
        Assert.Equal(entry.Id, sortedBySize[0].Id);
        Assert.Single(sortedByLocation);
        Assert.Equal(entry.Id, sortedByLocation[0].Id);
    }

    private static Arbitrary<List<SoftwareEntry>> GenerateSoftwareEntryList()
    {
        var entryGen = from id in Gen.Elements("id-1", "id-2", "id-3", "id-4", "id-5", "id-6", "id-7", "id-8", "id-9", "id-10")
                       from name in Gen.Elements("Visual Studio", "Node.js", "Python", "Git", "Docker", "Chrome", "Firefox", "VSCode", "Notepad++", "7-Zip")
                       from category in Gen.Elements(Enum.GetValues<SoftwareCategory>())
                       from size in Gen.Choose(0, 1_000_000)
                       from pathIndex in Gen.Choose(1, 5)
                       select new SoftwareEntry
                       {
                           Id = $"{id}-{Guid.NewGuid():N}",
                           Name = name,
                           InstallPath = $@"C:\Program Files\App{pathIndex}",
                           Category = category,
                           TotalSizeBytes = (long)size * 1000
                       };

        return Gen.ListOf(entryGen).Select(fsList => fsList.ToList()).ToArbitrary();
    }

    private static Arbitrary<List<SoftwareEntry>> GenerateSoftwareEntryListWithDuplicateCategories()
    {
        var categories = new[] { SoftwareCategory.IDE, SoftwareCategory.DevTool, SoftwareCategory.Utility };
        
        var entryGen = from category in Gen.Elements(categories)
                       from index in Gen.Choose(1, 10)
                       select new SoftwareEntry
                       {
                           Id = $"id-{category}-{index}-{Guid.NewGuid():N}",
                           Name = $"Software {index}",
                           InstallPath = $@"C:\Program Files\App{index}",
                           Category = category,
                           TotalSizeBytes = index * 1000
                       };

        return Gen.ListOf(entryGen).Select(fsList => fsList.ToList()).ToArbitrary();
    }
}
