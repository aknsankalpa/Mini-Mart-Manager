using System.Windows.Controls;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class DashboardView : UserControl
{
    // Exposed so MainWindow can call Refresh() whenever it navigates back here, and can
    // subscribe to NavigationRequested for the "View..." buttons.
    public DashboardViewModel ViewModel { get; }

    public DashboardView()
    {
        InitializeComponent();
        ViewModel = new DashboardViewModel();
        DataContext = ViewModel;
    }
}
