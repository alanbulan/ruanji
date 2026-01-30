using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of ILinkManager for managing NTFS junctions and symbolic links.
/// Implements requirements 4.4, 4.6
/// </summary>
public class LinkManager : ILinkManager
{
    #region Native Methods and Constants

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

    private const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
    private const uint FSCTL_GET_REPARSE_POINT = 0x000900A8;
    private const uint FSCTL_DELETE_REPARSE_POINT = 0x000900AC;

    private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    private const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

    private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
    private const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        int dwFlags);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFileAttributes(string lpFileName);

    [StructLayout(LayoutKind.Sequential)]
    private struct REPARSE_DATA_BUFFER
    {
        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        // For symlinks, there's a Flags field here
        // PathBuffer follows
    }

    #endregion

    /// <inheritdoc />
    /// <remarks>
    /// Checks if the path is on an NTFS file system which supports junctions.
    /// Requirement 4.6: Check link support
    /// </remarks>
    public bool IsJunctionSupported(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            var driveInfo = new DriveInfo(root);
            return driveInfo.IsReady && 
                   driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Checks if the current process has privileges to create symbolic links.
    /// Requirement 4.6: Check link support
    /// </remarks>
    public bool IsSymbolicLinkSupported()
    {
        try
        {
            // Check if running as administrator
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                return true;
            }

            // Windows 10 Creator's Update and later allows unprivileged symlink creation
            // if Developer Mode is enabled
            return IsDevModeEnabled();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if Windows Developer Mode is enabled.
    /// </summary>
    private static bool IsDevModeEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            if (key != null)
            {
                var value = key.GetValue("AllowDevelopmentWithoutDevLicense");
                return value is int intValue && intValue == 1;
            }
        }
        catch
        {
            // Registry access failed
        }
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a junction (directory hard link) using DeviceIoControl.
    /// Requirement 4.4: Create junctions
    /// </remarks>
    public Task<LinkResult> CreateJunctionAsync(string linkPath, string targetPath)
    {
        return Task.Run(() =>
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(linkPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = "链接路径不能为空",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.Junction
                    };
                }

                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = "目标路径不能为空",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.Junction
                    };
                }

                // Check if target exists
                if (!Directory.Exists(targetPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = $"目标目录不存在: {targetPath}",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.Junction
                    };
                }

                // Check if link path already exists
                if (Directory.Exists(linkPath) || File.Exists(linkPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = $"链接路径已存在: {linkPath}",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.Junction
                    };
                }

                // Check NTFS support
                if (!IsJunctionSupported(linkPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = "目标文件系统不支持 Junction",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.Junction
                    };
                }

                // Create the junction directory
                Directory.CreateDirectory(linkPath);

                // Create the junction using DeviceIoControl
                var result = CreateJunctionInternal(linkPath, targetPath);
                
                if (!result.Success)
                {
                    // Clean up the directory if junction creation failed
                    try { Directory.Delete(linkPath); } catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new LinkResult
                {
                    Success = false,
                    ErrorMessage = $"创建 Junction 失败: {ex.Message}",
                    LinkPath = linkPath,
                    TargetPath = targetPath,
                    LinkType = LinkType.Junction
                };
            }
        });
    }

    /// <summary>
    /// Internal method to create a junction using P/Invoke.
    /// </summary>
    private LinkResult CreateJunctionInternal(string linkPath, string targetPath)
    {
        // Normalize target path to NT path format
        var normalizedTarget = targetPath;
        if (!normalizedTarget.StartsWith(@"\??\"))
        {
            normalizedTarget = @"\??\" + Path.GetFullPath(targetPath);
        }

        // Calculate buffer size
        var targetBytes = Encoding.Unicode.GetBytes(normalizedTarget);
        var printNameBytes = Encoding.Unicode.GetBytes(targetPath);
        
        // REPARSE_DATA_BUFFER structure size
        var reparseDataLength = (ushort)(8 + targetBytes.Length + 2 + printNameBytes.Length + 2);
        var bufferSize = 8 + reparseDataLength; // Header (8 bytes) + data

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            // Zero out the buffer
            for (int i = 0; i < bufferSize; i++)
            {
                Marshal.WriteByte(buffer, i, 0);
            }

            // Fill in the REPARSE_DATA_BUFFER
            Marshal.WriteInt32(buffer, 0, unchecked((int)IO_REPARSE_TAG_MOUNT_POINT)); // ReparseTag
            Marshal.WriteInt16(buffer, 4, (short)reparseDataLength); // ReparseDataLength
            Marshal.WriteInt16(buffer, 6, 0); // Reserved

            // Mount point specific data
            Marshal.WriteInt16(buffer, 8, 0); // SubstituteNameOffset
            Marshal.WriteInt16(buffer, 10, (short)targetBytes.Length); // SubstituteNameLength
            Marshal.WriteInt16(buffer, 12, (short)(targetBytes.Length + 2)); // PrintNameOffset
            Marshal.WriteInt16(buffer, 14, (short)printNameBytes.Length); // PrintNameLength

            // Copy the path strings
            Marshal.Copy(targetBytes, 0, buffer + 16, targetBytes.Length);
            Marshal.Copy(printNameBytes, 0, buffer + 16 + targetBytes.Length + 2, printNameBytes.Length);

            // Open the directory with reparse point access
            using var handle = CreateFile(
                linkPath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                return new LinkResult
                {
                    Success = false,
                    ErrorMessage = $"无法打开目录: {new Win32Exception(error).Message}",
                    LinkPath = linkPath,
                    TargetPath = targetPath,
                    LinkType = LinkType.Junction
                };
            }

            // Set the reparse point
            if (!DeviceIoControl(handle, FSCTL_SET_REPARSE_POINT, buffer, (uint)bufferSize,
                IntPtr.Zero, 0, out _, IntPtr.Zero))
            {
                var error = Marshal.GetLastWin32Error();
                return new LinkResult
                {
                    Success = false,
                    ErrorMessage = $"设置重解析点失败: {new Win32Exception(error).Message}",
                    LinkPath = linkPath,
                    TargetPath = targetPath,
                    LinkType = LinkType.Junction
                };
            }

            return new LinkResult
            {
                Success = true,
                LinkPath = linkPath,
                TargetPath = targetPath,
                LinkType = LinkType.Junction
            };
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a symbolic link using CreateSymbolicLink API.
    /// Requirement 4.4: Create symbolic links
    /// </remarks>
    public Task<LinkResult> CreateSymbolicLinkAsync(string linkPath, string targetPath)
    {
        return Task.Run(() =>
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(linkPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = "链接路径不能为空",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.SymbolicLink
                    };
                }

                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = "目标路径不能为空",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.SymbolicLink
                    };
                }

                // Check if target exists
                var isDirectory = Directory.Exists(targetPath);
                var isFile = File.Exists(targetPath);
                
                if (!isDirectory && !isFile)
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = $"目标不存在: {targetPath}",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.SymbolicLink
                    };
                }

                // Check if link path already exists
                if (Directory.Exists(linkPath) || File.Exists(linkPath))
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = $"链接路径已存在: {linkPath}",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.SymbolicLink
                    };
                }

                // Check privileges
                if (!IsSymbolicLinkSupported())
                {
                    return new LinkResult
                    {
                        Success = false,
                        ErrorMessage = "创建符号链接需要管理员权限或启用开发者模式",
                        LinkPath = linkPath,
                        TargetPath = targetPath,
                        LinkType = LinkType.SymbolicLink
                    };
                }

                // Determine flags
                var flags = isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : 0;
                
                // Try with unprivileged flag first (Windows 10+)
                if (!CreateSymbolicLink(linkPath, targetPath, flags | SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE))
                {
                    // Fall back to privileged creation
                    if (!CreateSymbolicLink(linkPath, targetPath, flags))
                    {
                        var error = Marshal.GetLastWin32Error();
                        return new LinkResult
                        {
                            Success = false,
                            ErrorMessage = $"创建符号链接失败: {new Win32Exception(error).Message}",
                            LinkPath = linkPath,
                            TargetPath = targetPath,
                            LinkType = LinkType.SymbolicLink
                        };
                    }
                }

                return new LinkResult
                {
                    Success = true,
                    LinkPath = linkPath,
                    TargetPath = targetPath,
                    LinkType = LinkType.SymbolicLink
                };
            }
            catch (Exception ex)
            {
                return new LinkResult
                {
                    Success = false,
                    ErrorMessage = $"创建符号链接失败: {ex.Message}",
                    LinkPath = linkPath,
                    TargetPath = targetPath,
                    LinkType = LinkType.SymbolicLink
                };
            }
        });
    }

    /// <inheritdoc />
    /// <remarks>
    /// Removes a junction or symbolic link.
    /// Requirement 4.4: Remove links
    /// </remarks>
    public Task<bool> RemoveLinkAsync(string linkPath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(linkPath))
                {
                    return false;
                }

                var linkInfo = GetLinkInfo(linkPath);
                if (linkInfo == null)
                {
                    return false; // Not a link
                }

                // For junctions and directory symlinks, use Directory.Delete
                // For file symlinks, use File.Delete
                if (Directory.Exists(linkPath))
                {
                    // Remove the reparse point first, then delete the directory
                    RemoveReparsePoint(linkPath);
                    Directory.Delete(linkPath, false);
                    return true;
                }
                else if (File.Exists(linkPath))
                {
                    File.Delete(linkPath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Removes the reparse point from a directory.
    /// </summary>
    private static void RemoveReparsePoint(string path)
    {
        using var handle = CreateFile(
            path,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            return;
        }

        // Create a minimal REPARSE_DATA_BUFFER for deletion
        var bufferSize = 8;
        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            Marshal.WriteInt32(buffer, 0, unchecked((int)IO_REPARSE_TAG_MOUNT_POINT));
            Marshal.WriteInt32(buffer, 4, 0);

            DeviceIoControl(handle, FSCTL_DELETE_REPARSE_POINT, buffer, (uint)bufferSize,
                IntPtr.Zero, 0, out _, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Gets information about a link at the specified path.
    /// Requirement 4.4: Read link information
    /// </remarks>
    public LinkInfo? GetLinkInfo(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            // Check if it's a reparse point
            var attributes = GetFileAttributes(path);
            if (attributes == 0xFFFFFFFF) // INVALID_FILE_ATTRIBUTES
            {
                return null;
            }

            if ((attributes & FILE_ATTRIBUTE_REPARSE_POINT) == 0)
            {
                return null; // Not a reparse point
            }

            // Open the reparse point to read its data
            using var handle = CreateFile(
                path,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return null;
            }

            // Allocate buffer for reparse data
            const int bufferSize = 16384;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                if (!DeviceIoControl(handle, FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0,
                    buffer, bufferSize, out var bytesReturned, IntPtr.Zero))
                {
                    return null;
                }

                // Read the reparse tag
                var reparseTag = (uint)Marshal.ReadInt32(buffer, 0);
                
                LinkType linkType;
                string targetPath;

                if (reparseTag == IO_REPARSE_TAG_MOUNT_POINT)
                {
                    linkType = LinkType.Junction;
                    targetPath = ReadMountPointTarget(buffer);
                }
                else if (reparseTag == IO_REPARSE_TAG_SYMLINK)
                {
                    linkType = LinkType.SymbolicLink;
                    targetPath = ReadSymlinkTarget(buffer);
                }
                else
                {
                    return null; // Unknown reparse point type
                }

                // Clean up the target path
                if (targetPath.StartsWith(@"\??\"))
                {
                    targetPath = targetPath.Substring(4);
                }

                return new LinkInfo
                {
                    LinkPath = path,
                    TargetPath = targetPath,
                    LinkType = linkType,
                    TargetExists = Directory.Exists(targetPath) || File.Exists(targetPath)
                };
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the target path from a mount point (junction) reparse buffer.
    /// </summary>
    private static string ReadMountPointTarget(IntPtr buffer)
    {
        var substituteNameOffset = Marshal.ReadInt16(buffer, 8);
        var substituteNameLength = Marshal.ReadInt16(buffer, 10);
        
        var pathBuffer = buffer + 16 + substituteNameOffset;
        return Marshal.PtrToStringUni(pathBuffer, substituteNameLength / 2) ?? string.Empty;
    }

    /// <summary>
    /// Reads the target path from a symbolic link reparse buffer.
    /// </summary>
    private static string ReadSymlinkTarget(IntPtr buffer)
    {
        var substituteNameOffset = Marshal.ReadInt16(buffer, 8);
        var substituteNameLength = Marshal.ReadInt16(buffer, 10);
        
        // Symlink has an extra Flags field (4 bytes) before the path buffer
        var pathBuffer = buffer + 20 + substituteNameOffset;
        return Marshal.PtrToStringUni(pathBuffer, substituteNameLength / 2) ?? string.Empty;
    }
}
