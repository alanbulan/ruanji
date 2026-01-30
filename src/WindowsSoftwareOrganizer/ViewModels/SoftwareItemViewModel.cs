using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Helpers;

namespace WindowsSoftwareOrganizer.ViewModels;

/// <summary>
/// ViewModel wrapper for SoftwareEntry with icon support.
/// </summary>
public partial class SoftwareItemViewModel : ObservableObject
{
    public SoftwareEntry Entry { get; }

    [ObservableProperty]
    private ImageSource? _icon;

    [ObservableProperty]
    private bool _isIconLoaded;

    public string Name => Entry.Name;
    public string? Version => Entry.Version;
    public string? Vendor => Entry.Vendor;
    public string InstallPath => Entry.InstallPath;
    public SoftwareCategory Category => Entry.Category;
    public long TotalSizeBytes => Entry.TotalSizeBytes;
    public string? IconPath => Entry.IconPath;
    public string Id => Entry.Id;

    public SoftwareItemViewModel(SoftwareEntry entry)
    {
        Entry = entry;
    }

    /// <summary>
    /// Loads the icon asynchronously.
    /// </summary>
    public async Task LoadIconAsync()
    {
        if (IsIconLoaded || string.IsNullOrEmpty(Entry.IconPath))
            return;

        try
        {
            Icon = await IconExtractor.ExtractIconAsync(Entry.IconPath);
        }
        catch
        {
            // Icon extraction failed, leave as null
        }
        finally
        {
            IsIconLoaded = true;
        }
    }
}
