using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

public partial class AssistantView : UserControl
{
    // Exposed so MainWindow can subscribe to NavigationRequested.
    public AssistantViewModel ViewModel { get; }

    public AssistantView()
    {
        InitializeComponent();
        ViewModel = new AssistantViewModel();
        DataContext = ViewModel;

        // Auto-scroll to the newest message — a chat transcript that doesn't keep up
        // with the conversation is worse than no scrolling at all.
        ViewModel.Messages.CollectionChanged += Messages_CollectionChanged;
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ConversationScrollViewer.ScrollToEnd();
    }

    private void AssistantInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.SendCommand.CanExecute(null))
        {
            ViewModel.SendCommand.Execute(null);
        }
    }
}
