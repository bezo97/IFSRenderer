using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class IFSViewModel : ViewModelBase
    {
        private readonly Workspace workspace;

        //HACK: actully more related to the renderer than the ifs
        public List<TransformFunction> RegisteredTransforms => TransformFunction.LoadedTransformFunctions;

        public ObservableCollection<IteratorViewModel> IteratorViewModels { get; private set; } = new ObservableCollection<IteratorViewModel>();


        private IteratorViewModel connectingIterator;

        private IteratorViewModel selectedIterator;
        public IteratorViewModel SelectedIterator
        {
            get => selectedIterator; set
            {
                if(selectedIterator != null)
                    selectedIterator.IsSelected = false;

                Set(ref selectedIterator, value);

                if (selectedIterator != null)
                    selectedIterator.IsSelected = true;
            }
        }

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
            ivm.Selected += (s, e) => SelectedIterator = ivm;
            ivm.ConnectEvent += (s, finish) =>
            {
                if (!finish)
                    connectingIterator = ivm;
                else if(connectingIterator != null)
                {
                    if (connectingIterator.iterator.WeightTo.ContainsKey(ivm.iterator))
                        connectingIterator.iterator.WeightTo.Remove(ivm.iterator);
                    else
                        connectingIterator.iterator.WeightTo[ivm.iterator] = 0.5;//
                    HandleConnectionsChanged(connectingIterator);
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
            var newConnections = vm.iterator.WeightTo.Where(w => !vm.ConnectionViewModels.Any(c => c.to.iterator == w.Key));
            var removedConnections = vm.ConnectionViewModels.Where(c => !vm.iterator.WeightTo.Any(i => c.to.iterator == i.Key));
            removedConnections.ToList().ForEach(vm2 => vm.ConnectionViewModels.Remove(vm2));
            newConnections.ToList().ForEach(c => vm.ConnectionViewModels.Add(new ConnectionViewModel(vm, IteratorViewModels.First(vm2=>vm2.iterator == c.Key))));
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
                    workspace.IFS.DuplicateIterator(SelectedIterator.iterator);
                    RaisePropertyChanged("InvalidateParams");
                    HandleIteratorsChanged();
                    SelectedIterator = null;
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
                    }
                }));
        }

        private RelayCommand _reloadTransformsCommand;
        public RelayCommand ReloadTransformsCommand
        {
            get => _reloadTransformsCommand ?? (
                _reloadTransformsCommand = new RelayCommand(async () =>
                {
                    //TODO: Fix TransformFunctions reloading
                    //TransformFunction.LoadedTransformFunctions.Clear();
                    //var loadedTransforms = System.IO.Directory.GetFiles(@".\Functions\Transforms").Select(file => TransformFunction.FromString(System.IO.File.ReadAllText(file))).ToList();
                    ////await workspace.Renderer.WithContext(() =>
                    //{
                    //    workspace.Renderer.initRenderer(loadedTransforms);
                    //});
                    RaisePropertyChanged(()=>RegisteredTransforms);
                }));
        }

    }
}
