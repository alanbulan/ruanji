using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.ViewModels;

namespace WindowsSoftwareOrganizer.Views;

/// <summary>
/// Page for application settings.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Current.GetService<SettingsViewModel>();
        this.InitializeComponent();
        
        // Set initial selections based on ViewModel
        SetInitialSelections();
    }

    private void SetInitialSelections()
    {
        // Set theme selection
        var themeIndex = ViewModel.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };
        ThemeComboBox.SelectedIndex = themeIndex;

        // Set link type selection
        LinkTypeComboBox.SelectedIndex = ViewModel.PreferredLinkType == LinkType.SymbolicLink ? 1 : 0;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var theme = tag switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            ViewModel.SetThemeCommand.Execute(theme);
        }
    }

    private void LinkTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LinkTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.PreferredLinkType = tag == "SymbolicLink" ? LinkType.SymbolicLink : LinkType.Junction;
        }
    }
}
