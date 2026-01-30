using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for managing NTFS junctions and symbolic links.
/// </summary>
public interface ILinkManager
{
    /// <summary>
    /// Checks if junction creation is supported for the given path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if junctions are supported.</returns>
    bool IsJunctionSupported(string path);

    /// <summary>
    /// Checks if symbolic link creation is supported (requires admin privileges).
    /// </summary>
    /// <returns>True if symbolic links are supported.</returns>
    bool IsSymbolicLinkSupported();

    /// <summary>
    /// Creates a junction (directory hard link) at the specified path.
    /// </summary>
    /// <param name="linkPath">The path where the junction will be created.</param>
    /// <param name="targetPath">The target path the junction will point to.</param>
    /// <returns>The result of the link creation operation.</returns>
    Task<LinkResult> CreateJunctionAsync(string linkPath, string targetPath);

    /// <summary>
    /// Creates a symbolic link at the specified path.
    /// </summary>
    /// <param name="linkPath">The path where the symbolic link will be created.</param>
    /// <param name="targetPath">The target path the symbolic link will point to.</param>
    /// <returns>The result of the link creation operation.</returns>
    Task<LinkResult> CreateSymbolicLinkAsync(string linkPath, string targetPath);

    /// <summary>
    /// Removes a junction or symbolic link.
    /// </summary>
    /// <param name="linkPath">The path of the link to remove.</param>
    /// <returns>True if the link was successfully removed.</returns>
    Task<bool> RemoveLinkAsync(string linkPath);

    /// <summary>
    /// Gets information about a link at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>Link information, or null if the path is not a link.</returns>
    LinkInfo? GetLinkInfo(string path);
}
