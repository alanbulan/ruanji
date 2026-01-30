using System.Security.Cryptography;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of IMigrationEngine for managing software migrations.
/// Implements requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6
/// </summary>
public class MigrationEngine : IMigrationEngine
{
    private readonly ILinkManager _linkManager;
    private readonly IRegistryUpdater _registryUpdater;
    private readonly IOperationLogger _operationLogger;
    private readonly INamingEngine _namingEngine;
    private readonly Dictionary<string, MigrationState> _migrationStates = new();

    public MigrationEngine(
        ILinkManager linkManager,
        IRegistryUpdater registryUpdater,
        IOperationLogger operationLogger,
        INamingEngine namingEngine)
    {
        _linkManager = linkManager ?? throw new ArgumentNullException(nameof(linkManager));
        _registryUpdater = registryUpdater ?? throw new ArgumentNullException(nameof(registryUpdater));
        _operationLogger = operationLogger ?? throw new ArgumentNullException(nameof(operationLogger));
        _namingEngine = namingEngine ?? throw new ArgumentNullException(nameof(namingEngine));
    }

    /// <inheritdoc />
    public Task<MigrationPlan> CreatePlanAsync(
        SoftwareEntry entry,
        string targetBasePath,
        NamingTemplate template)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrWhiteSpace(targetBasePath))
            throw new ArgumentException("Target base path cannot be null or empty.", nameof(targetBasePath));
        if (template == null) throw new ArgumentNullException(nameof(template));

        var targetName = _namingEngine.GenerateName(entry, template.Pattern);
        var targetPath = _namingEngine.ResolveConflict(targetBasePath, targetName);

        // Get available space at target
        var driveRoot = Path.GetPathRoot(targetPath);
        long availableSpace = 0;
        if (!string.IsNullOrEmpty(driveRoot))
        {
            try
            {
                var driveInfo = new DriveInfo(driveRoot);
                availableSpace = driveInfo.AvailableFreeSpace;
            }
            catch { /* Use 0 if we can't determine */ }
        }

        // Build file operations list
        var fileOperations = new List<FileMoveOperation>();
        long totalSize = 0;

        if (Directory.Exists(entry.InstallPath))
        {
            foreach (var file in Directory.EnumerateFiles(entry.InstallPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var relativePath = Path.GetRelativePath(entry.InstallPath, file);
                    var targetFilePath = Path.Combine(targetPath, relativePath);

                    fileOperations.Add(new FileMoveOperation
                    {
                        SourcePath = file,
                        TargetPath = targetFilePath,
                        SizeBytes = fileInfo.Length
                    });
                    totalSize += fileInfo.Length;
                }
                catch { /* Skip inaccessible files */ }
            }
        }

        var plan = new MigrationPlan
        {
            Id = Guid.NewGuid().ToString("N"),
            Software = entry,
            SourcePath = entry.InstallPath,
            TargetPath = targetPath,
            FileOperations = fileOperations,
            TotalSizeBytes = totalSize > 0 ? totalSize : entry.TotalSizeBytes,
            AvailableSpaceBytes = availableSpace,
            RecommendedLinkType = _linkManager.IsSymbolicLinkSupported() ? LinkType.SymbolicLink : LinkType.Junction
        };

        return Task.FromResult(plan);
    }


    /// <inheritdoc />
    public async Task<MigrationResult> ExecuteAsync(
        MigrationPlan plan,
        MigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        if (options == null) throw new ArgumentNullException(nameof(options));

        // Check space (Property 9: 迁移空间检查)
        if (plan.TotalSizeBytes > plan.AvailableSpaceBytes)
        {
            return new MigrationResult
            {
                Success = false,
                ErrorMessage = "目标位置磁盘空间不足"
            };
        }

        var operationId = await _operationLogger.BeginOperationAsync(
            OperationType.Migration,
            $"迁移 {plan.Software.Name} 从 {plan.SourcePath} 到 {plan.TargetPath}");

        var state = new MigrationState
        {
            OperationId = operationId,
            SourcePath = plan.SourcePath,
            TargetPath = plan.TargetPath,
            CopiedFiles = new List<string>(),
            OriginalFiles = new Dictionary<string, string>()
        };
        _migrationStates[operationId] = state;

        var migratedFiles = new List<string>();
        var skippedFiles = new List<string>();
        var failedFiles = new List<string>();
        long bytesTransferred = 0;

        try
        {
            // Create target directory
            Directory.CreateDirectory(plan.TargetPath);

            // Copy files
            for (int i = 0; i < plan.FileOperations.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var op = plan.FileOperations[i];
                var result = await CopyFileAsync(op, options, state, cancellationToken);

                switch (result)
                {
                    case CopyResult.Success:
                        migratedFiles.Add(op.SourcePath);
                        bytesTransferred += op.SizeBytes;
                        break;
                    case CopyResult.Skipped:
                        skippedFiles.Add(op.SourcePath);
                        break;
                    case CopyResult.Failed:
                        failedFiles.Add(op.SourcePath);
                        break;
                }

                progress?.Report(new MigrationProgress
                {
                    ProgressPercentage = (int)((i + 1) * 100 / plan.FileOperations.Count),
                    StatusMessage = $"正在复制文件 {i + 1}/{plan.FileOperations.Count}",
                    CurrentFile = op.SourcePath,
                    BytesTransferred = bytesTransferred,
                    TotalBytes = plan.TotalSizeBytes
                });
            }

            // Verify integrity if requested
            if (options.VerifyIntegrity && migratedFiles.Count > 0)
            {
                progress?.Report(new MigrationProgress
                {
                    ProgressPercentage = 95,
                    StatusMessage = "正在验证文件完整性..."
                });

                var verifyFailed = await VerifyIntegrityAsync(plan.FileOperations, cancellationToken);
                if (verifyFailed.Count > 0)
                {
                    failedFiles.AddRange(verifyFailed);
                    migratedFiles.RemoveAll(f => verifyFailed.Contains(f));
                }
            }

            // Delete original files and create link
            if (failedFiles.Count == 0 && migratedFiles.Count > 0)
            {
                progress?.Report(new MigrationProgress
                {
                    ProgressPercentage = 97,
                    StatusMessage = "正在删除原始文件..."
                });

                // Delete original directory contents
                await DeleteOriginalFilesAsync(plan.SourcePath, state, cancellationToken);

                // Create link from source to target
                progress?.Report(new MigrationProgress
                {
                    ProgressPercentage = 98,
                    StatusMessage = "正在创建链接..."
                });

                var linkResult = options.LinkType == LinkType.SymbolicLink
                    ? await _linkManager.CreateSymbolicLinkAsync(plan.SourcePath, plan.TargetPath)
                    : await _linkManager.CreateJunctionAsync(plan.SourcePath, plan.TargetPath);

                if (!linkResult.Success)
                {
                    // Rollback if link creation fails
                    await RollbackFilesAsync(state, cancellationToken);
                    await _operationLogger.CompleteOperationAsync(operationId, false);
                    return new MigrationResult
                    {
                        Success = false,
                        OperationId = operationId,
                        ErrorMessage = $"创建链接失败: {linkResult.ErrorMessage}"
                    };
                }

                state.LinkCreated = true;
                state.LinkPath = plan.SourcePath;

                // Update registry if requested
                if (options.UpdateRegistry)
                {
                    progress?.Report(new MigrationProgress
                    {
                        ProgressPercentage = 99,
                        StatusMessage = "正在更新注册表..."
                    });

                    var references = await _registryUpdater.FindReferencesAsync(plan.SourcePath, cancellationToken);
                    if (references.Count > 0)
                    {
                        state.RegistryBackupId = await _registryUpdater.CreateBackupAsync(references);
                        await _registryUpdater.UpdateReferencesAsync(references, plan.SourcePath, plan.TargetPath);
                    }
                }
            }

            await _operationLogger.CompleteOperationAsync(operationId, failedFiles.Count == 0);

            progress?.Report(new MigrationProgress
            {
                ProgressPercentage = 100,
                StatusMessage = "迁移完成"
            });

            return new MigrationResult
            {
                Success = failedFiles.Count == 0,
                OperationId = operationId,
                MigratedFiles = migratedFiles,
                SkippedFiles = skippedFiles,
                FailedFiles = failedFiles,
                ErrorMessage = failedFiles.Count > 0 ? $"{failedFiles.Count} 个文件迁移失败" : null
            };
        }
        catch (OperationCanceledException)
        {
            await RollbackFilesAsync(state, CancellationToken.None);
            await _operationLogger.CompleteOperationAsync(operationId, false);
            return new MigrationResult
            {
                Success = false,
                OperationId = operationId,
                ErrorMessage = "操作已取消",
                MigratedFiles = migratedFiles,
                SkippedFiles = skippedFiles,
                FailedFiles = failedFiles
            };
        }
        catch (Exception ex)
        {
            await RollbackFilesAsync(state, CancellationToken.None);
            await _operationLogger.CompleteOperationAsync(operationId, false);
            return new MigrationResult
            {
                Success = false,
                OperationId = operationId,
                ErrorMessage = ex.Message,
                MigratedFiles = migratedFiles,
                SkippedFiles = skippedFiles,
                FailedFiles = failedFiles
            };
        }
    }


    /// <inheritdoc />
    public async Task<RollbackResult> RollbackAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("Operation ID cannot be null or empty.", nameof(operationId));

        var operation = await _operationLogger.GetOperationAsync(operationId);
        if (operation == null)
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = "找不到指定的操作记录"
            };
        }

        if (!_migrationStates.TryGetValue(operationId, out var state))
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = "找不到迁移状态信息，无法回滚"
            };
        }

        var restoredFiles = new List<string>();
        var failedFiles = new List<string>();

        try
        {
            // Restore registry if backed up
            if (!string.IsNullOrEmpty(state.RegistryBackupId))
            {
                try
                {
                    await _registryUpdater.RestoreBackupAsync(state.RegistryBackupId);
                }
                catch { /* Continue with file rollback */ }
            }

            // Remove link if created
            if (state.LinkCreated && !string.IsNullOrEmpty(state.LinkPath))
            {
                try
                {
                    await _linkManager.RemoveLinkAsync(state.LinkPath);
                }
                catch { /* Continue with file rollback */ }
            }

            // Restore original files
            foreach (var (targetFile, sourceFile) in state.OriginalFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var sourceDir = Path.GetDirectoryName(sourceFile);
                    if (!string.IsNullOrEmpty(sourceDir))
                        Directory.CreateDirectory(sourceDir);

                    if (File.Exists(targetFile))
                    {
                        File.Copy(targetFile, sourceFile, true);
                        restoredFiles.Add(sourceFile);
                    }
                }
                catch
                {
                    failedFiles.Add(sourceFile);
                }
            }

            // Clean up target directory
            if (Directory.Exists(state.TargetPath) && restoredFiles.Count > 0)
            {
                try
                {
                    Directory.Delete(state.TargetPath, true);
                }
                catch { /* Best effort cleanup */ }
            }

            _migrationStates.Remove(operationId);

            return new RollbackResult
            {
                Success = failedFiles.Count == 0,
                RestoredFiles = restoredFiles,
                FailedFiles = failedFiles,
                ErrorMessage = failedFiles.Count > 0 ? $"{failedFiles.Count} 个文件恢复失败" : null
            };
        }
        catch (Exception ex)
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                RestoredFiles = restoredFiles,
                FailedFiles = failedFiles
            };
        }
    }

    private async Task<CopyResult> CopyFileAsync(
        FileMoveOperation op,
        MigrationOptions options,
        MigrationState state,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if file is locked
            if (IsFileLocked(op.SourcePath))
            {
                if (options.OnLockedFile == LockedFileHandling.Skip)
                    return CopyResult.Skipped;
                if (options.OnLockedFile == LockedFileHandling.Abort)
                    throw new IOException($"文件被锁定: {op.SourcePath}");
                // For Ask, we skip in automated mode
                return CopyResult.Skipped;
            }

            // Check if target exists
            if (File.Exists(op.TargetPath))
            {
                switch (options.OnFileConflict)
                {
                    case ConflictResolution.Skip:
                        return CopyResult.Skipped;
                    case ConflictResolution.Rename:
                        // Generate unique name
                        var dir = Path.GetDirectoryName(op.TargetPath) ?? "";
                        var name = Path.GetFileNameWithoutExtension(op.TargetPath);
                        var ext = Path.GetExtension(op.TargetPath);
                        var counter = 1;
                        var newPath = op.TargetPath;
                        while (File.Exists(newPath))
                        {
                            newPath = Path.Combine(dir, $"{name}_{counter++}{ext}");
                        }
                        // Use the new path
                        break;
                    case ConflictResolution.Ask:
                        // In automated mode, skip
                        return CopyResult.Skipped;
                    // Overwrite continues below
                }
            }

            // Create target directory
            var targetDir = Path.GetDirectoryName(op.TargetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            // Copy file
            await Task.Run(() => File.Copy(op.SourcePath, op.TargetPath, true), cancellationToken);

            state.CopiedFiles.Add(op.TargetPath);
            state.OriginalFiles[op.TargetPath] = op.SourcePath;

            return CopyResult.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return CopyResult.Failed;
        }
    }


    private async Task<List<string>> VerifyIntegrityAsync(
        IReadOnlyList<FileMoveOperation> operations,
        CancellationToken cancellationToken)
    {
        var failedFiles = new List<string>();

        foreach (var op in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(op.TargetPath))
            {
                failedFiles.Add(op.SourcePath);
                continue;
            }

            try
            {
                var sourceHash = await ComputeFileHashAsync(op.SourcePath, cancellationToken);
                var targetHash = await ComputeFileHashAsync(op.TargetPath, cancellationToken);

                if (!sourceHash.SequenceEqual(targetHash))
                    failedFiles.Add(op.SourcePath);
            }
            catch
            {
                failedFiles.Add(op.SourcePath);
            }
        }

        return failedFiles;
    }

    private static async Task<byte[]> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var sha256 = SHA256.Create();
        return await sha256.ComputeHashAsync(stream, cancellationToken);
    }

    private static async Task DeleteOriginalFilesAsync(
        string sourcePath,
        MigrationState state,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sourcePath))
            return;

        await Task.Run(() =>
        {
            // Delete all files first
            foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { File.Delete(file); }
                catch { /* Best effort */ }
            }

            // Delete empty directories
            foreach (var dir in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length))
            {
                try
                {
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch { /* Best effort */ }
            }

            // Delete root directory
            try
            {
                if (Directory.Exists(sourcePath) && !Directory.EnumerateFileSystemEntries(sourcePath).Any())
                    Directory.Delete(sourcePath);
            }
            catch { /* Best effort */ }
        }, cancellationToken);
    }

    private async Task RollbackFilesAsync(MigrationState state, CancellationToken cancellationToken)
    {
        // Remove link if created
        if (state.LinkCreated && !string.IsNullOrEmpty(state.LinkPath))
        {
            try { await _linkManager.RemoveLinkAsync(state.LinkPath); }
            catch { /* Best effort */ }
        }

        // Restore original files from target
        foreach (var (targetFile, sourceFile) in state.OriginalFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var sourceDir = Path.GetDirectoryName(sourceFile);
                if (!string.IsNullOrEmpty(sourceDir))
                    Directory.CreateDirectory(sourceDir);

                if (File.Exists(targetFile))
                    File.Copy(targetFile, sourceFile, true);
            }
            catch { /* Best effort */ }
        }

        // Clean up copied files
        foreach (var file in state.CopiedFiles)
        {
            try { if (File.Exists(file)) File.Delete(file); }
            catch { /* Best effort */ }
        }

        // Clean up target directory if empty
        if (Directory.Exists(state.TargetPath))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(state.TargetPath).Any())
                    Directory.Delete(state.TargetPath);
            }
            catch { /* Best effort */ }
        }
    }

    private static bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    private enum CopyResult { Success, Skipped, Failed }

    private class MigrationState
    {
        public string OperationId { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public List<string> CopiedFiles { get; set; } = new();
        public Dictionary<string, string> OriginalFiles { get; set; } = new();
        public bool LinkCreated { get; set; }
        public string? LinkPath { get; set; }
        public string? RegistryBackupId { get; set; }
    }
}
