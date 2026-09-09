using System.Collections.ObjectModel;
using System.Windows.Input;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.ViewModels;

/// <summary>
/// One entry in the Product slicer dropdown. A plain wrapper (rather than binding directly
/// to Product) so an "All Products" option can sit at the top without a fake Product row.
/// </summary>
public class ProductFilterOption
{
    public int? ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public override string ToString() => Name;
}

/// <summary>
/// Drives the Power BI-style Dashboard: four KPI cards, five charts, and the Date/Category/
/// Product slicers that filter all of them together. Every figure comes from
/// DashboardService's async aggregate queries — this class only holds what was loaded and
/// reacts to slicer/command input. The view is created once and reused (see MainWindow),
/// so Refresh() is called every time the user navigates back here, and it must never throw
/// past its own try/catch or the whole application would go down with it.
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private const int TopProductsCount = 5;

    public static readonly string[] DateRangePresets =
    {
        "Today", "Last 7 Days", "Last 30 Days", "This Month", "Custom Range"
    };

    private readonly DashboardService _dashboardService = new();

    public ObservableCollection<SalesTrendPoint> SalesTrend { get; } = new();
    public ObservableCollection<CategorySales> SalesByCategory { get; } = new();
    public ObservableCollection<TopSellingProduct> TopSellingProducts { get; } = new();
    public ObservableCollection<HeatmapCell> SalesHeatmap { get; } = new();
    public ObservableCollection<StockVsSalesPoint> StockVsSales { get; } = new();

    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<ProductFilterOption> Products { get; } = new();

    // --- KPI cards ---

    private decimal _periodSalesTotal;
    public decimal PeriodSalesTotal { get => _periodSalesTotal; private set => SetField(ref _periodSalesTotal, value); }

    private int _activeProductCount;
    public int ActiveProductCount { get => _activeProductCount; private set => SetField(ref _activeProductCount, value); }

    private int _lowStockCount;
    public int LowStockCount { get => _lowStockCount; private set => SetField(ref _lowStockCount, value); }

    private string? _bestSellingProductName;
    public string? BestSellingProductName { get => _bestSellingProductName; private set => SetField(ref _bestSellingProductName, value); }

    private int _bestSellingUnits;
    public int BestSellingUnits { get => _bestSellingUnits; private set => SetField(ref _bestSellingUnits, value); }

    public bool HasBestSellingProduct => !string.IsNullOrEmpty(BestSellingProductName);

    // --- Slicers ---

    private string _selectedDateRangePreset = "Today";
    public string SelectedDateRangePreset
    {
        get => _selectedDateRangePreset;
        set
        {
            if (SetField(ref _selectedDateRangePreset, value))
            {
                OnPropertyChanged(nameof(IsCustomRange));
                ApplyPresetDates();
                if (!IsCustomRange)
                {
                    Refresh();
                }
            }
        }
    }

    public bool IsCustomRange => SelectedDateRangePreset == "Custom Range";

    private DateTime _startDate = DateTime.Today;
    public DateTime StartDate { get => _startDate; set => SetField(ref _startDate, value); }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate { get => _endDate; set => SetField(ref _endDate, value); }

    private string? _selectedCategory;
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetField(ref _selectedCategory, value))
            {
                Refresh();
            }
        }
    }

    private ProductFilterOption? _selectedProduct;
    public ProductFilterOption? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetField(ref _selectedProduct, value))
            {
                Refresh();
            }
        }
    }

    // --- Loading / empty / error state ---

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; private set => SetField(ref _isLoading, value); }

    private bool _hasError;
    public bool HasError { get => _hasError; private set => SetField(ref _hasError, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; private set => SetField(ref _errorMessage, value); }

    public bool HasSalesTrendData => SalesTrend.Count > 0;
    public bool HasCategoryData => SalesByCategory.Count > 0;
    public bool HasTopProductsData => TopSellingProducts.Count > 0;
    public bool HasHeatmapData => SalesHeatmap.Count > 0;
    public bool HasStockVsSalesData => StockVsSales.Count > 0;

    public ICommand ApplyCustomRangeCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ViewProductsCommand { get; }
    public ICommand ViewLowStockCommand { get; }

    /// <summary>Raised when a KPI card should switch screens (and optionally filter to
    /// low-stock products), handled by MainWindow the same way the MiniMart Assistant's
    /// navigation requests are.</summary>
    public event EventHandler<(string Target, bool LowStockOnly)>? NavigationRequested;

    public DashboardViewModel()
    {
        ApplyCustomRangeCommand = new RelayCommand(_ => Refresh());
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        RefreshCommand = new RelayCommand(_ => Refresh());
        ViewProductsCommand = new RelayCommand(_ => NavigationRequested?.Invoke(this, ("Products", false)));
        ViewLowStockCommand = new RelayCommand(_ => NavigationRequested?.Invoke(this, ("Products", true)));

        ApplyPresetDates();
        LoadSlicerOptions();
        Refresh();
    }

    private void ApplyPresetDates()
    {
        var today = DateTime.Today;
        switch (SelectedDateRangePreset)
        {
            case "Today":
                StartDate = today;
                EndDate = today;
                break;
            case "Last 7 Days":
                StartDate = today.AddDays(-6);
                EndDate = today;
                break;
            case "Last 30 Days":
                StartDate = today.AddDays(-29);
                EndDate = today;
                break;
            case "This Month":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = today;
                break;
            case "Custom Range":
                // Leave StartDate/EndDate as whatever the user last picked.
                break;
        }
    }

    private void LoadSlicerOptions()
    {
        try
        {
            Categories.Clear();
            Categories.Add("All Categories");
            foreach (var category in _dashboardService.GetCategories())
            {
                Categories.Add(category);
            }
            _selectedCategory = "All Categories";
            OnPropertyChanged(nameof(SelectedCategory));

            Products.Clear();
            Products.Add(new ProductFilterOption { ProductId = null, Name = "All Products" });
            foreach (var product in _dashboardService.GetActiveProducts())
            {
                Products.Add(new ProductFilterOption { ProductId = product.Id, Name = product.Name });
            }
            _selectedProduct = Products[0];
            OnPropertyChanged(nameof(SelectedProduct));
        }
        catch (Exception ex)
        {
            Logger.LogError("DashboardViewModel.LoadSlicerOptions", ex);
        }
    }

    private void ResetFilters()
    {
        _selectedCategory = "All Categories";
        OnPropertyChanged(nameof(SelectedCategory));
        _selectedProduct = Products.Count > 0 ? Products[0] : null;
        OnPropertyChanged(nameof(SelectedProduct));

        _selectedDateRangePreset = "Today";
        OnPropertyChanged(nameof(SelectedDateRangePreset));
        OnPropertyChanged(nameof(IsCustomRange));
        ApplyPresetDates();

        Refresh();
    }

    private DashboardFilter BuildFilter() => new()
    {
        StartDate = StartDate,
        EndDate = EndDate,
        Category = string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "All Categories" ? null : SelectedCategory,
        ProductId = SelectedProduct?.ProductId
    };

    /// <summary>
    /// Reloads every KPI and chart from the current slicer selection. Safe to call
    /// repeatedly (e.g. every time MainWindow navigates back to the Dashboard) — it never
    /// lets an exception escape, so a database hiccup shows a friendly error instead of
    /// taking the whole application down.
    /// </summary>
    public void Refresh()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;

        try
        {
            var filter = BuildFilter();

            var summary = await _dashboardService.GetDashboardSummaryAsync(filter);
            PeriodSalesTotal = summary.PeriodSales;
            ActiveProductCount = summary.ActiveProductCount;
            LowStockCount = summary.LowStockCount;
            BestSellingProductName = summary.BestSellingProductName;
            BestSellingUnits = summary.BestSellingUnits;

            var trend = await _dashboardService.GetSalesTrendAsync(filter);
            SalesTrend.Clear();
            foreach (var point in trend) SalesTrend.Add(point);

            var byCategory = await _dashboardService.GetSalesByCategoryAsync(filter);
            SalesByCategory.Clear();
            foreach (var c in byCategory) SalesByCategory.Add(c);

            var topProducts = await _dashboardService.GetTopSellingProductsAsync(filter, TopProductsCount);
            TopSellingProducts.Clear();
            foreach (var p in topProducts) TopSellingProducts.Add(p);

            var heatmap = await _dashboardService.GetSalesHeatmapAsync(filter);
            SalesHeatmap.Clear();
            foreach (var h in heatmap) SalesHeatmap.Add(h);

            var stockVsSales = await _dashboardService.GetStockVsSalesAsync(filter);
            StockVsSales.Clear();
            foreach (var s in stockVsSales) StockVsSales.Add(s);
        }
        catch (Exception ex)
        {
            Logger.LogError("DashboardViewModel.LoadAsync", ex);
            HasError = true;
            ErrorMessage = "The dashboard could not be loaded due to an unexpected error. Please try Refresh.";

            SalesTrend.Clear();
            SalesByCategory.Clear();
            TopSellingProducts.Clear();
            SalesHeatmap.Clear();
            StockVsSales.Clear();
        }
        finally
        {
            IsLoading = false;
        }

        OnPropertyChanged(nameof(HasBestSellingProduct));
        OnPropertyChanged(nameof(HasSalesTrendData));
        OnPropertyChanged(nameof(HasCategoryData));
        OnPropertyChanged(nameof(HasTopProductsData));
        OnPropertyChanged(nameof(HasHeatmapData));
        OnPropertyChanged(nameof(HasStockVsSalesData));
    }
}
