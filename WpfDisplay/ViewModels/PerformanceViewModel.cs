using System;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class PerformanceViewModel : ObservableObject
{
    private readonly Workspace _workspace;
    private readonly DispatcherTimer _dt;
    private ulong _lastTotalIters = 0;

    //TODO: add total render time, account for pauses
    public int Fps { get; private set; } = 0;
    public int InvocationIters => _workspace.Renderer.InvocationIters;
    public string IterationSpeed => (_workspace.Renderer.TotalIterations - _lastTotalIters).ToKMB() + " /s";
    public string TotalIterations => _workspace.Renderer.TotalIterations.ToKMB();

    public PerformanceViewModel(Workspace workspace)
    {
        _workspace = workspace;
        workspace.LoadedParamsChanged += (s, e) => OnPropertyChanged(string.Empty);
        workspace.Renderer.DisplayFramebufferUpdated += (s, e) => Fps++;
        _dt = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (s, e) => UpdateValues(), Dispatcher.CurrentDispatcher);
        _dt.Start();
    }

    public void UpdateValues()
    {
        OnPropertyChanged(nameof(Fps));
        Fps = 0;

        _lastTotalIters = Math.Min(_workspace.Renderer.TotalIterations, _lastTotalIters);
        OnPropertyChanged(nameof(IterationSpeed));
        _lastTotalIters = _workspace.Renderer.TotalIterations;

        OnPropertyChanged(nameof(TotalIterations));
        OnPropertyChanged(nameof(InvocationIters));
    }
}
