using System.Windows.Controls;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class SalesView : UserControl
{
    // Exposed so MainWindow can call Refresh() whenever it navigates back here.
    public SalesViewModel ViewModel { get; }

    public SalesView()
    {
        InitializeComponent();
        ViewModel = new SalesViewModel();
        DataContext = ViewModel;
    }
}
