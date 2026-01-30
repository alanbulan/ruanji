using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;

namespace WindowsSoftwareOrganizer.Helpers;

/// <summary>
/// Helper class for extracting icons from executable files using Windows Shell API.
/// Uses WriteableBitmap for WinUI 3 compatibility.
/// Implements multiple fallback methods for maximum icon extraction success.
/// </summary>
public static class IconExtractor
{
    private static readonly Dictionary<string, WriteableBitmap?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    #region Win32 API Declarations

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(
        string lpszFile,
        int nIconIndex,
        IntPtr[] phiconLarge,
        IntPtr[] phiconSmall,
        uint nIcons);

    // PrivateExtractIcons - Can extract larger icons (up to 256x256)
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint PrivateExtractIcons(
        string szFileName,
        int nIconIndex,
        int cxIcon,
        int cyIcon,
        IntPtr[] phicon,
        IntPtr[] piconid,
        uint nIcons,
        uint flags);

    // SHGetImageList - Gets icons from system image list (supports jumbo 256x256)
    [DllImport("shell32.dll", EntryPoint = "#727")]
    private static extern int SHGetImageList(
        int iImageList,
        ref Guid riid,
        out IImageList ppv);

    // LoadImage - Load .ico files directly
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(
        IntPtr hInst,
        string lpszName,
        uint uType,
        int cxDesired,
        int cyDesired,
        uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(
        IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines,
        byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr hObject, int nCount, ref BITMAP lpObject);

    // IImageList COM interface for SHGetImageList
    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IImageList
    {
        [PreserveSig]
        int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
        [PreserveSig]
        int ReplaceIcon(int i, IntPtr hicon, ref int pi);
        [PreserveSig]
        int SetOverlayImage(int iImage, int iOverlay);
        [PreserveSig]
        int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
        [PreserveSig]
        int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
        [PreserveSig]
        int Draw(ref IMAGELISTDRAWPARAMS pimldp);
        [PreserveSig]
        int Remove(int i);
        [PreserveSig]
        int GetIcon(int i, int flags, ref IntPtr picon);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGELISTDRAWPARAMS
    {
        public int cbSize;
        public IntPtr himl;
        public int i;
        public IntPtr hdcDst;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int xBitmap;
        public int yBitmap;
        public int rgbBk;
        public int rgbFg;
        public int fStyle;
        public int dwRop;
        public int fState;
        public int Frame;
        public int crEffect;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public uint[] bmiColors;
    }

    // SHGetFileInfo flags
    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_SYSICONINDEX = 0x000004000;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    // LoadImage constants
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    // SHGetImageList constants
    private const int SHIL_LARGE = 0x0;      // 32x32
    private const int SHIL_SMALL = 0x1;      // 16x16
    private const int SHIL_EXTRALARGE = 0x2; // 48x48
    private const int SHIL_SYSSMALL = 0x3;   // System small
    private const int SHIL_JUMBO = 0x4;      // 256x256

    // IImageList GUID
    private static readonly Guid IID_IImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");

    #endregion

    /// <summary>
    /// Extracts an icon from an executable file and returns it as a WriteableBitmap.
    /// Uses Windows Shell API for reliable icon extraction.
    /// </summary>
    public static async Task<WriteableBitmap?> ExtractIconAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        // Check cache first
        await _cacheLock.WaitAsync();
        try
        {
            if (_iconCache.TryGetValue(filePath, out var cachedIcon))
            {
                return cachedIcon;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        WriteableBitmap? bitmap = null;

        try
        {
            if (!File.Exists(filePath))
                return null;

            // Extract icon data on background thread
            var iconData = await Task.Run(() => ExtractIconData(filePath));
            
            if (iconData != null)
            {
                // Create WriteableBitmap on UI thread
                bitmap = CreateWriteableBitmap(iconData.Value.width, iconData.Value.height, iconData.Value.pixels);
            }
        }
        catch
        {
            bitmap = null;
        }

        // Cache the result
        await _cacheLock.WaitAsync();
        try
        {
            _iconCache[filePath] = bitmap;
        }
        finally
        {
            _cacheLock.Release();
        }

        return bitmap;
    }

    /// <summary>
    /// Extracts icon pixel data from file using multiple fallback methods.
    /// Priority: PrivateExtractIcons (256x256) → SHGetImageList (Jumbo) → ExtractIconEx → SHGetFileInfo → .ico files
    /// </summary>
    private static (int width, int height, byte[] pixels)? ExtractIconData(string filePath)
    {
        // Method 1: Try PrivateExtractIcons for large icons (256x256, 128x128, 64x64, 48x48, 32x32)
        var result = TryPrivateExtractIcons(filePath, 256);
        if (result != null) return result;

        result = TryPrivateExtractIcons(filePath, 128);
        if (result != null) return result;

        result = TryPrivateExtractIcons(filePath, 64);
        if (result != null) return result;

        result = TryPrivateExtractIcons(filePath, 48);
        if (result != null) return result;

        // Method 2: Try SHGetImageList with Jumbo (256x256) or ExtraLarge (48x48)
        result = TrySHGetImageList(filePath, SHIL_JUMBO);
        if (result != null) return result;

        result = TrySHGetImageList(filePath, SHIL_EXTRALARGE);
        if (result != null) return result;

        // Method 3: Try ExtractIconEx for large icon (32x32)
        result = TryExtractIconEx(filePath);
        if (result != null) return result;

        // Method 4: Fallback to SHGetFileInfo
        result = TrySHGetFileInfo(filePath);
        if (result != null) return result;

        // Method 5: Search for .ico files in the same directory
        result = TryLoadIcoFile(filePath);
        if (result != null) return result;

        return null;
    }

    /// <summary>
    /// Try extracting icon using PrivateExtractIcons (supports up to 256x256).
    /// </summary>
    private static (int width, int height, byte[] pixels)? TryPrivateExtractIcons(string filePath, int size)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            var icons = new IntPtr[1];
            var iconIds = new IntPtr[1];
            
            uint count = PrivateExtractIcons(filePath, 0, size, size, icons, iconIds, 1, 0);
            
            if (count > 0 && icons[0] != IntPtr.Zero)
            {
                hIcon = icons[0];
                return ConvertIconToPixels(hIcon);
            }
        }
        catch
        {
            // Ignore and try next method
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                DestroyIcon(hIcon);
        }
        return null;
    }

    /// <summary>
    /// Try extracting icon using SHGetImageList (system image list with jumbo support).
    /// </summary>
    private static (int width, int height, byte[] pixels)? TrySHGetImageList(string filePath, int imageListType)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            // Get the icon index from SHGetFileInfo
            var shfi = new SHFILEINFO();
            var result = SHGetFileInfo(
                filePath,
                FILE_ATTRIBUTE_NORMAL,
                ref shfi,
                (uint)Marshal.SizeOf(shfi),
                SHGFI_SYSICONINDEX);

            if (result == IntPtr.Zero)
                return null;

            int iconIndex = shfi.iIcon;

            // Get the image list
            var guid = IID_IImageList;
            int hr = SHGetImageList(imageListType, ref guid, out IImageList imageList);
            
            if (hr != 0 || imageList == null)
                return null;

            // Get the icon from the image list
            hIcon = IntPtr.Zero;
            hr = imageList.GetIcon(iconIndex, 0, ref hIcon);
            
            if (hr == 0 && hIcon != IntPtr.Zero)
            {
                return ConvertIconToPixels(hIcon);
            }
        }
        catch
        {
            // Ignore and try next method
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                DestroyIcon(hIcon);
        }
        return null;
    }

    /// <summary>
    /// Try extracting icon using ExtractIconEx.
    /// </summary>
    private static (int width, int height, byte[] pixels)? TryExtractIconEx(string filePath)
    {
        IntPtr hIcon = IntPtr.Zero;
        IntPtr hSmallIcon = IntPtr.Zero;
        try
        {
            var largeIcons = new IntPtr[1];
            var smallIcons = new IntPtr[1];
            
            uint count = ExtractIconEx(filePath, 0, largeIcons, smallIcons, 1);
            if (count > 0 && largeIcons[0] != IntPtr.Zero)
            {
                hIcon = largeIcons[0];
                hSmallIcon = smallIcons[0];
                return ConvertIconToPixels(hIcon);
            }
        }
        catch
        {
            // Ignore and try next method
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                DestroyIcon(hIcon);
            if (hSmallIcon != IntPtr.Zero)
                DestroyIcon(hSmallIcon);
        }
        return null;
    }

    /// <summary>
    /// Try extracting icon using SHGetFileInfo.
    /// </summary>
    private static (int width, int height, byte[] pixels)? TrySHGetFileInfo(string filePath)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            var shfi = new SHFILEINFO();
            var result = SHGetFileInfo(
                filePath,
                FILE_ATTRIBUTE_NORMAL,
                ref shfi,
                (uint)Marshal.SizeOf(shfi),
                SHGFI_ICON | SHGFI_LARGEICON);

            if (result != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                hIcon = shfi.hIcon;
                return ConvertIconToPixels(hIcon);
            }
        }
        catch
        {
            // Ignore and try next method
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                DestroyIcon(hIcon);
        }
        return null;
    }

    /// <summary>
    /// Try loading .ico file from the same directory or common icon locations.
    /// </summary>
    private static (int width, int height, byte[] pixels)? TryLoadIcoFile(string filePath)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                return null;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            // Common icon file patterns to search
            var iconPatterns = new[]
            {
                $"{fileNameWithoutExt}.ico",
                "app.ico",
                "icon.ico",
                "main.ico",
                "logo.ico",
                $"{fileNameWithoutExt}_icon.ico",
                "favicon.ico"
            };

            // Search in current directory and common subdirectories
            var searchDirs = new List<string> { directory };
            
            var subDirs = new[] { "icons", "icon", "resources", "res", "assets", "Images" };
            foreach (var subDir in subDirs)
            {
                string subPath = Path.Combine(directory, subDir);
                if (Directory.Exists(subPath))
                    searchDirs.Add(subPath);
            }

            foreach (var searchDir in searchDirs)
            {
                foreach (var pattern in iconPatterns)
                {
                    string icoPath = Path.Combine(searchDir, pattern);
                    if (File.Exists(icoPath))
                    {
                        hIcon = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, 256, 256, LR_LOADFROMFILE);
                        if (hIcon == IntPtr.Zero)
                            hIcon = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
                        
                        if (hIcon != IntPtr.Zero)
                        {
                            var result = ConvertIconToPixels(hIcon);
                            if (result != null)
                                return result;
                        }
                    }
                }

                // Also search for any .ico file in the directory
                try
                {
                    var icoFiles = Directory.GetFiles(searchDir, "*.ico", SearchOption.TopDirectoryOnly);
                    foreach (var icoFile in icoFiles.Take(5)) // Limit to first 5 ico files
                    {
                        hIcon = LoadImage(IntPtr.Zero, icoFile, IMAGE_ICON, 256, 256, LR_LOADFROMFILE);
                        if (hIcon == IntPtr.Zero)
                            hIcon = LoadImage(IntPtr.Zero, icoFile, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
                        
                        if (hIcon != IntPtr.Zero)
                        {
                            var result = ConvertIconToPixels(hIcon);
                            if (result != null)
                                return result;
                            
                            DestroyIcon(hIcon);
                            hIcon = IntPtr.Zero;
                        }
                    }
                }
                catch
                {
                    // Ignore directory access errors
                }
            }
        }
        catch
        {
            // Ignore and return null
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                DestroyIcon(hIcon);
        }
        return null;
    }

    /// <summary>
    /// Converts an HICON to BGRA pixel data.
    /// </summary>
    private static (int width, int height, byte[] pixels)? ConvertIconToPixels(IntPtr hIcon)
    {
        if (hIcon == IntPtr.Zero)
            return null;

        try
        {
            if (GetIconInfo(hIcon, out ICONINFO iconInfo) == 0)
                return null;

            try
            {
                IntPtr hBitmap = iconInfo.hbmColor != IntPtr.Zero ? iconInfo.hbmColor : iconInfo.hbmMask;
                
                if (hBitmap == IntPtr.Zero)
                    return null;

                var bmp = new BITMAP();
                GetObject(hBitmap, Marshal.SizeOf(typeof(BITMAP)), ref bmp);

                int width = bmp.bmWidth;
                int height = bmp.bmHeight;
                
                if (width <= 0 || height <= 0)
                    return null;

                // Get color bitmap data with top-down orientation
                var bmi = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                        biWidth = width,
                        biHeight = -height, // Negative for top-down
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0 // BI_RGB
                    },
                    bmiColors = new uint[256]
                };

                var colorPixels = new byte[width * height * 4];
                IntPtr hdc = GetDC(IntPtr.Zero);
                
                try
                {
                    GetDIBits(hdc, hBitmap, 0, (uint)height, colorPixels, ref bmi, 0);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }

                // Get mask bitmap data if available
                byte[]? maskPixels = null;
                if (iconInfo.hbmMask != IntPtr.Zero && iconInfo.hbmColor != IntPtr.Zero)
                {
                    maskPixels = new byte[width * height * 4];
                    hdc = GetDC(IntPtr.Zero);
                    try
                    {
                        var maskBmi = new BITMAPINFO
                        {
                            bmiHeader = new BITMAPINFOHEADER
                            {
                                biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                                biWidth = width,
                                biHeight = -height,
                                biPlanes = 1,
                                biBitCount = 32,
                                biCompression = 0
                            },
                            bmiColors = new uint[256]
                        };
                        GetDIBits(hdc, iconInfo.hbmMask, 0, (uint)height, maskPixels, ref maskBmi, 0);
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, hdc);
                    }
                }

                // Check if color bitmap has valid alpha channel
                bool hasAlpha = false;
                for (int i = 3; i < colorPixels.Length; i += 4)
                {
                    if (colorPixels[i] != 0)
                    {
                        hasAlpha = true;
                        break;
                    }
                }

                // If no alpha in color bitmap, derive from mask
                if (!hasAlpha && maskPixels != null)
                {
                    for (int i = 0; i < colorPixels.Length; i += 4)
                    {
                        // Mask pixel: white (255) = transparent, black (0) = opaque
                        byte maskValue = maskPixels[i];
                        colorPixels[i + 3] = (byte)(255 - maskValue);
                    }
                }
                else if (!hasAlpha)
                {
                    // No mask available, set all pixels to opaque
                    for (int i = 3; i < colorPixels.Length; i += 4)
                    {
                        colorPixels[i] = 255;
                    }
                }

                return (width, height, colorPixels);
            }
            finally
            {
                if (iconInfo.hbmColor != IntPtr.Zero)
                    DeleteObject(iconInfo.hbmColor);
                if (iconInfo.hbmMask != IntPtr.Zero)
                    DeleteObject(iconInfo.hbmMask);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a WriteableBitmap from BGRA pixel data.
    /// Must be called on UI thread.
    /// </summary>
    private static WriteableBitmap CreateWriteableBitmap(int width, int height, byte[] bgraPixels)
    {
        var bitmap = new WriteableBitmap(width, height);
        
        using (var stream = bitmap.PixelBuffer.AsStream())
        {
            stream.Write(bgraPixels, 0, bgraPixels.Length);
        }
        
        bitmap.Invalidate();
        return bitmap;
    }

    /// <summary>
    /// Clears the icon cache.
    /// </summary>
    public static async Task ClearCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            _iconCache.Clear();
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
