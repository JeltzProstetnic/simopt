using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimOpt.Visualization.Controls;

namespace SimOpt.Visualization.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "Ready — press Start";

    [ObservableProperty]
    private bool _isRunning;

    // The canvas is bound via the view — commands relay to it
    public SimulationCanvas? Canvas { get; set; }

    [RelayCommand]
    private void Start()
    {
        if (Canvas == null) return;
        Canvas.StartSimulation(seed: 42, duration: 200.0, speedMs: 16);
        IsRunning = true;
        StatusText = "Simulation running...";
    }

    [RelayCommand]
    private void Stop()
    {
        Canvas?.StopSimulation();
        IsRunning = false;
        StatusText = "Simulation stopped";
    }
}
