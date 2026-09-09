using System.Collections.ObjectModel;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.ViewModels;

/// <summary>
/// Drives the Transaction History screen: a searchable list of completed sales on the
/// left, and a receipt-style breakdown of whichever one is selected on the right.
/// </summary>
public class TransactionViewModel : ViewModelBase
{
    private readonly SalesService _salesService = new();

    public ObservableCollection<Sale> Transactions { get; } = new();
    public ObservableCollection<SaleItem> SelectedTransactionItems { get; } = new();

    public bool HasTransactions => Transactions.Count > 0;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) LoadTransactions(); }
    }

    private DateTime? _fromDate;
    public DateTime? FromDate
    {
        get => _fromDate;
        set { if (SetField(ref _fromDate, value)) LoadTransactions(); }
    }

    private DateTime? _toDate;
    public DateTime? ToDate
    {
        get => _toDate;
        set { if (SetField(ref _toDate, value)) LoadTransactions(); }
    }

    private Sale? _selectedTransaction;
    public Sale? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            if (SetField(ref _selectedTransaction, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                LoadDetail();
            }
        }
    }

    public bool HasSelection => SelectedTransaction is not null;

    public TransactionViewModel()
    {
        LoadTransactions();
    }

    /// <summary>
    /// Re-runs the current search/date filter against the database. Since this screen is
    /// created once and reused, MainWindow calls this every time it navigates here, so a
    /// sale completed elsewhere always shows up — property setters like SearchText only
    /// reload when the value actually changes, which navigation alone doesn't guarantee.
    /// </summary>
    public void Refresh() => LoadTransactions();

    private void LoadTransactions()
    {
        // Every screen is constructed eagerly at startup (see MainWindow.xaml.cs), so an
        // uncaught exception here would take the whole application down rather than just
        // leave this one screen showing nothing — see ProductViewModel.LoadProducts for
        // the same reasoning.
        try
        {
            var results = _salesService.SearchSales(SearchText, FromDate, ToDate);

            Transactions.Clear();
            foreach (var sale in results)
            {
                Transactions.Add(sale);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("TransactionViewModel.LoadTransactions", ex);
            Transactions.Clear();
        }

        OnPropertyChanged(nameof(HasTransactions));
    }

    private void LoadDetail()
    {
        SelectedTransactionItems.Clear();

        if (SelectedTransaction is null)
        {
            return;
        }

        try
        {
            var fullSale = _salesService.GetSaleWithItems(SelectedTransaction.Id);
            if (fullSale is null)
            {
                return;
            }

            foreach (var item in fullSale.SaleItems)
            {
                SelectedTransactionItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("TransactionViewModel.LoadDetail", ex);
        }
    }
}
