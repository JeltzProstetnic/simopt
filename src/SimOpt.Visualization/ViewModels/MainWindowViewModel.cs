using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimOpt.Visualization.Controls;

namespace SimOpt.Visualization.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _speed = 30;

    public SimulationCanvas? Canvas { get; set; }

    partial void OnSpeedChanged(int value)
    {
        if (Canvas != null) Canvas.SpeedMs = value;
    }

    [RelayCommand]
    private void Start()
    {
        if (Canvas == null) return;
        Canvas.StartSimulation(seed: 42, duration: 200.0, speedMs: Speed);
        IsRunning = true;
        StatusText = "Running";
    }

    [RelayCommand]
    private void Stop()
    {
        Canvas?.StopSimulation();
        IsRunning = false;
        StatusText = "Stopped";
    }

    [RelayCommand]
    private void Faster()
    {
        Speed = Math.Max(5, Speed - 10);
    }

    [RelayCommand]
    private void Slower()
    {
        Speed = Math.Min(200, Speed + 10);
    }
}
