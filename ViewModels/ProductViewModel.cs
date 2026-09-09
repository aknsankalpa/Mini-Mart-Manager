using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.ViewModels;

/// <summary>
/// Drives the Product Management screen: the searchable product list on the left, and the
/// add/edit form on the right. All the actual validation and saving is delegated to
/// ProductService — this class only holds UI state and reacts to button clicks.
/// </summary>
public class ProductViewModel : ViewModelBase
{
    private readonly ProductService _productService = new();

    public ObservableCollection<Product> Products { get; } = new();

    public bool HasProducts => Products.Count > 0;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) LoadProducts(); }
    }

    private bool _showInactive;
    public bool ShowInactive
    {
        get => _showInactive;
        set { if (SetField(ref _showInactive, value)) LoadProducts(); }
    }

    private bool _lowStockOnly;
    public bool LowStockOnly
    {
        get => _lowStockOnly;
        set { if (SetField(ref _lowStockOnly, value)) LoadProducts(); }
    }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetField(ref _selectedProduct, value))
            {
                OnPropertyChanged(nameof(ToggleActiveButtonText));
                CommandManager.InvalidateRequerySuggested();
                if (value is not null)
                {
                    LoadIntoForm(value);
                }
            }
        }
    }

    // Numeric fields are kept as strings so the TextBoxes can bind directly; they're
    // parsed and validated together with the rest of the form when Save is pressed.
    private int _editingProductId;

    private string _formProductCode = string.Empty;
    public string FormProductCode { get => _formProductCode; set => SetField(ref _formProductCode, value); }

    private string _formName = string.Empty;
    public string FormName { get => _formName; set => SetField(ref _formName, value); }

    private string _formCategory = string.Empty;
    public string FormCategory { get => _formCategory; set => SetField(ref _formCategory, value); }

    private string _formPrice = string.Empty;
    public string FormPrice { get => _formPrice; set => SetField(ref _formPrice, value); }

    private string _formStockQuantity = string.Empty;
    public string FormStockQuantity { get => _formStockQuantity; set => SetField(ref _formStockQuantity, value); }

    private string _formReorderLevel = string.Empty;
    public string FormReorderLevel { get => _formReorderLevel; set => SetField(ref _formReorderLevel, value); }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set { if (SetField(ref _isEditMode, value)) OnPropertyChanged(nameof(FormTitle)); }
    }

    public string FormTitle => IsEditMode ? "Edit Product" : "Add New Product";

    public string ToggleActiveButtonText =>
        SelectedProduct is { IsActive: true } ? "Deactivate Product" : "Reactivate Product";

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    private bool _isError;
    public bool IsError { get => _isError; set => SetField(ref _isError, value); }

    public ICommand NewProductCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ToggleActiveCommand { get; }

    public ProductViewModel()
    {
        NewProductCommand = new RelayCommand(_ => StartNewProduct());
        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => StartNewProduct());
        ToggleActiveCommand = new RelayCommand(_ => ToggleActive(), _ => SelectedProduct is not null);

        StartNewProduct();
        LoadProducts();
    }

    /// <summary>
    /// Re-runs the current search/filter against the database. Since this screen is
    /// created once and reused, MainWindow calls this every time it navigates here, so
    /// stock changed by a sale elsewhere is reflected immediately rather than only after
    /// the search text or a checkbox happens to change.
    /// </summary>
    public void Refresh() => LoadProducts();

    private void LoadProducts()
    {
        // A screen's own data load must not be allowed to throw: every View/ViewModel in
        // this app is constructed once, eagerly, when the main window starts up (see
        // MainWindow.xaml.cs), so an uncaught exception here would take down the whole
        // application before it ever shows a window — not just leave this one screen
        // showing no data. Catching here keeps a database problem contained to "this
        // screen has nothing to show right now" instead.
        try
        {
            var results = _productService.Search(SearchText, ShowInactive);

            if (LowStockOnly)
            {
                results = results.Where(p => ValidationHelper.IsLowStock(p.StockQuantity, p.ReorderLevel)).ToList();
            }

            Products.Clear();
            foreach (var product in results)
            {
                Products.Add(product);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("ProductViewModel.LoadProducts", ex);
            Products.Clear();
            ShowError("Products could not be loaded due to an unexpected error.");
        }

        OnPropertyChanged(nameof(HasProducts));
    }

    private void LoadIntoForm(Product product)
    {
        _editingProductId = product.Id;
        FormProductCode = product.ProductCode;
        FormName = product.Name;
        FormCategory = product.Category;
        FormPrice = product.Price.ToString("0.##");
        FormStockQuantity = product.StockQuantity.ToString();
        FormReorderLevel = product.ReorderLevel.ToString();
        IsEditMode = true;
        StatusMessage = string.Empty;
    }

    private void StartNewProduct()
    {
        _editingProductId = 0;
        FormProductCode = string.Empty;
        FormName = string.Empty;
        FormCategory = string.Empty;
        FormPrice = string.Empty;
        FormStockQuantity = string.Empty;
        FormReorderLevel = string.Empty;
        IsEditMode = false;
        SelectedProduct = null;
        StatusMessage = string.Empty;
    }

    private void Save()
    {
        // Numeric text is parsed before it ever reaches the service/database layer, so a
        // typo like "12o" is caught here with a plain-language message.
        if (!decimal.TryParse(FormPrice, out var price))
        {
            ShowError("Price must be a valid number.");
            return;
        }

        if (!int.TryParse(FormStockQuantity, out var stock))
        {
            ShowError("Stock quantity must be a whole number.");
            return;
        }

        if (!int.TryParse(FormReorderLevel, out var reorderLevel))
        {
            ShowError("Reorder level must be a whole number.");
            return;
        }

        var product = new Product
        {
            Id = _editingProductId,
            ProductCode = FormProductCode.Trim(),
            Name = FormName.Trim(),
            Category = FormCategory.Trim(),
            Price = price,
            StockQuantity = stock,
            ReorderLevel = reorderLevel
        };

        var result = IsEditMode
            ? _productService.UpdateProduct(product)
            : _productService.AddProduct(product);

        if (!result.Success)
        {
            ShowError(result.ErrorMessage);
            return;
        }

        var wasEdit = IsEditMode;
        StartNewProduct();
        LoadProducts();

        StatusMessage = wasEdit ? "Product updated successfully." : "Product added successfully.";
        IsError = false;
    }

    private void ToggleActive()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        var activating = !SelectedProduct.IsActive;
        var productName = SelectedProduct.Name;

        var confirmMessage = activating
            ? $"Reactivate '{productName}'? It will become available for sale again."
            : $"Deactivate '{productName}'? It will no longer appear on the Sales screen, but its past transactions are kept.";

        var confirm = MessageBox.Show(confirmMessage, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = _productService.SetActiveStatus(SelectedProduct.Id, activating);
        if (!result.Success)
        {
            ShowError(result.ErrorMessage);
            return;
        }

        StartNewProduct();
        LoadProducts();

        StatusMessage = activating ? "Product reactivated." : "Product deactivated.";
        IsError = false;
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        IsError = true;
    }
}
