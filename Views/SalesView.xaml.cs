using System.Windows.Controls;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
        DataContext = new SalesViewModel();
    }
}
