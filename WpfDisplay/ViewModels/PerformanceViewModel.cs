using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Threading;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class PerformanceViewModel
{
    private readonly Workspace workspace;
    private DispatcherTimer dt;
    private int fpsCounter = 0;
    private ulong lastTotalIters = 0;

    //TODO: add total render time, account for pauses
    public int Fps => fpsCounter;
    public int InvocationIters => workspace.Renderer.InvocationIters;
    public string IterationSpeed => (workspace.Renderer.TotalIterations - lastTotalIters).ToKMB() + " /s";
    public string TotalIterations => workspace.Renderer.TotalIterations.ToKMB();

    public PerformanceViewModel(Workspace workspace)
    {
        this.workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        workspace.Renderer.DisplayFramebufferUpdated += (s, e) => fpsCounter++;
        dt = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (s, e) => UpdateValues(), Dispatcher.CurrentDispatcher);
        dt.Start();
    }

    public void UpdateValues()
    {
        OnPropertyChanged(nameof(Fps));
        fpsCounter = 0;

        lastTotalIters = Math.Min(workspace.Renderer.TotalIterations, lastTotalIters);
        OnPropertyChanged(nameof(IterationSpeed));
        lastTotalIters = workspace.Renderer.TotalIterations;

        OnPropertyChanged(nameof(TotalIterations));
        OnPropertyChanged(nameof(InvocationIters));
    }
}
