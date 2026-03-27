using Avalonia.Controls;
using Avalonia.Input;
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
            {
                vm.Canvas = SimCanvas;
                vm.OwnerWindow = this;
            }
        };
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        switch (e.Key)
        {
            case Key.Space:
                if (vm.IsRunning) vm.StopCommand.Execute(null);
                else vm.StartCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.OemMinus or Key.Subtract:
                vm.FasterCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.OemPlus or Key.Add:
                vm.SlowerCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F or Key.F11:
                vm.ToggleFullscreenCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.D:
                vm.DetachControlsCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                if (vm.IsFullscreen)
                {
                    vm.ToggleFullscreenCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }
}
