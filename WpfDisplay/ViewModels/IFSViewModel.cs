using IFSEngine.Model;
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
    private readonly Workspace _workspace;

    public CompositeCollection NodeMapElements { get; private set; }
    public IReadOnlyCollection<Transform> RegisteredTransforms => _workspace.LoadedTransforms;
    private readonly ObservableCollection<IteratorViewModel> _iteratorViewModels = new();
    private readonly ObservableCollection<ConnectionViewModel> _connectionViewModels = new();
    private IteratorViewModel _connectingIterator;
    private IteratorViewModel _selectedIterator;
    public IteratorViewModel SelectedIterator
    {
        get => _selectedIterator;
        set
        {
            if (_selectedIterator != null)
                _selectedIterator.IsSelected = false;

            SetProperty(ref _selectedIterator, value);
            OnPropertyChanged(nameof(IsIteratorEditorVisible));

            if (_selectedIterator != null)
            {
                SelectedConnection = null;
                _selectedIterator.IsSelected = true;
            }
        }
    }

    public Visibility IsIteratorEditorVisible => SelectedIterator == null ? Visibility.Collapsed : Visibility.Visible;

    private ConnectionViewModel _selectedConnection;
    public ConnectionViewModel SelectedConnection
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
    
    private ValueSliderViewModel _fogEffect;
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
        MinValue = 0,
    };

    public IFSViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _iteratorViewModels.CollectionChanged += (s, e) => workspace.Renderer.InvalidateParamsBuffer();
        workspace.PropertyChanged += (s, e) =>
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
            RemoveCommand = RemoveSelectedCommand,
            DuplicateCommand = DuplicateSelectedCommand
        };
        ivm.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        ivm.ConnectingStarted += Iterator_ConnectingStarted;
        ivm.ConnectingEnded += Iterator_ConnectingEnded;
        if (SelectedIterator != null)
        {
            float XCoord = SelectedIterator.XCoord + (float)SelectedIterator.NodeSize / 1.5f + (float)ivm.NodeSize / 1.5f;
            float YCoord = SelectedIterator.YCoord;
            ivm.UpdatePosition(XCoord, YCoord);
        }
        _iteratorViewModels.Add(ivm);
        return ivm;
    }

    private void Iterator_ConnectingStarted(object sender, EventArgs e)
    {
        _connectingIterator = (IteratorViewModel)sender;
    }

    private void Iterator_ConnectingEnded(object sender, EventArgs e)
    {
        var ivm = (IteratorViewModel)sender;
        _workspace.TakeSnapshot();
        if (_connectingIterator.iterator.WeightTo[ivm.iterator] > 0.0)
            _connectingIterator.iterator.WeightTo[ivm.iterator] = 0.0;
        else
            _connectingIterator.iterator.WeightTo[ivm.iterator] = 1.0;
        _workspace.Renderer.InvalidateParamsBuffer();

        HandleIteratorsChanged();
        SelectedConnection = _connectionViewModels.FirstOrDefault(c => c.from == _connectingIterator && c.to == ivm);
        _connectingIterator = null;
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

    [ICommand]
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
    }

    [ICommand]
    private void RemoveSelected()
    {
        if (SelectedIterator != null)
        {
            _workspace.TakeSnapshot();
            _workspace.Ifs.RemoveIterator(SelectedIterator.iterator);
            _workspace.Renderer.InvalidateParamsBuffer();
            SelectedIterator = null;
            HandleIteratorsChanged();
        }
        else if (SelectedConnection != null)
        {
            _workspace.TakeSnapshot();
            SelectedConnection.from.iterator.WeightTo[SelectedConnection.to.iterator] = 0.0;
            _workspace.Renderer.InvalidateParamsBuffer();
            SelectedConnection = null;
            HandleIteratorsChanged();
        }
    }

    [ICommand]
    private void DuplicateSelected()
    {
        if (SelectedIterator != null)
        {
            _workspace.TakeSnapshot();
            Iterator dupe = _workspace.Ifs.DuplicateIterator(SelectedIterator.iterator);
            _workspace.Renderer.InvalidateParamsBuffer();
            HandleIteratorsChanged();
            SelectedIterator = _iteratorViewModels.First(vm => vm.iterator == dupe);
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
                _workspace.TakeSnapshot();
                _workspace.Ifs.Palette = picker.SelectedPalette;
                _workspace.Renderer.InvalidateParamsBuffer();
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

    [ICommand]
    private void OpenTransformsDirectory()
    {
        //show the directory with the os file explorer
        Process.Start(new ProcessStartInfo
        {
            FileName = _workspace.TransformsDirectoryPath,
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
            _workspace.UndoHistory();
        }, () => _workspace.IsHistoryUndoable);

    private RelayCommand _redoCommand;
    public RelayCommand RedoCommand => _redoCommand
        ??= new(() =>
        {
            _workspace.RedoHistory();
        }, () => _workspace.IsHistoryRedoable);

    [ICommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();

}
