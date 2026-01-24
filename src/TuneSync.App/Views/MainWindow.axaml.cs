using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TuneSync.App.ViewModels;

namespace TuneSync.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HandleSpacePressed();
            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HandleSpaceReleased();
            e.Handled = true;
        }
    }
}
