using System.Windows;
using RetailFlow.Data;

namespace RetailFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            using var context = new AppDbContext();
            DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"MiniMart Manager could not start because the database could not be prepared.\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
