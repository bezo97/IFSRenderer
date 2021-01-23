using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class IFSViewModel : ViewModelBase
    {
        private readonly Workspace workspace;

        public List<TransformFunction> RegisteredTransforms => workspace.Renderer.RegisteredTransforms;
        public ObservableCollection<IteratorViewModel> IteratorViewModels { get; private set; } = new ObservableCollection<IteratorViewModel>();


        private IteratorViewModel connectingIterator;

        private IteratorViewModel selectedIterator;
        public IteratorViewModel SelectedIterator
        {
            get => selectedIterator; 
            set
            {
                if(selectedIterator != null)
                    selectedIterator.IsSelected = false;

                Set(ref selectedIterator, value);
                RaisePropertyChanged(() => IsIteratorEditorVisible);

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

                Set(ref selectedConnection, value);
                RaisePropertyChanged(() => IsConnectionEditorVisible);

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
                var c = workspace.IFS.BackgroundColor;
                return Color.FromRgb(c.R, c.G, c.B);
            }
            set
            {
                workspace.IFS.BackgroundColor = System.Drawing.Color.FromArgb(255, value.R, value.G, value.B);
                workspace.Renderer.UpdateDisplay();
                RaisePropertyChanged(() => BackgroundColor);
            }
        }

        public FlamePalette Palette => workspace.IFS.Palette;

        public double FogEffect
        {
            get => workspace.IFS.FogEffect;
            set
            {
                workspace.IFS.FogEffect = value;
                workspace.Renderer.InvalidateAccumulation();
                RaisePropertyChanged(() => FogEffect);
            }
        }

        public IFSViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => RaisePropertyChanged(string.Empty);
            IteratorViewModels.CollectionChanged += (s, e) => workspace.Renderer.InvalidateParams();
            workspace.IFS.Iterators.ToList().ForEach(i => AddNewIteratorVM(i));
            workspace.PropertyChanged += (s, e) => {
                SelectedIterator = null;
                HandleIteratorsChanged(); 
            };
            HandleIteratorsChanged();
        }

        private IteratorViewModel AddNewIteratorVM(Iterator i)
        {
            var ivm = new IteratorViewModel(i, workspace)
            {
                RemoveCommand = RemoveSelectedCommand,
                DuplicateCommand = DuplicateSelectedCommand
            };
            ivm.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
            ivm.ViewChanged += (s, e) => { Redraw(); };
            ivm.ConnectEvent += (s, finish) =>
            {
                if (!finish)
                    connectingIterator = ivm;
                else if(connectingIterator != null)
                {
                    if (connectingIterator.iterator.WeightTo.ContainsKey(ivm.iterator))
                        connectingIterator.iterator.WeightTo.Remove(ivm.iterator);
                    else
                        connectingIterator.iterator.WeightTo[ivm.iterator] = 1.0;
                    HandleConnectionsChanged(connectingIterator);
                    SelectedConnection = connectingIterator.ConnectionViewModels.FirstOrDefault(c=>c.to==ivm);
                    connectingIterator = null;

                }
            };
            if (SelectedIterator != null)
            {
                ivm.XCoord = SelectedIterator.XCoord + (float)SelectedIterator.WeightedSize / 1.5f + (float)ivm.WeightedSize / 1.5f;
                ivm.YCoord = SelectedIterator.YCoord;
            }
            IteratorViewModels.Add(ivm);
            SelectedIterator = ivm;
            return ivm;
        }

        private void HandleIteratorsChanged()
        {
            var newIterators = workspace.IFS.Iterators.Where(i => !IteratorViewModels.Any(vm => vm.iterator == i));
            var removedIteratorVMs = IteratorViewModels.Where(vm => !workspace.IFS.Iterators.Any(i => vm.iterator == i));
            removedIteratorVMs.ToList().ForEach(vm => IteratorViewModels.Remove(vm));
            newIterators.ToList().ForEach(i => AddNewIteratorVM(i));

            //update connections vms:
            IteratorViewModels.ToList().ForEach(vm => HandleConnectionsChanged(vm));
        }
        private void HandleConnectionsChanged(IteratorViewModel vm)
        {
            var newConnections = vm.iterator.WeightTo.Where(w => !vm.ConnectionViewModels.Any(c => c.to.iterator == w.Key && w.Value > 0.0));
            var removedConnections = vm.ConnectionViewModels.Where(c => !vm.iterator.WeightTo.Any(i => c.to.iterator == i.Key && i.Value > 0.0));
            removedConnections.ToList().ForEach(vm2 => vm.ConnectionViewModels.Remove(vm2));
            newConnections.ToList().ForEach(c => vm.ConnectionViewModels.Add(new ConnectionViewModel(vm, IteratorViewModels.First(vm2=>vm2.iterator == c.Key), workspace)));
            Redraw();
        }

        public void Redraw()
        {
            foreach (var i in IteratorViewModels)
            {
                i.Redraw();
                foreach (var con in i.ConnectionViewModels)
                {
                    con.UpdateGeometry();
                }
            }
        }

        private RelayCommand<TransformFunction> _addIteratorCommand;
        public RelayCommand<TransformFunction> AddIteratorCommand
        {
            get => _addIteratorCommand ?? (
                _addIteratorCommand = new RelayCommand<TransformFunction>((tf) =>
                {
                    Iterator newIterator = new Iterator(tf);
                    workspace.IFS.AddIterator(newIterator, false);
                    if (SelectedIterator != null)
                    {
                        SelectedIterator.iterator.WeightTo[newIterator] = 1.0;
                        newIterator.WeightTo[SelectedIterator.iterator] = 1.0;
                    }
                    workspace.Renderer.InvalidateParams();
                    HandleIteratorsChanged();
                }));
        }

        private RelayCommand _removeSelectedCommand;
        public RelayCommand RemoveSelectedCommand
        {
            get => _removeSelectedCommand ?? (
                _removeSelectedCommand = new RelayCommand(() =>
                {
                    workspace.IFS.RemoveIterator(SelectedIterator.iterator);
                    SelectedIterator = null;
                    workspace.Renderer.InvalidateParams();
                    HandleIteratorsChanged();
                }));
        }

        private RelayCommand _duplicateSelectedCommand;
        public RelayCommand DuplicateSelectedCommand
        {
            get => _duplicateSelectedCommand ?? (
                _duplicateSelectedCommand = new RelayCommand(() =>
                {
                    var newIterator = workspace.IFS.DuplicateIterator(SelectedIterator.iterator);
                    RaisePropertyChanged("InvalidateParams");
                    HandleIteratorsChanged();
                    SelectedIterator = IteratorViewModels.First(vm=>vm.iterator == newIterator);
                }));
        }

        private RelayCommand _loadPaletteCommand;
        public RelayCommand LoadPaletteCommand
        {
            get => _loadPaletteCommand ?? (
                _loadPaletteCommand = new RelayCommand(() =>
                {
                    if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenPalette, out string path))
                    {
                        var palettes = FlamePalette.FromFile(path);
                        //TODO: Replace random choice with a palette picker here
                        //var palette = PalettePicker.ShowDialog(palettes);
                        workspace.IFS.Palette = palettes[new Random().Next(palettes.Count)];
                        workspace.Renderer.InvalidateParams();
                        RaisePropertyChanged(() => Palette);
                    }
                }));
        }

        private RelayCommand _reloadTransformsCommand;
        public RelayCommand ReloadTransformsCommand
        {
            get => _reloadTransformsCommand ?? (
                _reloadTransformsCommand = new RelayCommand(async () =>
                {
                    var loadedTransforms = System.IO.Directory.GetFiles(@".\Functions\Transforms")
                        .Select(file => TransformFunction.FromFile(file));
                    await workspace.Renderer.LoadTransforms(loadedTransforms);
                    RaisePropertyChanged(() => RegisteredTransforms);
                }));
        }

        private RelayCommand _openTransformsDirectoryCommand;
        public RelayCommand OpenTransformsDirectoryCommand
        {
            get => _openTransformsDirectoryCommand ?? (
                _openTransformsDirectoryCommand = new RelayCommand(() =>
                {
                    //show the directory with the os file explorer
                    System.Diagnostics.Process.Start(@".\Functions\Transforms");
                }));
        }

        private RelayCommand<string> _editTransformSourceCommand;
        public RelayCommand<string> EditTransformSourceCommand
        {
            get => _editTransformSourceCommand ?? (
                _editTransformSourceCommand = new RelayCommand<string>((filePath) =>
                {
                    //open transform source file with the preferred text editor
                    System.Diagnostics.Process.Start(filePath);
                }));
        }

        private RelayCommand<ConnectionViewModel> _selectConnectionCommand;
        public RelayCommand<ConnectionViewModel> SelectConnectionCommand
        {
            get => _selectConnectionCommand ?? (
                _selectConnectionCommand = new RelayCommand<ConnectionViewModel>((con) =>
                {
                    SelectedConnection = con;
                }));
        }

        private RelayCommand<IteratorViewModel> _selectIteratorCommand;
        public RelayCommand<IteratorViewModel> SelectIteratorCommand
        {
            get => _selectIteratorCommand ?? (
                _selectIteratorCommand = new RelayCommand<IteratorViewModel>((it) =>
                {
                    SelectedIterator = it;
                }));
        }

    }
}
