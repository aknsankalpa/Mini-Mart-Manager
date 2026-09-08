using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RetailFlow.ViewModels;

/// <summary>
/// Base class for ViewModels. Implements INotifyPropertyChanged, which is how a ViewModel
/// tells WPF "one of my properties changed, please refresh anything bound to it in the UI."
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a backing field and raises PropertyChanged only if the value actually changed.
    /// Returns true when it changed, so callers can chain a follow-up action:
    /// if (SetField(ref _searchText, value)) LoadProducts();
    /// </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
