using System.Windows;
using System.Windows.Controls;
using RetailFlow.Models;
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
    private readonly AssistantView _assistantView = new();

    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = _dashboardView;

        // The assistant never touches WPF controls itself (see AssistantQueryService) —
        // it raises this event, and MainWindow is what actually performs the navigation
        // and applies whatever filter came with it. That keeps navigation responsibility
        // in the application layer, not the interpretation layer.
        _assistantView.ViewModel.NavigationRequested += OnAssistantNavigationRequested;

        // The Dashboard's "View..." buttons use the same pattern.
        _dashboardView.ViewModel.NavigationRequested += OnDashboardNavigationRequested;
    }

    // --- Plain sidebar navigation: switches screens only, never touches a screen's own
    // search/filter state. A user browsing Products, stepping away, and coming back
    // should still see whatever they were searching for. ---

    private void BtnDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void BtnProducts_Click(object sender, RoutedEventArgs e) => ShowProducts();
    private void BtnSales_Click(object sender, RoutedEventArgs e) => ShowSales();
    private void BtnTransactions_Click(object sender, RoutedEventArgs e) => ShowTransactions();

    private void BtnAssistant_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = _assistantView;
        PageTitle.Text = "MiniMart Assistant";
        SetActiveButton(BtnAssistant);
    }

    private void ShowDashboard()
    {
        MainContent.Content = _dashboardView;
        PageTitle.Text = "Dashboard";
        SetActiveButton(BtnDashboard);

        // The Dashboard view is created once and reused, so its numbers would otherwise
        // still reflect whatever they were the last time it was shown — refreshing on
        // every visit is what keeps "Today's Sales" etc. actually current.
        _dashboardView.ViewModel.Refresh();
    }

    private void ShowProducts()
    {
        MainContent.Content = _productsView;
        PageTitle.Text = "Product Management";
        SetActiveButton(BtnProducts);

        // These views are created once and reused; refreshing on every visit (not just
        // when a search box happens to change) is what keeps stock/sales numbers from
        // another screen showing up here instead of a stale startup-time snapshot.
        _productsView.ViewModel.Refresh();
    }

    private void ShowSales()
    {
        MainContent.Content = _salesView;
        PageTitle.Text = "New Sale";
        SetActiveButton(BtnSales);

        _salesView.ViewModel.Refresh();
    }

    private void ShowTransactions()
    {
        MainContent.Content = _transactionsView;
        PageTitle.Text = "Transaction History";
        SetActiveButton(BtnTransactions);

        _transactionsView.ViewModel.Refresh();
    }

    // --- Assistant-driven navigation: unlike the plain sidebar clicks above, this is
    // expressing a complete, fresh request from the user ("find tea", "low stock
    // products"), so it deliberately DOES set the destination screen's search/filter
    // state to match what was asked. ---

    private void OnAssistantNavigationRequested(object? sender, AssistantResponse response)
    {
        switch (response.NavigationTarget)
        {
            case "Dashboard":
                ShowDashboard();
                break;

            case "Products":
                ShowProducts();
                _productsView.ViewModel.SearchText = response.SearchTerm ?? string.Empty;
                _productsView.ViewModel.LowStockOnly = response.LowStockOnly;
                break;

            case "Sales":
                ShowSales();
                break;

            case "Transactions":
                ShowTransactions();
                _transactionsView.ViewModel.SearchText = response.SearchTerm ?? string.Empty;
                _transactionsView.ViewModel.FromDate = response.StartDate;
                _transactionsView.ViewModel.ToDate = response.EndDate;
                break;
        }
    }

    private void OnDashboardNavigationRequested(object? sender, (string Target, bool LowStockOnly) request)
    {
        switch (request.Target)
        {
            case "Products":
                ShowProducts();
                _productsView.ViewModel.SearchText = string.Empty;
                _productsView.ViewModel.LowStockOnly = request.LowStockOnly;
                break;

            case "Transactions":
                ShowTransactions();
                break;
        }
    }

    /// <summary>
    /// Highlights the selected nav button and resets the others to their normal style,
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
        BtnAssistant.Style = normalStyle;

        active.Style = activeStyle;
    }
}
