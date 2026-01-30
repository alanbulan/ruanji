using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for classifying software into categories and finding related directories.
/// </summary>
public interface ISoftwareClassifier
{
    /// <summary>
    /// Classifies a software entry into a category.
    /// </summary>
    /// <param name="entry">The software entry to classify.</param>
    /// <returns>The determined software category.</returns>
    SoftwareCategory Classify(SoftwareEntry entry);

    /// <summary>
    /// Finds all directories related to a software entry.
    /// </summary>
    /// <param name="entry">The software entry to analyze.</param>
    /// <returns>A list of related directories.</returns>
    IReadOnlyList<RelatedDirectory> FindRelatedDirectories(SoftwareEntry entry);

    /// <summary>
    /// Saves a user's classification preference for a software.
    /// </summary>
    /// <param name="softwareId">The software identifier.</param>
    /// <param name="category">The user-assigned category.</param>
    Task SaveUserClassificationAsync(string softwareId, SoftwareCategory category);

    /// <summary>
    /// Gets a user's saved classification preference for a software.
    /// </summary>
    /// <param name="softwareId">The software identifier.</param>
    /// <returns>The user-assigned category, or null if not set.</returns>
    Task<SoftwareCategory?> GetUserClassificationAsync(string softwareId);
}
