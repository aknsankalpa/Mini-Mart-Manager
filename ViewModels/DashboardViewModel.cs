using System.Collections.ObjectModel;
using System.Windows.Input;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.ViewModels;

/// <summary>
/// Drives the Dashboard: today's headline numbers plus a short list of recent
/// transactions and low-stock products. All figures come from existing services — this
/// class only holds the loaded values and reacts to the "View..." buttons. Because the
/// Dashboard view is created once and reused, Refresh() is called every time MainWindow
/// navigates back to it, so the numbers never go stale after a sale or stock change.
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly DashboardService _dashboardService = new();
    private readonly ProductService _productService = new();
    private readonly SalesService _salesService = new();

    public ObservableCollection<Sale> RecentTransactions { get; } = new();
    public ObservableCollection<Product> LowStockProducts { get; } = new();

    private int _activeProductCount;
    public int ActiveProductCount { get => _activeProductCount; private set => SetField(ref _activeProductCount, value); }

    private decimal _todaysSalesTotal;
    public decimal TodaysSalesTotal { get => _todaysSalesTotal; private set => SetField(ref _todaysSalesTotal, value); }

    private int _todaysTransactionCount;
    public int TodaysTransactionCount { get => _todaysTransactionCount; private set => SetField(ref _todaysTransactionCount, value); }

    private int _lowStockCount;
    public int LowStockCount { get => _lowStockCount; private set => SetField(ref _lowStockCount, value); }

    public bool HasRecentTransactions => RecentTransactions.Count > 0;
    public bool HasLowStockProducts => LowStockProducts.Count > 0;

    public ICommand ViewProductsCommand { get; }
    public ICommand ViewLowStockCommand { get; }
    public ICommand ViewTransactionsCommand { get; }

    /// <summary>Raised when a "View..." button should switch screens (and optionally
    /// filter to low-stock products), handled by MainWindow the same way the MiniMart
    /// Assistant's navigation requests are.</summary>
    public event EventHandler<(string Target, bool LowStockOnly)>? NavigationRequested;

    public DashboardViewModel()
    {
        ViewProductsCommand = new RelayCommand(_ => NavigationRequested?.Invoke(this, ("Products", false)));
        ViewLowStockCommand = new RelayCommand(_ => NavigationRequested?.Invoke(this, ("Products", true)));
        ViewTransactionsCommand = new RelayCommand(_ => NavigationRequested?.Invoke(this, ("Transactions", false)));

        Refresh();
    }

    public void Refresh()
    {
        // Every screen is constructed eagerly at startup (see MainWindow.xaml.cs), so an
        // uncaught exception here would take the whole application down rather than just
        // leave the Dashboard showing stale or empty figures.
        try
        {
            ActiveProductCount = _dashboardService.GetActiveProductCount();

            var today = DateTime.Today;
            var (total, count) = _dashboardService.GetSalesSummary(today, today);
            TodaysSalesTotal = total;
            TodaysTransactionCount = count;

            var lowStock = _productService.GetLowStockProducts();
            LowStockCount = lowStock.Count;

            LowStockProducts.Clear();
            foreach (var product in lowStock.Take(5))
            {
                LowStockProducts.Add(product);
            }

            RecentTransactions.Clear();
            foreach (var sale in _salesService.GetRecentSales(5))
            {
                RecentTransactions.Add(sale);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("DashboardViewModel.Refresh", ex);
            LowStockProducts.Clear();
            RecentTransactions.Clear();
        }

        OnPropertyChanged(nameof(HasLowStockProducts));
        OnPropertyChanged(nameof(HasRecentTransactions));
    }
}
