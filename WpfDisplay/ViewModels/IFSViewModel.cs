using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class IFSViewModel : ObservableObject
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

                SetProperty(ref selectedIterator, value);

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
            }
        }

        public IFSViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
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
            ivm.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
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

        private AsyncRelayCommand<TransformFunction> _addIteratorCommand;
        public AsyncRelayCommand<TransformFunction> AddIteratorCommand =>
            _addIteratorCommand ??= new AsyncRelayCommand<TransformFunction>(OnAddIteratorCommand);
        private async Task OnAddIteratorCommand(TransformFunction tf)
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
        }

        private AsyncRelayCommand _removeSelectedCommand;
        public AsyncRelayCommand RemoveSelectedCommand =>
            _removeSelectedCommand ??= new AsyncRelayCommand(OnRemoveSelectedCommand);
        private async Task OnRemoveSelectedCommand()
        {
            workspace.IFS.RemoveIterator(SelectedIterator.iterator);
            SelectedIterator = null;
            workspace.Renderer.InvalidateParams();
            HandleIteratorsChanged();
        }

        private AsyncRelayCommand _duplicateSelectedCommand;
        public AsyncRelayCommand DuplicateSelectedCommand =>
            _duplicateSelectedCommand ??= new AsyncRelayCommand(OnDuplicateSelectedCommand);
        private async Task OnDuplicateSelectedCommand()
        {
            workspace.IFS.DuplicateIterator(SelectedIterator.iterator);
            workspace.Renderer.InvalidateParams();
            HandleIteratorsChanged();
            SelectedIterator = null;
        }

        private AsyncRelayCommand _loadPaletteCommand;
        public AsyncRelayCommand LoadPaletteCommand =>
            _loadPaletteCommand ??= new AsyncRelayCommand(OnLoadPaletteCommand);
        private async Task OnLoadPaletteCommand()
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenPalette, out string path))
            {
                var palettes = FlamePalette.FromFile(path);
                //TODO: Replace random choice with a palette picker here
                //var palette = PalettePicker.ShowDialog(palettes);
                workspace.IFS.Palette = palettes[new Random().Next(palettes.Count)];
                workspace.Renderer.InvalidateParams();
            }
        }

        private AsyncRelayCommand _reloadTransformsCommand;
        public AsyncRelayCommand ReloadTransformsCommand =>
            _reloadTransformsCommand ??= new AsyncRelayCommand(OnReloadTransformsCommand);
        private async Task OnReloadTransformsCommand()
        {
            //TODO: Fix TransformFunctions reloading
            //TransformFunction.LoadedTransformFunctions.Clear();
            //var loadedTransforms = System.IO.Directory.GetFiles(@".\Functions\Transforms").Select(file => TransformFunction.FromString(System.IO.File.ReadAllText(file))).ToList();
            ////await workspace.Renderer.WithContext(() =>
            //{
            //    workspace.Renderer.initRenderer(loadedTransforms);
            //});
            OnPropertyChanged(nameof(RegisteredTransforms));
        }

    }
}
