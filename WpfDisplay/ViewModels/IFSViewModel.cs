﻿using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;
using Transform = IFSEngine.Model.Transform;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class IFSViewModel
{
    private readonly Workspace workspace;

    public CompositeCollection NodeMapElements { get; private set; }
    public IReadOnlyCollection<Transform> RegisteredTransforms => workspace.LoadedTransforms;
    private readonly ObservableCollection<IteratorViewModel> IteratorViewModels = new();
    private readonly ObservableCollection<ConnectionViewModel> ConnectionViewModels = new();
    private IteratorViewModel connectingIterator;
    private IteratorViewModel selectedIterator;
    public IteratorViewModel SelectedIterator
    {
        get => selectedIterator;
        set
        {
            if (selectedIterator != null)
                selectedIterator.IsSelected = false;

            SetProperty(ref selectedIterator, value);
            OnPropertyChanged(nameof(IsIteratorEditorVisible));

            if (selectedIterator != null)
            {
                SelectedConnection = null;
                selectedIterator.IsSelected = true;
            }
        }
    }

    public Visibility IsIteratorEditorVisible => SelectedIterator == null ? Visibility.Collapsed : Visibility.Visible;

    private ConnectionViewModel selectedConnection;
    public ConnectionViewModel SelectedConnection
    {
        get => selectedConnection;
        set
        {
            if (selectedConnection != null)
                selectedConnection.IsSelected = false;

            SetProperty(ref selectedConnection, value);
            OnPropertyChanged(nameof(IsConnectionEditorVisible));

            if (selectedConnection != null)
            {
                SelectedIterator = null;
                selectedConnection.IsSelected = true;
            }
        }
    }

    public Visibility IsConnectionEditorVisible => SelectedConnection == null ? Visibility.Collapsed : Visibility.Visible;

    public Color BackgroundColor
    {
        get
        {
            var c = workspace.Ifs.BackgroundColor;
            return Color.FromRgb(c.R, c.G, c.B);
        }
        set
        {
            workspace.Ifs.BackgroundColor = System.Drawing.Color.FromArgb(255, value.R, value.G, value.B);
            workspace.Renderer.InvalidateDisplay();
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    public FlamePalette Palette => workspace.Ifs.Palette;

    public double FogEffect
    {
        get => workspace.Ifs.FogEffect;
        set
        {
            workspace.Ifs.FogEffect = value;
            workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(FogEffect));
        }
    }

    public IFSViewModel(Workspace workspace)
    {
        this.workspace = workspace;
        IteratorViewModels.CollectionChanged += (s, e) => workspace.Renderer.InvalidateParamsBuffer();
        workspace.PropertyChanged += (s, e) =>
        {
            SelectedIterator = null;
            HandleIteratorsChanged();
            OnPropertyChanged(string.Empty);
        };

        NodeMapElements = new CompositeCollection()
            {
                new CollectionContainer(){Collection=ConnectionViewModels },
                new CollectionContainer(){Collection=IteratorViewModels },
            };

        workspace.Ifs.Iterators.ToList().ForEach(i => AddNewIteratorVM(i));
        HandleIteratorsChanged();
    }

    private IteratorViewModel AddNewIteratorVM(Iterator i)
    {
        var ivm = new IteratorViewModel(i, workspace)
        {
            RemoveCommand = RemoveSelectedCommand,
            DuplicateCommand = DuplicateSelectedCommand
        };
        ivm.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        ivm.ViewChanged += (s, e) => { Redraw(); };
        ivm.ConnectEvent += (s, finish) =>
        {
            if (!finish)
                connectingIterator = ivm;
            else if (connectingIterator != null)
            {
                workspace.TakeSnapshot();
                if (connectingIterator.iterator.WeightTo[ivm.iterator] > 0.0)
                    connectingIterator.iterator.WeightTo[ivm.iterator] = 0.0;
                else
                    connectingIterator.iterator.WeightTo[ivm.iterator] = 1.0;
                HandleIteratorsChanged();
                SelectedConnection = ConnectionViewModels.FirstOrDefault(c => c.from == connectingIterator && c.to == ivm);
                connectingIterator = null;

            }
        };
        if (SelectedIterator != null)
        {
            float XCoord = SelectedIterator.XCoord + (float)SelectedIterator.WeightedSize / 1.5f + (float)ivm.WeightedSize / 1.5f;
            float YCoord = SelectedIterator.YCoord;
            ivm.UpdatePosition(XCoord, YCoord);
        }
        IteratorViewModels.Add(ivm);
        return ivm;
    }

    private void HandleIteratorsChanged()
    {
        //remove nodes
        var removedIteratorVMs = IteratorViewModels.Where(vm => !workspace.Ifs.Iterators.Any(i => vm.iterator == i)).ToList();
        removedIteratorVMs.ForEach(vm =>
        {
            IteratorViewModels.Remove(vm);
        });
        //remove connections
        var removedConnections = ConnectionViewModels.Where(c => !c.from.iterator.WeightTo.TryGetValue(c.to.iterator, out double ww) || ww == 0.0 || !IteratorViewModels.Any(i => i == c.from) || !IteratorViewModels.Any(i => i == c.to));
        removedConnections.ToList().ForEach(vm2 => ConnectionViewModels.Remove(vm2));
        //add nodes
        var newIterators = workspace.Ifs.Iterators.Where(i => !IteratorViewModels.Any(vm => vm.iterator == i));
        newIterators.ToList().ForEach(i => AddNewIteratorVM(i));
        //add connections:
        IteratorViewModels.ToList().ForEach(vm => HandleConnectionsChanged(vm));
        Redraw();
        OnPropertyChanged(nameof(NodeMapElements));
    }
    private void HandleConnectionsChanged(IteratorViewModel vm)
    {
        var newConnections = vm.iterator.WeightTo.Where(w => w.Value > 0.0 && !ConnectionViewModels.Any(c => c.from == vm && c.to.iterator == w.Key));
        foreach (var c in newConnections)
        {
            var cvm = new ConnectionViewModel(ConnectionViewModels, vm, IteratorViewModels.First(vm2 => vm2.iterator == c.Key), workspace);
            cvm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Weight")
                {
                    workspace.Renderer.InvalidateParamsBuffer();
                    HandleIteratorsChanged();//ugh
                    }
            };
            ConnectionViewModels.Add(cvm);
        }
    }

    public void Redraw()
    {
        foreach (var i in IteratorViewModels)
        {
            i.Redraw();
        }
        foreach (var con in ConnectionViewModels)
        {
            con.UpdateGeometry();
        }
    }

    [ICommand]
    private void AddIterator(Transform tf)
    {
        workspace.TakeSnapshot();
        Iterator newIterator = new(tf);
        workspace.Ifs.AddIterator(newIterator, false);
        if (SelectedIterator != null)
        {
            SelectedIterator.iterator.WeightTo[newIterator] = 1.0;
            newIterator.WeightTo[SelectedIterator.iterator] = 1.0;
        }
        workspace.Renderer.InvalidateParamsBuffer();
        HandleIteratorsChanged();
        SelectedIterator = IteratorViewModels.First(vm => vm.iterator == newIterator);
    }

    [ICommand]
    private void RemoveSelected()
    {
        if (SelectedIterator != null)
        {
            workspace.TakeSnapshot();
            workspace.Ifs.RemoveIterator(SelectedIterator.iterator);
            workspace.Renderer.InvalidateParamsBuffer();
            SelectedIterator = null;
            HandleIteratorsChanged();
        }
        else if (SelectedConnection != null)
        {
            workspace.TakeSnapshot();
            SelectedConnection.from.iterator.WeightTo[SelectedConnection.to.iterator] = 0.0;
            workspace.Renderer.InvalidateParamsBuffer();
            SelectedConnection = null;
            HandleIteratorsChanged();
        }
    }

    [ICommand]
    private void DuplicateSelected()
    {
        if (SelectedIterator != null)
        {
            workspace.TakeSnapshot();
            Iterator dupe = workspace.Ifs.DuplicateIterator(SelectedIterator.iterator);
            workspace.Renderer.InvalidateParamsBuffer();
            HandleIteratorsChanged();
            SelectedIterator = IteratorViewModels.First(vm => vm.iterator == dupe);
        }
    }

    [ICommand]
    private async Task LoadPalette()
    {
        if (DialogHelper.ShowOpenPaletteDialog(out string path))
        {
            var picker = new Views.PaletteDialogWindow
            {
                Palettes = await FlamePalette.FromFileAsync(path)
            };
            if (picker.ShowDialog() == true)
            {
                workspace.TakeSnapshot();
                workspace.Ifs.Palette = picker.SelectedPalette;
                workspace.Renderer.InvalidateParamsBuffer();
                OnPropertyChanged(nameof(Palette));
                Redraw();//update ColorRGB prop for nodes
            }
        }
    }

    [ICommand]
    private async Task ReloadTransforms()
    {
        try
        {
            await workspace.ReloadTransforms();
            workspace.UpdateStatusText("Transforms reloaded.");
        }
        catch (Exception ex)
        {
            workspace.UpdateStatusText("Failed to reload transforms.");
            MessageBox.Show($"Failed to reload transforms.\r\n{ex.Message}", "Plugin error");
            return;
        }
        foreach (IteratorViewModel ivm in IteratorViewModels)
            ivm.ReloadParameters();//handles when the number and names of parameters have changed.
        OnPropertyChanged(nameof(RegisteredTransforms));
    }

    [ICommand]
    private void OpenTransformsDirectory()
    {
        //show the directory with the os file explorer
        Process.Start(new ProcessStartInfo
        {
            FileName = workspace.TransformsDirectoryPath,
            UseShellExecute = true
        });
    }

    [ICommand]
    private void EditTransformSource(string filePath)
    {
        //open transform source file with the preferred text editor
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }

    [ICommand]
    private void SelectConnection(ConnectionViewModel con) => SelectedConnection = con;

    [ICommand]
    private void SelectIterator(IteratorViewModel it) => SelectedIterator = it;

    // TODO: Check how to update the canExecute delegate

    private RelayCommand _undoCommand;
    public RelayCommand UndoCommand => _undoCommand
        ??= new(() =>
        {
            workspace.UndoHistory();
        }, () => workspace.IsHistoryUndoable);

    private RelayCommand _redoCommand;
    public RelayCommand RedoCommand => _redoCommand
        ??= new(() =>
        {
            workspace.RedoHistory();
        }, () => workspace.IsHistoryRedoable);

    [ICommand]
    private void TakeSnapshot() => workspace.TakeSnapshot();

}
