using System.Windows.Controls;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class TransactionsView : UserControl
{
    public TransactionsView()
    {
        InitializeComponent();
        DataContext = new TransactionViewModel();
    }
}
