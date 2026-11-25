using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    private async void OnOpen(object? sender, RoutedEventArgs e) => await OpenAsync();

    private async void OnSave(object? sender, RoutedEventArgs e) => await SaveAsync();

    /// <summary>SIM-110 step 4: a model (save_model, export_model, or saved here) or a run record.</summary>
    private async Task OpenAsync()
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a model or a run record",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Model or run record (JSON)") { Patterns = new[] { "*.json" } }, FilePickerFileTypes.All },
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is string path) vm.OpenFile(path);
    }

    private async Task SaveAsync()
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the model",
            DefaultExtension = "json",
            SuggestedFileName = "model.json",
            FileTypeChoices = new[] { new FilePickerFileType("Model (JSON)") { Patterns = new[] { "*.json" } } },
        });
        if (file?.TryGetLocalPath() is string path) vm.SaveFile(path);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (e.KeyModifiers == KeyModifiers.Control && e.Key is Key.O or Key.S)
        {
            _ = e.Key == Key.O ? OpenAsync() : SaveAsync();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                vm.PlayPauseCommand.Execute(null);   // SIM-107: pause keeps the run
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
            case Key.R:
                vm.ToggleRenderModeCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.S:
                vm.ToggleStatsCommand.Execute(null);
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
