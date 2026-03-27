using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimOpt.Visualization.Controls;
using SimOpt.Visualization.Models;

namespace SimOpt.Visualization.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _speed = 30;

    [ObservableProperty]
    private int _selectedTopology;

    public SimulationCanvas? Canvas { get; set; }

    public List<string> TopologyNames { get; } = new()
    {
        "SQSS (Source-Queue-Server-Sink)",
        "Parallel Servers",
        "Production Line",
        "Factory Floor (Physical Layout)"
    };

    private VizTopology GetSelectedTopology() => SelectedTopology switch
    {
        1 => VizTopology.ParallelServers(),
        2 => VizTopology.ProductionLine(),
        3 => VizTopology.FactoryFloor(),
        _ => VizTopology.Sqss()
    };

    partial void OnSpeedChanged(int value)
    {
        if (Canvas != null) Canvas.SpeedMs = value;
    }

    [RelayCommand]
    private void Start()
    {
        if (Canvas == null) return;
        var topology = GetSelectedTopology();
        Canvas.StartSimulation(topology, duration: 200.0, speedMs: Speed);
        IsRunning = true;
        StatusText = $"Running: {topology.Name}";
    }

    [RelayCommand]
    private void Stop()
    {
        Canvas?.StopSimulation();
        IsRunning = false;
        StatusText = "Stopped";
    }

    [RelayCommand]
    private void Faster() => Speed = Math.Max(5, Speed - 10);

    [RelayCommand]
    private void Slower() => Speed = Math.Min(200, Speed + 10);
}
