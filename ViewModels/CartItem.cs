namespace RetailFlow.ViewModels;

/// <summary>
/// One line of an in-progress sale, before it's saved. Not a database entity — once
/// Complete Sale succeeds, each CartItem becomes a SaleItem row. Inherits ViewModelBase
/// so changing Quantity (e.g. when the same product is added twice) refreshes the
/// LineTotal shown in the cart grid.
/// </summary>
public class CartItem : ViewModelBase
{
    public int ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }

    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetField(ref _quantity, value))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    // Always derived from UnitPrice and Quantity — never a value the user can type directly.
    public decimal LineTotal => UnitPrice * Quantity;
}
