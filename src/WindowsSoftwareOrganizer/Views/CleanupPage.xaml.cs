using Microsoft.UI.Xaml.Controls;
using WindowsSoftwareOrganizer.ViewModels;

namespace WindowsSoftwareOrganizer.Views;

/// <summary>
/// Page for managing cleanup operations.
/// </summary>
public sealed partial class CleanupPage : Page
{
    public CleanupViewModel ViewModel { get; }

    public CleanupPage()
    {
        ViewModel = App.Current.GetService<CleanupViewModel>();
        this.InitializeComponent();
        
        // Initialize dispatcher for UI thread operations
        ViewModel.InitializeDispatcher();
    }

    private void CheckBox_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.UpdateSelectedSize();
    }

    private void CheckBox_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.UpdateSelectedSize();
    }
}
