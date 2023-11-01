#nullable enable
using IFSEngine.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;
using static WpfDisplay.Helper.ForceDirectedGraphLayout;
using Transform = IFSEngine.Model.Transform;

namespace WpfDisplay.ViewModels;

public partial class IFSViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    public CompositeCollection NodeMapElements { get; private set; }
    public IReadOnlyCollection<Transform> RegisteredTransforms => _workspace.LoadedTransforms;
    public IReadOnlyCollection<ConnectionViewModel> ConnectionViewModels => _connectionViewModels;
    [ObservableProperty] private IteratorViewModel? _connectingIterator;
    private readonly ObservableCollection<IteratorViewModel> _iteratorViewModels = new();
    private readonly ObservableCollection<ConnectionViewModel> _connectionViewModels = new();
    private IteratorViewModel? _selectedIterator;
    public IteratorViewModel? SelectedIterator
    {
        get => _selectedIterator;
        set
        {
            if (_selectedIterator != null)
                _selectedIterator.IsSelected = false;

            SetProperty(ref _selectedIterator, value);

            OnPropertyChanged(nameof(IsIteratorEditorVisible));
            DuplicateSelectedCommand.NotifyCanExecuteChanged();
            SplitSelectedCommand.NotifyCanExecuteChanged();

            if (_selectedIterator != null)
            {
                SelectedConnection = null;
                _selectedIterator.IsSelected = true;
            }
        }
    }

    private bool _hasIteratorSelection => SelectedIterator != null;
    public Visibility IsIteratorEditorVisible => SelectedIterator == null ? Visibility.Collapsed : Visibility.Visible;

    private ConnectionViewModel? _selectedConnection;
    public ConnectionViewModel? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            if (_selectedConnection != null)
                _selectedConnection.IsSelected = false;

            SetProperty(ref _selectedConnection, value);
            OnPropertyChanged(nameof(IsConnectionEditorVisible));

            if (_selectedConnection != null)
            {
                SelectedIterator = null;
                _selectedConnection.IsSelected = true;
            }
        }
    }

    public Visibility IsConnectionEditorVisible => SelectedConnection == null ? Visibility.Collapsed : Visibility.Visible;

    public Color BackgroundColor
    {
        get
        {
            var c = _workspace.Ifs.BackgroundColor;
            return Color.FromRgb(c.R, c.G, c.B);
        }
        set
        {
            _workspace.Ifs.BackgroundColor = System.Drawing.Color.FromArgb(255, value.R, value.G, value.B);
            _workspace.Renderer.InvalidateDisplay();
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    public FlamePalette Palette => _workspace.Ifs.Palette;
    
    private ValueSliderViewModel? _fogEffect;
    public ValueSliderViewModel FogEffect => _fogEffect ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🌫 Fog effect",
        ToolTip = "Fades parts that are out of focus.",
        DefaultValue = IFS.Default.FogEffect,
        GetV = () => _workspace.Ifs.FogEffect,
        SetV = (value) => {
            _workspace.Ifs.FogEffect = value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        ValueWillChange = _workspace.TakeSnapshot,
        AnimationPath = "FogEffect",
        MinValue = 0,
    };

    public IFSViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _iteratorViewModels.CollectionChanged += (s, e) => workspace.Renderer.InvalidateParamsBuffer();
        workspace.LoadedParamsChanged += (s, e) =>
        {
            SelectedIterator = null;
            HandleIteratorsChanged();
            OnPropertyChanged(string.Empty);
        };

        NodeMapElements = new CompositeCollection()
            {
                new CollectionContainer(){Collection=_connectionViewModels },
                new CollectionContainer(){Collection=_iteratorViewModels },
            };

        workspace.Ifs.Iterators.ToList().ForEach(i => AddNewIteratorVM(i));
        HandleIteratorsChanged();
    }

    private IteratorViewModel AddNewIteratorVM(Iterator i)
    {
        var ivm = new IteratorViewModel(i, _workspace)
        {
            RemoveCommand = RemoveIteratorCommand,
            DuplicateCommand = DuplicateCommand,
            SplitCommand = SplitCommand,
        };
        ivm.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        ivm.ConnectingStarted += Iterator_ConnectingStarted;
        ivm.ConnectingEnded += Iterator_ConnectingEnded;
        if (SelectedIterator != null)
        {
            ivm.Position = new BindablePoint(
                SelectedIterator.Position.X + SelectedIterator.NodeSize / 1.5 + ivm.NodeSize / 1.5,
                SelectedIterator.Position.Y);
        }
        _iteratorViewModels.Add(ivm);
        return ivm;
    }

    private void Iterator_ConnectingStarted(object? sender, EventArgs e)
    {
        if (ConnectingIterator is null)
        {
            ConnectingIterator = (IteratorViewModel)sender!;
            ConnectingIterator.Redraw();
        }
    }

    private void Iterator_ConnectingEnded(object? sender, EventArgs e)
    {
        if (ConnectingIterator is null)
            return;

        var ivm = (IteratorViewModel)sender!;
        _workspace.TakeSnapshot();
        if (ConnectingIterator.iterator.WeightTo[ivm.iterator] > 0.0)
            ConnectingIterator.iterator.WeightTo[ivm.iterator] = 0.0;
        else
            ConnectingIterator.iterator.WeightTo[ivm.iterator] = 1.0;
        _workspace.Renderer.InvalidateParamsBuffer();

        HandleIteratorsChanged();
        SelectedConnection = _connectionViewModels.FirstOrDefault(c => c.from == ConnectingIterator && c.to == ivm);
        ConnectingIterator = null;
    }

    private void HandleIteratorsChanged()
    {
        //remove nodes
        var removedIteratorVMs = _iteratorViewModels.Where(vm => !_workspace.Ifs.Iterators.Any(i => vm.iterator == i)).ToList();
        removedIteratorVMs.ForEach(vm =>
        {
            _iteratorViewModels.Remove(vm);
        });
        //remove connections
        var removedConnections = _connectionViewModels.Where(c => !c.from.iterator.WeightTo.TryGetValue(c.to.iterator, out double ww) || ww == 0.0 || !_iteratorViewModels.Any(i => i == c.from) || !_iteratorViewModels.Any(i => i == c.to));
        removedConnections.ToList().ForEach(vm2 => _connectionViewModels.Remove(vm2));
        //add nodes
        var newIterators = _workspace.Ifs.Iterators.Where(i => !_iteratorViewModels.Any(vm => vm.iterator == i));
        newIterators.ToList().ForEach(i => AddNewIteratorVM(i));
        //add connections:
        _iteratorViewModels.ToList().ForEach(vm => HandleConnectionsChanged(vm));
        Redraw();
        OnPropertyChanged(nameof(NodeMapElements));
    }
    private void HandleConnectionsChanged(IteratorViewModel vm)
    {
        var newConnections = vm.iterator.WeightTo.Where(w => w.Value > 0.0 && !_connectionViewModels.Any(c => c.from == vm && c.to.iterator == w.Key));
        foreach (var c in newConnections)
        {
            var cvm = new ConnectionViewModel(_connectionViewModels, vm, _iteratorViewModels.First(vm2 => vm2.iterator == c.Key), _workspace);
            cvm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Weight")
                {
                    _workspace.Renderer.InvalidateParamsBuffer();
                    HandleIteratorsChanged();//ugh
                }
            };
            _connectionViewModels.Add(cvm);
        }
        vm.RaiseConnectionPropertyChanged();
    }

    public void Redraw()
    {
        foreach (var i in _iteratorViewModels)
        {
            i.Redraw();
        }
    }

    [RelayCommand]
    private void AddIterator(Transform tf)
    {
        _workspace.TakeSnapshot();
        Iterator newIterator = new(tf);
        _workspace.Ifs.AddIterator(newIterator, false);
        if (SelectedIterator != null)
        {
            SelectedIterator.iterator.WeightTo[newIterator] = 1.0;
            newIterator.WeightTo[SelectedIterator.iterator] = 1.0;
        }
        _workspace.Renderer.InvalidateParamsBuffer();
        HandleIteratorsChanged();
        SelectedIterator = _iteratorViewModels.First(vm => vm.iterator == newIterator);
        if(!_workspace.Renderer.IsRendering)//Start rendering when user started from blank params and added the first iterator.
            _workspace.Renderer.StartRenderLoop();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedConnection is not null)
        {
            _workspace.TakeSnapshot();
            SelectedConnection.from.iterator.WeightTo[SelectedConnection.to.iterator] = 0.0;
            _workspace.Renderer.InvalidateParamsBuffer();
            SelectedConnection = null;
            HandleIteratorsChanged();
        }
        else if (SelectedIterator is not null)
            RemoveIteratorCommand.Execute(SelectedIterator);
    }

    [RelayCommand]
    private void RemoveIterator(IteratorViewModel vm)
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.RemoveIterator(vm.iterator);
        _workspace.Renderer.InvalidateParamsBuffer();
        if(SelectedIterator == vm)
            SelectedIterator = null;
        HandleIteratorsChanged();
    }

    [RelayCommand(CanExecute=nameof(_hasIteratorSelection))]
    private void DuplicateSelected() => Duplicate(SelectedIterator!);

    [RelayCommand]
    private void Duplicate(IteratorViewModel vm)
    {
        _workspace.TakeSnapshot();
        Iterator dupe = _workspace.Ifs.DuplicateIterator(vm.iterator, splitWeights: false);
        _workspace.Renderer.InvalidateParamsBuffer();
        HandleIteratorsChanged();
        SelectedIterator = _iteratorViewModels.First(vm => vm.iterator == dupe);
    }

    [RelayCommand(CanExecute = nameof(_hasIteratorSelection))]
    private void SplitSelected() => Split(SelectedIterator!);

    [RelayCommand]
    private void Split(IteratorViewModel vm)
    {
        _workspace.TakeSnapshot();
        Iterator dupe = _workspace.Ifs.DuplicateIterator(vm.iterator, splitWeights: true);
        _workspace.Renderer.InvalidateParamsBuffer();
        HandleIteratorsChanged();
        SelectedIterator = _iteratorViewModels.First(vm => vm.iterator == dupe);
    }

    [RelayCommand]
    private async Task LoadPalette()
    {
        if (DialogHelper.ShowOpenPaletteDialog(out string path))
            await LoadPaletteFromFile(path);
    }

    /// <summary>
    /// From a drag & drop operation.
    /// </summary>
    [RelayCommand]
    private async Task DropPalette(string path)
    {
        await LoadPaletteFromFile(path);
    }

    private async Task LoadPaletteFromFile(string path)
    {
        var picker = new Views.PaletteDialogWindow
        {
            Owner = Application.Current.MainWindow,
            Palettes = await FlamePalette.FromFileAsync(path)
        };
        if (picker.ShowDialog() == true)
        {
            _workspace.TakeSnapshot();
            _workspace.Ifs.Palette = picker.SelectedPalette;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(Palette));
            Redraw();//update ColorRGB prop for nodes
            _workspace.UpdateStatusText($"Palette file loaded - {path}");
        }
    }

    [RelayCommand]
    private async Task ReloadTransforms()
    {
        try
        {
            await _workspace.ReloadTransforms();
            _workspace.UpdateStatusText("Transforms reloaded.");
        }
        catch (Exception ex)
        {
            _workspace.UpdateStatusText("Failed to reload transforms.");
            MessageBox.Show($"Failed to reload transforms.\r\n{ex.Message}", "Plugin error");
            return;
        }
        foreach (IteratorViewModel ivm in _iteratorViewModels)
            ivm.ReloadParameters();//handles when the number and names of parameters have changed.
        OnPropertyChanged(nameof(RegisteredTransforms));
    }

    [RelayCommand]
    private static void OpenTransformsDirectory()
    {
        //show the directory with the os file explorer
        Process.Start(new ProcessStartInfo
        {
            FileName = App.TransformsDirectoryPath,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private static void EditTransformSource(string filePath)
    {
        //open transform source file with the preferred text editor
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void SelectConnection(ConnectionViewModel con) => SelectedConnection = con;

    [RelayCommand]
    private void SelectIterator(IteratorViewModel it) => SelectedIterator = it;

    private bool _canExecuteUndo => _workspace.IsHistoryUndoable;
    [RelayCommand(CanExecute = "_canExecuteUndo")]
    private void Undo() => _workspace.UndoHistory();

    private bool _canExecuteRedo => _workspace.IsHistoryRedoable;
    [RelayCommand(CanExecute = "_canExecuteRedo")]
    private void Redo() => _workspace.RedoHistory();

    [RelayCommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();

    [RelayCommand]
    private void AutoLayoutNodes()
    {
        var vertices = _iteratorViewModels.Select(v => new Vector2((float)v.Position.X, (float)v.Position.Y)).ToList();
        var edges = _connectionViewModels.Where(e=>!e.IsLoopback).Select(e => (_iteratorViewModels.IndexOf(e.from), _iteratorViewModels.IndexOf(e.to))).ToList();
        Graph graph = new(vertices, edges);
        var nodePositions = GenerateLayout(graph, 1.0, 1.0, 0.000001);
        var avg = new Vector2(
            nodePositions.Average(x => x.X),
            nodePositions.Average(x => x.Y));
        nodePositions = nodePositions.ConvertAll(p => p - avg + new Vector2(500, 500));
        for (int i = 0; i < nodePositions.Count; i++)
        {
            _iteratorViewModels[i].Position = new BindablePoint(nodePositions[i].X, nodePositions[i].Y);
        }
    }

}
