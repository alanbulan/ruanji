namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for scanning installed software on the system.
/// </summary>
public interface ISoftwareScanner
{
    /// <summary>
    /// Scans the Windows registry for installed software.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of discovered software entries.</returns>
    Task<IReadOnlyList<Models.SoftwareEntry>> ScanInstalledSoftwareAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a directory for software installations.
    /// </summary>
    /// <param name="directoryPath">The directory path to scan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of discovered software entries.</returns>
    Task<IReadOnlyList<Models.SoftwareEntry>> ScanDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when scan progress changes.
    /// </summary>
    event EventHandler<ScanProgressEventArgs>? ProgressChanged;
}

/// <summary>
/// Event arguments for scan progress updates.
/// </summary>
public class ScanProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; init; }

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the number of items scanned so far.
    /// </summary>
    public int ItemsScanned { get; init; }
}
