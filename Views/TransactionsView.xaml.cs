using System.Windows.Controls;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class TransactionsView : UserControl
{
    // Exposed so the MiniMart Assistant (via MainWindow) can apply a date/search filter
    // to this screen after navigating to it.
    public TransactionViewModel ViewModel { get; }

    public TransactionsView()
    {
        InitializeComponent();
        ViewModel = new TransactionViewModel();
        DataContext = ViewModel;
    }
}
