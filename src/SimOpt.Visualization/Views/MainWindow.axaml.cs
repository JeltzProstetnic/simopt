using Avalonia.Controls;
using SimOpt.Visualization.ViewModels;

namespace SimOpt.Visualization.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                vm.Canvas = SimCanvas;
        };
    }
}
