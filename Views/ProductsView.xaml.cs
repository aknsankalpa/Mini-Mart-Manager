using System.Windows.Controls;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class ProductsView : UserControl
{
    // Exposed so the MiniMart Assistant (via MainWindow) can apply a search/filter to
    // this screen after navigating to it, reusing the same properties the UI itself binds to.
    public ProductViewModel ViewModel { get; }

    public ProductsView()
    {
        InitializeComponent();
        ViewModel = new ProductViewModel();
        DataContext = ViewModel;
    }
}
