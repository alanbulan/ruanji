using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.ViewModels;
using WindowsSoftwareOrganizer.Views;
using WinRT.Interop;

namespace WindowsSoftwareOrganizer;

/// <summary>
/// Main window of the application with Mica backdrop and custom title bar.
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;

    public MainWindow()
    {
        this.InitializeComponent();

        // Get AppWindow
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Set window title
        Title = "软迹";

        // Setup custom title bar
        SetupTitleBar();

        // Try to apply Mica backdrop
        TrySetMicaBackdrop();

        // Set window size and icon
        if (_appWindow != null)
        {
            _appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
            SetWindowIcon();
        }

        // Setup navigation
        NavView.SelectionChanged += NavView_SelectionChanged;
        ContentFrame.Navigated += ContentFrame_Navigated;
        
        // Navigate to default page
        NavView.SelectedItem = NavView.MenuItems[0];

        // Handle theme changes
        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged += (s, e) => UpdateTitleBarColors();
        }
    }

    /// <summary>
    /// Handles frame navigation to subscribe to page-specific events.
    /// </summary>
    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.Content is SoftwareListPage softwareListPage)
        {
            // Subscribe to migration request event
            softwareListPage.ViewModel.MigrationRequested += OnMigrationRequested;
        }
    }

    /// <summary>
    /// Handles migration request from software list page.
    /// </summary>
    private void OnMigrationRequested(object? sender, SoftwareEntry software)
    {
        // Navigate to migration page
        foreach (var item in NavView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Migration")
            {
                NavView.SelectedItem = navItem;
                break;
            }
        }

        // Pass the software to migration page after navigation
        if (ContentFrame.Content is MigrationPage migrationPage)
        {
            migrationPage.SetSoftwareToMigrate(software);
        }
    }

    /// <summary>
    /// Sets up the custom title bar with Windows 11 style.
    /// </summary>
    private void SetupTitleBar()
    {
        if (_appWindow == null) return;

        // Check if custom title bar is supported
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = _appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            // Set drag region
            AppTitleBar.Loaded += (s, e) =>
            {
                SetDragRegion();
            };
            AppTitleBar.SizeChanged += (s, e) =>
            {
                SetDragRegion();
            };

            // Update colors
            UpdateTitleBarColors();
        }
        else
        {
            // Fallback: hide custom title bar
            AppTitleBar.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Sets the drag region for the custom title bar.
    /// </summary>
    private void SetDragRegion()
    {
        if (_appWindow == null || !AppWindowTitleBar.IsCustomizationSupported())
            return;

        var scaleAdjustment = GetScaleAdjustment();
        var titleBar = _appWindow.TitleBar;

        // Set the interactive regions
        titleBar.SetDragRectangles(new Windows.Graphics.RectInt32[]
        {
            new Windows.Graphics.RectInt32
            {
                X = 0,
                Y = 0,
                Width = (int)(AppTitleBar.ActualWidth * scaleAdjustment),
                Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment)
            }
        });
    }

    /// <summary>
    /// Updates title bar button colors based on current theme.
    /// </summary>
    private void UpdateTitleBarColors()
    {
        if (_appWindow == null || !AppWindowTitleBar.IsCustomizationSupported())
            return;

        var titleBar = _appWindow.TitleBar;
        var theme = (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;

        // Set button colors based on theme
        if (theme == ElementTheme.Dark)
        {
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(50, 255, 255, 255);
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(128, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 0, 0, 0);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(50, 0, 0, 0);
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(128, 0, 0, 0);
        }

        // Transparent backgrounds
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }

    /// <summary>
    /// Sets the window icon from the embedded application resource.
    /// </summary>
    private void SetWindowIcon()
    {
        if (_appWindow == null) return;

        try
        {
            // Method 1: Try to load from embedded resource (ApplicationIcon in csproj)
            var iconId = GetApplicationIconId();
            if (iconId.Value != 0)
            {
                _appWindow.SetIcon(iconId);
                return;
            }

            // Method 2: Fallback to file path
            var iconPath = System.IO.Path.Combine(
                AppContext.BaseDirectory, 
                "Assets", 
                "AppIcon.ico");
            
            if (System.IO.File.Exists(iconPath))
            {
                _appWindow.SetIcon(iconPath);
            }
        }
        catch
        {
            // Icon setting failed, continue without icon
        }
    }

    /// <summary>
    /// Gets the application icon ID from the embedded resource.
    /// </summary>
    private static IconId GetApplicationIconId()
    {
        // Application resource ID assigned by Visual Studio to .NET applications
        // https://devblogs.microsoft.com/oldnewthing/20250423-00/?p=111106
        IntPtr iconResourceId = new(32512);

        IntPtr hModule = NativeMethods.GetModuleHandle(null);
        if (hModule == IntPtr.Zero)
        {
            return default;
        }

        IntPtr hIcon = NativeMethods.LoadIcon(hModule, iconResourceId);
        if (hIcon == IntPtr.Zero)
        {
            return default;
        }

        return Win32Interop.GetIconIdFromIcon(hIcon);
    }

    /// <summary>
    /// Gets the scale adjustment for high DPI displays.
    /// </summary>
    private double GetScaleAdjustment()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        // Get DPI
        int result = NativeMethods.GetDpiForMonitor(hMonitor, MonitorDpiType.MDT_Default, out uint dpiX, out uint _);
        if (result != 0)
        {
            return 1.0;
        }

        return dpiX / 96.0;
    }

    /// <summary>
    /// Attempts to set the Mica backdrop for the window.
    /// </summary>
    private bool TrySetMicaBackdrop()
    {
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
            return true;
        }

        return false;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "SoftwareList":
                    ContentFrame.Navigate(typeof(SoftwareListPage));
                    break;
                case "FileManager":
                    ContentFrame.Navigate(typeof(FileManagerPage));
                    break;
                case "Migration":
                    ContentFrame.Navigate(typeof(MigrationPage));
                    break;
                case "Cleanup":
                    ContentFrame.Navigate(typeof(CleanupPage));
                    break;
            }
        }
    }

    private enum MonitorDpiType
    {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    /// <summary>
    /// Native methods for Win32 API calls.
    /// </summary>
    private static class NativeMethods
    {
        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", EntryPoint = "LoadIconW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr LoadIcon(IntPtr hModule, IntPtr lpIconName);

        [DllImport("Shcore.dll", SetLastError = true)]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);
    }
}
