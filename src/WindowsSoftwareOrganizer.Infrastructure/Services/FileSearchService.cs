using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// 文件搜索服务实现。
/// </summary>
public class FileSearchService : IFileSearchService
{
    public IAsyncEnumerable<FileEntry> SearchAsync(
        FileSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        return SearchAsyncCore(criteria, cancellationToken);
    }

    private async IAsyncEnumerable<FileEntry> SearchAsyncCore(
        FileSearchCriteria criteria,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pattern = criteria.Pattern;
        var regex = criteria.UseWildcard ? WildcardToRegex(pattern, criteria.MatchCase) : 
            new Regex(pattern, criteria.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);

        var searchOption = criteria.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        // 从当前目录开始搜索
        var startPath = Environment.CurrentDirectory;
        if (!Directory.Exists(startPath)) yield break;

        var dirInfo = new DirectoryInfo(startPath);
        
        await Task.Yield(); // 确保异步执行
        
        foreach (var file in dirInfo.EnumerateFiles("*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchesCriteria(file, regex, criteria))
            {
                yield return CreateFileEntry(file);
            }
        }
    }

    public async Task<FileSearchResult> SearchAllAsync(
        FileSearchCriteria criteria,
        int maxResults = 1000,
        IProgress<FileSearchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var files = new List<FileEntry>();
        var pattern = criteria.Pattern;
        var regex = criteria.UseWildcard ? WildcardToRegex(pattern, criteria.MatchCase) :
            new Regex(pattern, criteria.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);

        var searchOption = criteria.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var startPath = Environment.CurrentDirectory;
        
        int filesSearched = 0;
        int dirsSearched = 0;
        bool reachedLimit = false;

        await Task.Run(() =>
        {
            if (!Directory.Exists(startPath)) return;

            var dirInfo = new DirectoryInfo(startPath);

            foreach (var file in dirInfo.EnumerateFiles("*", searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();
                filesSearched++;

                if (MatchesCriteria(file, regex, criteria))
                {
                    files.Add(CreateFileEntry(file));
                    
                    if (files.Count >= maxResults)
                    {
                        reachedLimit = true;
                        break;
                    }
                }

                // 每 100 个文件报告一次进度
                if (filesSearched % 100 == 0)
                {
                    progress?.Report(new FileSearchProgress
                    {
                        CurrentDirectory = file.DirectoryName ?? startPath,
                        FilesSearched = filesSearched,
                        DirectoriesSearched = dirsSearched,
                        MatchesFound = files.Count
                    });
                }
            }
        }, cancellationToken);

        stopwatch.Stop();

        return new FileSearchResult
        {
            Criteria = criteria,
            Files = files,
            DirectoriesSearched = dirsSearched,
            FilesSearched = filesSearched,
            Duration = stopwatch.Elapsed,
            ReachedLimit = reachedLimit
        };
    }

    public async IAsyncEnumerable<FileEntry> QuickSearchAsync(
        string path,
        string pattern,
        bool recursive = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path)) yield break;

        var regex = WildcardToRegex(pattern, false);
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var dirInfo = new DirectoryInfo(path);

        await Task.Yield(); // 确保异步执行

        var matchedFiles = new List<FileEntry>();
        foreach (var file in dirInfo.EnumerateFiles("*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (regex.IsMatch(file.Name))
                {
                    matchedFiles.Add(CreateFileEntry(file));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        foreach (var file in matchedFiles)
        {
            yield return file;
        }
    }

    public async IAsyncEnumerable<DirectoryEntry> SearchDirectoriesAsync(
        string path,
        string pattern,
        bool recursive = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path)) yield break;

        var regex = WildcardToRegex(pattern, false);
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var dirInfo = new DirectoryInfo(path);

        await Task.Yield();

        var matchedDirs = new List<DirectoryEntry>();
        foreach (var dir in dirInfo.EnumerateDirectories("*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (regex.IsMatch(dir.Name))
                {
                    matchedDirs.Add(CreateDirectoryEntry(dir));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        foreach (var dir in matchedDirs)
        {
            yield return dir;
        }
    }

    public async IAsyncEnumerable<FileContentMatch> SearchContentAsync(
        string path,
        string content,
        string? filePattern = null,
        bool caseSensitive = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path)) yield break;

        var dirInfo = new DirectoryInfo(path);
        var searchPattern = filePattern ?? "*";
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        await Task.Yield();

        // 只搜索文本文件
        var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".log", ".md", ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf",
            ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".hpp", ".css", ".html", ".htm",
            ".sql", ".sh", ".bat", ".cmd", ".ps1", ".rb", ".go", ".rs", ".swift", ".kt", ".scala"
        };

        var results = new List<FileContentMatch>();

        foreach (var file in dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 跳过非文本文件和大文件
            if (!textExtensions.Contains(file.Extension) || file.Length > 10 * 1024 * 1024)
                continue;

            var matches = new List<ContentMatchLine>();

            try
            {
                var lines = await File.ReadAllLinesAsync(file.FullName, cancellationToken);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var index = line.IndexOf(content, comparison);
                    if (index >= 0)
                    {
                        matches.Add(new ContentMatchLine
                        {
                            LineNumber = i + 1,
                            Content = line.Length > 500 ? line.Substring(0, 500) + "..." : line,
                            MatchPosition = index,
                            MatchLength = content.Length
                        });
                    }
                }

                if (matches.Count > 0)
                {
                    results.Add(new FileContentMatch
                    {
                        File = CreateFileEntry(file),
                        Matches = matches
                    });
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        foreach (var result in results)
        {
            yield return result;
        }
    }

    public async Task<IReadOnlyList<FileEntry>> GetRecentFilesAsync(
        string path,
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        var files = new List<FileEntry>();

        await Task.Run(() =>
        {
            if (!Directory.Exists(path)) return;

            var dirInfo = new DirectoryInfo(path);
            
            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    files.Add(CreateFileEntry(file));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }, cancellationToken);

        return files
            .OrderByDescending(f => f.ModifiedTime)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<FileEntry>> GetRecentlyAccessedFilesAsync(
        string path,
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        var files = new List<FileEntry>();

        await Task.Run(() =>
        {
            if (!Directory.Exists(path)) return;

            var dirInfo = new DirectoryInfo(path);

            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    files.Add(CreateFileEntry(file));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }, cancellationToken);

        return files
            .OrderByDescending(f => f.AccessedTime)
            .Take(count)
            .ToList();
    }

    private static bool MatchesCriteria(FileInfo file, Regex nameRegex, FileSearchCriteria criteria)
    {
        try
        {
            // 文件名匹配
            if (!nameRegex.IsMatch(file.Name))
                return false;

            // 扩展名过滤
            if (criteria.Extensions != null && criteria.Extensions.Count > 0)
            {
                var ext = file.Extension.TrimStart('.');
                if (!criteria.Extensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                    e.Equals(file.Extension, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            // 大小过滤
            var size = file.Length;
            if (criteria.MinSize.HasValue && size < criteria.MinSize.Value)
                return false;
            if (criteria.MaxSize.HasValue && size > criteria.MaxSize.Value)
                return false;

            // 日期过滤
            var modified = file.LastWriteTime;
            if (criteria.ModifiedAfter.HasValue && modified < criteria.ModifiedAfter.Value)
                return false;
            if (criteria.ModifiedBefore.HasValue && modified > criteria.ModifiedBefore.Value)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Regex WildcardToRegex(string pattern, bool caseSensitive)
    {
        // 转换通配符为正则表达式
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        
        var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        return new Regex(regexPattern, options);
    }

    private static FileEntry CreateFileEntry(FileInfo file)
    {
        return new FileEntry
        {
            Name = file.Name,
            FullPath = file.FullName,
            Extension = file.Extension,
            Size = file.Length,
            CreatedTime = file.CreationTime,
            ModifiedTime = file.LastWriteTime,
            AccessedTime = file.LastAccessTime,
            Attributes = file.Attributes,
            Category = FileTypeCategoryHelper.GetCategory(file.Extension),
            ParentPath = file.DirectoryName
        };
    }

    private static DirectoryEntry CreateDirectoryEntry(DirectoryInfo dir)
    {
        bool hasSubdirs = false;
        try { hasSubdirs = dir.EnumerateDirectories().Any(); } catch { }

        return new DirectoryEntry
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            CreatedTime = dir.CreationTime,
            ModifiedTime = dir.LastWriteTime,
            AccessedTime = dir.LastAccessTime,
            Attributes = dir.Attributes,
            HasSubdirectories = hasSubdirs,
            IsAccessible = true,
            ParentPath = dir.Parent?.FullName
        };
    }
}
