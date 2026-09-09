using System.Windows;
using System.Windows.Threading;
using RetailFlow.Data;
using RetailFlow.Helpers;

namespace RetailFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // A last-resort safety net: anything that bubbles up from a ViewModel/Service
        // without being caught locally lands here instead of crashing the app or showing
        // WPF's default stack-trace dialog. Local try/catch blocks (in the services)
        // should still handle their own expected failures with a specific message —
        // this only exists for the unexpected case.
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            using var context = new AppDbContext();
            DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            Logger.LogError("Database initialization failed", ex);
            MessageBox.Show(
                "MiniMart Manager could not start because the database could not be prepared.\n\nPlease make sure no other copy of the application is running, then try again.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.LogError("Unhandled exception", e.Exception);

        MessageBox.Show(
            "Something went wrong and that action could not be completed.\n\nThe details have been logged. Please try again, and restart the application if the problem continues.",
            "Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Keep the application running rather than crashing — the user's cart, current
        // screen, etc. are still intact, and mid-demo is the worst time for a hard crash.
        e.Handled = true;
    }
}
