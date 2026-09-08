using System.Windows;
using System.Windows.Controls;
using RetailFlow.Views;

namespace RetailFlow;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Each view is created once and reused, so switching sections doesn't lose its state.
    private readonly DashboardView _dashboardView = new();
    private readonly ProductsView _productsView = new();
    private readonly SalesView _salesView = new();
    private readonly TransactionsView _transactionsView = new();

    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = _dashboardView;
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _dashboardView;
        PageTitle.Text = "Dashboard";
        SetActiveButton(BtnDashboard);
    }

    private void BtnProducts_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _productsView;
        PageTitle.Text = "Product Management";
        SetActiveButton(BtnProducts);
    }

    private void BtnSales_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _salesView;
        PageTitle.Text = "New Sale";
        SetActiveButton(BtnSales);
    }

    private void BtnTransactions_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _transactionsView;
        PageTitle.Text = "Transaction History";
        SetActiveButton(BtnTransactions);
    }

    /// <summary>
    /// Highlights the selected nav button and resets the other three to their normal style,
    /// so the user can always see which section is currently open.
    /// </summary>
    private void SetActiveButton(Button active)
    {
        var normalStyle = (Style)FindResource("NavButtonStyle");
        var activeStyle = (Style)FindResource("NavButtonActiveStyle");

        BtnDashboard.Style = normalStyle;
        BtnProducts.Style = normalStyle;
        BtnSales.Style = normalStyle;
        BtnTransactions.Style = normalStyle;

        active.Style = activeStyle;
    }
}
