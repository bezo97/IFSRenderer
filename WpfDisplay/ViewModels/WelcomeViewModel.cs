#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public sealed partial class WelcomeViewModel : ObservableObject
{
    public EventHandler<WelcomeWorkflow>? WorkflowSelected;
    public WelcomeWorkflow SelectedWorkflow { get; private set; } = WelcomeWorkflow.FromScratch;
    public IFS ExploreParams { get; private set; } = new IFS();

    [ObservableProperty] private string _selectedExpander = "0";

    [RelayCommand]
    private void StartFromScratch()
    {
        SelectedWorkflow = WelcomeWorkflow.FromScratch;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void LoadFile()
    {
        SelectedWorkflow = WelcomeWorkflow.LoadFile;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void BrowseRandoms()
    { 
        SelectedWorkflow = WelcomeWorkflow.BrowseRandoms;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void ExploreRandom()
    {
        SelectedWorkflow = WelcomeWorkflow.Explore;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }

    [RelayCommand]
    private void VisitSettings()
    {
        SelectedWorkflow = WelcomeWorkflow.VisitSettings;
        WorkflowSelected?.Invoke(this, SelectedWorkflow);
    }
}
