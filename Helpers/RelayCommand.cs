using System.Windows.Input;

namespace RetailFlow.Helpers;

/// <summary>
/// A generic ICommand implementation so a Button in XAML (Command="{Binding SaveCommand}")
/// can run a plain method on the ViewModel, instead of every screen needing its own
/// hand-written ICommand class for every button.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    // WPF automatically re-checks CanExecute (e.g. to enable/disable a button) whenever
    // the user interacts with the UI, by raising this event via CommandManager.
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
