using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.ViewModels;

/// <summary>
/// Drives the Sales / POS screen: searching for a product, adding it to the cart with a
/// quantity, and completing the sale. Subtotal and Total are recalculated here from the
/// cart's own data every time it changes — nothing about the totals is ever typed directly
/// by the user.
/// </summary>
public class SalesViewModel : ViewModelBase
{
    private readonly ProductService _productService = new();
    private readonly SalesService _salesService = new();

    public ObservableCollection<Product> AvailableProducts { get; } = new();
    public ObservableCollection<CartItem> Cart { get; } = new();

    public ICommand AddToCartCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand CompleteSaleCommand { get; }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) LoadAvailableProducts(); }
    }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetField(ref _selectedProduct, value);
    }

    private string _quantityText = "1";
    public string QuantityText
    {
        get => _quantityText;
        set => SetField(ref _quantityText, value);
    }

    private string _discountText = "0";
    public string DiscountText
    {
        get => _discountText;
        set { if (SetField(ref _discountText, value)) RecalculateTotals(); }
    }

    private decimal _subtotal;
    public decimal Subtotal { get => _subtotal; private set => SetField(ref _subtotal, value); }

    private decimal _total;
    public decimal Total { get => _total; private set => SetField(ref _total, value); }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    private bool _isError;
    public bool IsError { get => _isError; set => SetField(ref _isError, value); }

    public SalesViewModel()
    {
        AddToCartCommand = new RelayCommand(_ => AddToCart());
        RemoveFromCartCommand = new RelayCommand(RemoveFromCart);
        CompleteSaleCommand = new RelayCommand(_ => CompleteSale(), _ => Cart.Count > 0);

        LoadAvailableProducts();
    }

    private void LoadAvailableProducts()
    {
        var results = _productService.Search(SearchText, includeInactive: false);

        AvailableProducts.Clear();
        foreach (var product in results)
        {
            AvailableProducts.Add(product);
        }
    }

    private void AddToCart()
    {
        if (SelectedProduct is null)
        {
            ShowError("Select a product before adding it to the cart.");
            return;
        }

        if (!int.TryParse(QuantityText, out var quantity) || quantity <= 0)
        {
            ShowError("Quantity must be a whole number greater than 0.");
            return;
        }

        // If this product is already in the cart, the real question is whether the
        // COMBINED quantity still fits in stock, not just the amount being added now.
        var existingLine = Cart.FirstOrDefault(item => item.ProductId == SelectedProduct.Id);
        var totalRequested = quantity + (existingLine?.Quantity ?? 0);

        if (totalRequested > SelectedProduct.StockQuantity)
        {
            ShowError($"Insufficient Stock\n\nOnly {SelectedProduct.StockQuantity} units of {SelectedProduct.Name} are currently available.");
            return;
        }

        if (existingLine is not null)
        {
            existingLine.Quantity = totalRequested;
        }
        else
        {
            Cart.Add(new CartItem
            {
                ProductId = SelectedProduct.Id,
                ProductCode = SelectedProduct.ProductCode,
                ProductName = SelectedProduct.Name,
                UnitPrice = SelectedProduct.Price,
                Quantity = quantity
            });
        }

        RecalculateTotals();
        CommandManager.InvalidateRequerySuggested();

        QuantityText = "1";
        StatusMessage = string.Empty;
        IsError = false;
    }

    private void RemoveFromCart(object? parameter)
    {
        if (parameter is CartItem item)
        {
            Cart.Remove(item);
            RecalculateTotals();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void CompleteSale()
    {
        // The Complete Sale button is disabled while the cart is empty, but this guard
        // stays as a safety net in case that ever changes.
        if (Cart.Count == 0)
        {
            ShowError("Add at least one product to the cart before completing the sale.");
            return;
        }

        if (!decimal.TryParse(DiscountText, out var discount))
        {
            ShowError("Discount must be a valid number.");
            return;
        }

        if (discount < 0)
        {
            ShowError("Discount cannot be negative.");
            return;
        }

        if (discount > Subtotal)
        {
            ShowError("Discount cannot be greater than the subtotal.");
            return;
        }

        var result = _salesService.CompleteSale(Cart.ToList(), discount);
        if (!result.Success)
        {
            ShowError(result.ErrorMessage);
            return;
        }

        MessageBox.Show(
            $"Sale completed successfully.\n\nInvoice: {result.InvoiceNumber}\nTotal: Rs. {Total:N2}",
            "Sale Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Cart.Clear();
        DiscountText = "0";
        LoadAvailableProducts(); // refresh so the "In Stock" column reflects the new, lower quantities
        RecalculateTotals();
        CommandManager.InvalidateRequerySuggested();
        StatusMessage = string.Empty;
        IsError = false;
    }

    private void RecalculateTotals()
    {
        Subtotal = Cart.Sum(item => item.LineTotal);

        decimal.TryParse(DiscountText, out var discount);
        if (discount < 0)
        {
            discount = 0;
        }

        Total = Subtotal - discount;
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        IsError = true;
    }
}
