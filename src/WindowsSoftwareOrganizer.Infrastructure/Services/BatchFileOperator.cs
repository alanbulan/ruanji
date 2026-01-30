using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// 批量文件操作实现。
/// </summary>
public class BatchFileOperator : IBatchFileOperator
{
    private readonly Stack<UndoOperation> _undoStack = new();
    private string? _lastOperationDescription;

    public bool CanUndo => _undoStack.Count > 0;
    public string? LastOperationDescription => _lastOperationDescription;

    public Task<BatchOperationResult> MoveAsync(
        IReadOnlyList<(string Source, string Destination)> operations,
        bool overwrite = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BatchOperationError>();
            var successfulOps = new List<(string Source, string Destination)>();
            int processed = 0;

            foreach (var (source, destination) in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                progress?.Report(new BatchOperationProgress
                {
                    TotalItems = operations.Count,
                    ProcessedItems = processed,
                    ProgressPercentage = processed * 100 / operations.Count,
                    CurrentItem = Path.GetFileName(source)
                });

                try
                {
                    var destDir = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    if (File.Exists(source))
                    {
                        if (File.Exists(destination) && !overwrite)
                        {
                            errors.Add(new BatchOperationError
                            {
                                Path = source,
                                ErrorMessage = $"目标文件已存在: {destination}"
                            });
                            continue;
                        }
                        File.Move(source, destination, overwrite);
                    }
                    else if (Directory.Exists(source))
                    {
                        Directory.Move(source, destination);
                    }
                    else
                    {
                        errors.Add(new BatchOperationError
                        {
                            Path = source,
                            ErrorMessage = "源文件或目录不存在"
                        });
                        continue;
                    }

                    successfulOps.Add((destination, source)); // 反向记录用于撤销
                }
                catch (Exception ex)
                {
                    errors.Add(new BatchOperationError
                    {
                        Path = source,
                        ErrorMessage = ex.Message,
                        Exception = ex
                    });
                }
            }

            stopwatch.Stop();

            if (successfulOps.Count > 0)
            {
                _undoStack.Push(new UndoOperation
                {
                    Type = UndoOperationType.Move,
                    Operations = successfulOps,
                    Description = $"移动 {successfulOps.Count} 个项目"
                });
                _lastOperationDescription = $"移动 {successfulOps.Count} 个项目";
            }

            return new BatchOperationResult
            {
                TotalItems = operations.Count,
                SuccessCount = successfulOps.Count,
                FailedCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }, cancellationToken);
    }

    public Task<BatchOperationResult> CopyAsync(
        IReadOnlyList<(string Source, string Destination)> operations,
        bool overwrite = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BatchOperationError>();
            var copiedFiles = new List<string>();
            int processed = 0;
            long bytesProcessed = 0;

            foreach (var (source, destination) in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                progress?.Report(new BatchOperationProgress
                {
                    TotalItems = operations.Count,
                    ProcessedItems = processed,
                    ProgressPercentage = processed * 100 / operations.Count,
                    CurrentItem = Path.GetFileName(source),
                    BytesProcessed = bytesProcessed
                });

                try
                {
                    var destDir = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    if (File.Exists(source))
                    {
                        if (File.Exists(destination) && !overwrite)
                        {
                            errors.Add(new BatchOperationError
                            {
                                Path = source,
                                ErrorMessage = $"目标文件已存在: {destination}"
                            });
                            continue;
                        }
                        File.Copy(source, destination, overwrite);
                        bytesProcessed += new FileInfo(source).Length;
                        copiedFiles.Add(destination);
                    }
                    else if (Directory.Exists(source))
                    {
                        CopyDirectory(source, destination, overwrite);
                        copiedFiles.Add(destination);
                    }
                    else
                    {
                        errors.Add(new BatchOperationError
                        {
                            Path = source,
                            ErrorMessage = "源文件或目录不存在"
                        });
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new BatchOperationError
                    {
                        Path = source,
                        ErrorMessage = ex.Message,
                        Exception = ex
                    });
                }
            }

            stopwatch.Stop();

            if (copiedFiles.Count > 0)
            {
                _undoStack.Push(new UndoOperation
                {
                    Type = UndoOperationType.Copy,
                    CopiedPaths = copiedFiles,
                    Description = $"复制 {copiedFiles.Count} 个项目"
                });
                _lastOperationDescription = $"复制 {copiedFiles.Count} 个项目";
            }

            return new BatchOperationResult
            {
                TotalItems = operations.Count,
                SuccessCount = copiedFiles.Count,
                FailedCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }, cancellationToken);
    }

    public Task<BatchOperationResult> DeleteAsync(
        IReadOnlyList<string> paths,
        bool useRecycleBin = true,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BatchOperationError>();
            int successCount = 0;
            int processed = 0;

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                progress?.Report(new BatchOperationProgress
                {
                    TotalItems = paths.Count,
                    ProcessedItems = processed,
                    ProgressPercentage = processed * 100 / paths.Count,
                    CurrentItem = Path.GetFileName(path)
                });

                try
                {
                    if (File.Exists(path))
                    {
                        if (useRecycleBin)
                        {
                            MoveToRecycleBin(path);
                        }
                        else
                        {
                            File.Delete(path);
                        }
                        successCount++;
                    }
                    else if (Directory.Exists(path))
                    {
                        if (useRecycleBin)
                        {
                            MoveToRecycleBin(path);
                        }
                        else
                        {
                            Directory.Delete(path, true);
                        }
                        successCount++;
                    }
                    else
                    {
                        errors.Add(new BatchOperationError
                        {
                            Path = path,
                            ErrorMessage = "文件或目录不存在"
                        });
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new BatchOperationError
                    {
                        Path = path,
                        ErrorMessage = ex.Message,
                        Exception = ex
                    });
                }
            }

            stopwatch.Stop();
            _lastOperationDescription = $"删除 {successCount} 个项目";

            return new BatchOperationResult
            {
                TotalItems = paths.Count,
                SuccessCount = successCount,
                FailedCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }, cancellationToken);
    }

    public async Task<BatchOperationResult> RenameAsync(
        IReadOnlyList<RenameOperation> operations,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var moveOps = operations.Select(op =>
        {
            var dir = Path.GetDirectoryName(op.SourcePath) ?? "";
            var newPath = Path.Combine(dir, op.NewName);
            return (op.SourcePath, newPath);
        }).ToList();

        return await MoveAsync(moveOps, false, progress, cancellationToken);
    }

    public async Task<BatchOperationResult> RenameByRuleAsync(
        IReadOnlyList<FileEntry> files,
        RenameRule rule,
        bool preview = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var operations = new List<RenameOperation>();
        int sequence = rule.SequenceStart;

        foreach (var file in files)
        {
            var newName = ApplyRenameRule(file, rule, sequence);
            if (newName != file.Name)
            {
                operations.Add(new RenameOperation
                {
                    SourcePath = file.FullPath,
                    NewName = newName
                });
            }
            sequence += rule.SequenceStep;
        }

        if (preview)
        {
            // 预览模式，返回预期结果但不执行
            return new BatchOperationResult
            {
                TotalItems = operations.Count,
                SuccessCount = operations.Count,
                FailedCount = 0,
                Errors = Array.Empty<BatchOperationError>(),
                Duration = TimeSpan.Zero
            };
        }

        return await RenameAsync(operations, progress, cancellationToken);
    }

    public async Task<BatchOperationResult> OrganizeByTypeAsync(
        string sourcePath,
        IReadOnlyDictionary<FileTypeCategory, string>? organizationRules = null,
        bool preview = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rules = organizationRules ?? GetDefaultOrganizationRules();
        var operations = new List<(string Source, string Destination)>();

        if (!Directory.Exists(sourcePath))
        {
            return new BatchOperationResult
            {
                TotalItems = 0,
                SuccessCount = 0,
                FailedCount = 0,
                Errors = Array.Empty<BatchOperationError>(),
                Duration = TimeSpan.Zero
            };
        }

        foreach (var file in Directory.EnumerateFiles(sourcePath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ext = Path.GetExtension(file);
            var category = FileTypeCategoryHelper.GetCategory(ext);

            if (rules.TryGetValue(category, out var folderName))
            {
                var destFolder = Path.Combine(sourcePath, folderName);
                var destPath = Path.Combine(destFolder, Path.GetFileName(file));
                operations.Add((file, destPath));
            }
        }

        if (preview)
        {
            return new BatchOperationResult
            {
                TotalItems = operations.Count,
                SuccessCount = operations.Count,
                FailedCount = 0,
                Errors = Array.Empty<BatchOperationError>(),
                Duration = TimeSpan.Zero
            };
        }

        return await MoveAsync(operations, false, progress, cancellationToken);
    }

    public async Task<BatchOperationResult> ApplySuggestionsAsync(
        IReadOnlyList<OrganizationSuggestion> suggestions,
        bool preview = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var moveOps = new List<(string Source, string Destination)>();
        var renameOps = new List<RenameOperation>();
        var deleteOps = new List<string>();
        var errors = new List<BatchOperationError>();

        foreach (var suggestion in suggestions.Where(s => s.IsSelected))
        {
            switch (suggestion.Type)
            {
                case SuggestionType.Move when !string.IsNullOrEmpty(suggestion.DestinationPath):
                    moveOps.Add((suggestion.SourcePath, suggestion.DestinationPath));
                    break;

                case SuggestionType.Rename when !string.IsNullOrEmpty(suggestion.NewName):
                    renameOps.Add(new RenameOperation
                    {
                        SourcePath = suggestion.SourcePath,
                        NewName = suggestion.NewName
                    });
                    break;

                case SuggestionType.Delete:
                    deleteOps.Add(suggestion.SourcePath);
                    break;

                case SuggestionType.CreateFolder when !string.IsNullOrEmpty(suggestion.DestinationPath):
                    if (!preview && !Directory.Exists(suggestion.DestinationPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(suggestion.DestinationPath);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new BatchOperationError
                            {
                                Path = suggestion.DestinationPath,
                                ErrorMessage = ex.Message,
                                Exception = ex
                            });
                        }
                    }
                    break;
            }
        }

        if (preview)
        {
            var totalOps = moveOps.Count + renameOps.Count + deleteOps.Count;
            return new BatchOperationResult
            {
                TotalItems = totalOps,
                SuccessCount = totalOps,
                FailedCount = 0,
                Errors = Array.Empty<BatchOperationError>(),
                Duration = TimeSpan.Zero
            };
        }

        var stopwatch = Stopwatch.StartNew();
        int successCount = 0;

        // 执行移动操作
        if (moveOps.Count > 0)
        {
            var result = await MoveAsync(moveOps, false, progress, cancellationToken);
            successCount += result.SuccessCount;
            errors.AddRange(result.Errors);
        }

        // 执行重命名操作
        if (renameOps.Count > 0)
        {
            var result = await RenameAsync(renameOps, progress, cancellationToken);
            successCount += result.SuccessCount;
            errors.AddRange(result.Errors);
        }

        // 执行删除操作
        if (deleteOps.Count > 0)
        {
            var result = await DeleteAsync(deleteOps, true, progress, cancellationToken);
            successCount += result.SuccessCount;
            errors.AddRange(result.Errors);
        }

        stopwatch.Stop();

        return new BatchOperationResult
        {
            TotalItems = moveOps.Count + renameOps.Count + deleteOps.Count,
            SuccessCount = successCount,
            FailedCount = errors.Count,
            Errors = errors,
            Duration = stopwatch.Elapsed
        };
    }

    public Task<bool> UndoLastOperationAsync(CancellationToken cancellationToken = default)
    {
        if (!CanUndo) return Task.FromResult(false);

        return Task.Run(() =>
        {
            var operation = _undoStack.Pop();

            try
            {
                switch (operation.Type)
                {
                    case UndoOperationType.Move:
                        // 反向移动
                        foreach (var (source, dest) in operation.Operations!)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (File.Exists(source))
                                File.Move(source, dest);
                            else if (Directory.Exists(source))
                                Directory.Move(source, dest);
                        }
                        break;

                    case UndoOperationType.Copy:
                        // 删除复制的文件
                        foreach (var path in operation.CopiedPaths!)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (File.Exists(path))
                                File.Delete(path);
                            else if (Directory.Exists(path))
                                Directory.Delete(path, true);
                        }
                        break;
                }

                _lastOperationDescription = null;
                return true;
            }
            catch
            {
                // 撤销失败，将操作放回栈中
                _undoStack.Push(operation);
                return false;
            }
        }, cancellationToken);
    }

    private string ApplyRenameRule(FileEntry file, RenameRule rule, int sequence)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
        var ext = file.Extension;
        var newName = nameWithoutExt;

        switch (rule.Type)
        {
            case RenameRuleType.FindReplace:
                if (!string.IsNullOrEmpty(rule.FindText))
                {
                    if (rule.UseRegex)
                    {
                        var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                        newName = Regex.Replace(newName, rule.FindText, rule.ReplaceText ?? "", options);
                    }
                    else
                    {
                        var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                        newName = newName.Replace(rule.FindText, rule.ReplaceText ?? "", comparison);
                    }
                }
                break;

            case RenameRuleType.AddPrefix:
                newName = (rule.Prefix ?? "") + newName;
                break;

            case RenameRuleType.AddSuffix:
                newName = newName + (rule.Suffix ?? "");
                break;

            case RenameRuleType.AddSequence:
                var seqStr = sequence.ToString().PadLeft(rule.SequenceDigits, '0');
                newName = $"{rule.Prefix ?? ""}{seqStr}{rule.Suffix ?? ""}";
                break;

            case RenameRuleType.AddDate:
                var dateStr = file.ModifiedTime.ToString(rule.DateFormat ?? "yyyyMMdd");
                newName = $"{newName}_{dateStr}";
                break;

            case RenameRuleType.ChangeCase:
                newName = rule.CaseConversion switch
                {
                    CaseConversion.UpperCase => newName.ToUpperInvariant(),
                    CaseConversion.LowerCase => newName.ToLowerInvariant(),
                    CaseConversion.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(newName.ToLower()),
                    CaseConversion.SentenceCase => char.ToUpper(newName[0]) + newName.Substring(1).ToLower(),
                    CaseConversion.ToggleCase => new string(newName.Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray()),
                    _ => newName
                };
                break;

            case RenameRuleType.RemoveCharacters:
                if (!string.IsNullOrEmpty(rule.FindText))
                {
                    foreach (var c in rule.FindText)
                    {
                        newName = newName.Replace(c.ToString(), "");
                    }
                }
                break;

            case RenameRuleType.RegexReplace:
                if (!string.IsNullOrEmpty(rule.FindText))
                {
                    var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    newName = Regex.Replace(newName, rule.FindText, rule.ReplaceText ?? "", options);
                }
                break;
        }

        return rule.IncludeExtension ? newName : newName + ext;
    }

    private static Dictionary<FileTypeCategory, string> GetDefaultOrganizationRules()
    {
        return new Dictionary<FileTypeCategory, string>
        {
            { FileTypeCategory.RasterImage, "图片" },
            { FileTypeCategory.VectorImage, "图片" },
            { FileTypeCategory.RawImage, "图片" },
            { FileTypeCategory.Video, "视频" },
            { FileTypeCategory.ProVideo, "视频" },
            { FileTypeCategory.LossyAudio, "音乐" },
            { FileTypeCategory.LosslessAudio, "音乐" },
            { FileTypeCategory.WordDocument, "文档" },
            { FileTypeCategory.Spreadsheet, "文档" },
            { FileTypeCategory.Presentation, "文档" },
            { FileTypeCategory.PDF, "文档" },
            { FileTypeCategory.Archive, "压缩包" },
            { FileTypeCategory.WindowsExecutable, "程序" },
            { FileTypeCategory.WindowsInstaller, "安装包" }
        };
    }

    private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir, overwrite);
        }
    }

    private static void MoveToRecycleBin(string path)
    {
        // 使用 Shell32 API 移动到回收站
        var fileOp = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = path + '\0' + '\0',
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
        };
        SHFileOperation(ref fileOp);
    }

    #region Shell32 P/Invoke

    private const int FO_DELETE = 0x0003;
    private const int FOF_ALLOWUNDO = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;
    private const int FOF_SILENT = 0x0004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public int wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pTo;
        public short fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    #endregion

    private class UndoOperation
    {
        public UndoOperationType Type { get; set; }
        public List<(string Source, string Destination)>? Operations { get; set; }
        public List<string>? CopiedPaths { get; set; }
        public string? Description { get; set; }
    }

    private enum UndoOperationType
    {
        Move,
        Copy
    }
}
