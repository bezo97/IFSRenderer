using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using IFSEngine.TransformFunctions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using WpfDisplay.Helper;

namespace WpfDisplay.ViewModels
{
    public class IFSViewModel : ViewModelBase
    {
        //TODO: private
        public readonly IFS ifs;

        public ToneMappingViewModel ToneMappingViewModel { get; }
        public CameraSettingsViewModel CameraSettingsViewModel { get; }
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
                var c = ifs.BackgroundColor;
                return Color.FromRgb(c.R, c.G, c.B);
            }
            set
            {
                ifs.BackgroundColor = System.Drawing.Color.FromArgb(255, value.R, value.G, value.B);
                RaisePropertyChanged("InvalidateRender");
            }
        }

        public IFSViewModel(IFS ifs)
        {
            this.ifs = ifs;
            CameraSettingsViewModel = new CameraSettingsViewModel(ifs);
            CameraSettingsViewModel.PropertyChanged += (s,e) => RaisePropertyChanged(e.PropertyName);
            ToneMappingViewModel = new ToneMappingViewModel(ifs);
            ToneMappingViewModel.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
            IteratorViewModels.CollectionChanged += (s, e) => RaisePropertyChanged("InvalidateParams");
            ifs.Iterators.ToList().ForEach(i => AddNewIteratorVM(i));
            HandleIteratorsChanged();
        }

        private IteratorViewModel AddNewIteratorVM(Iterator i)
        {
            var ivm = new IteratorViewModel(i);
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
                ivm.XCoord = SelectedIterator.XCoord+(float)SelectedIterator.WeightedSize/1.5f+(float)ivm.WeightedSize/1.5f;
                ivm.YCoord = SelectedIterator.YCoord;
            }
            IteratorViewModels.Add(ivm);
            SelectedIterator = ivm;
            return ivm;
        }

        private void HandleIteratorsChanged()
        {
            var newIterators = ifs.Iterators.Where(i => !IteratorViewModels.Any(vm => vm.iterator == i));
            var removedIteratorVMs = IteratorViewModels.Where(vm => !ifs.Iterators.Any(i => vm.iterator == i));
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

        private RelayCommand<string> _addIteratorCommand;
        public RelayCommand<string> AddIteratorCommand
        {
            get => _addIteratorCommand ?? (
                _addIteratorCommand = new RelayCommand<string>((name) =>
                {
                    Iterator newIterator;
                    switch (name)
                    {//TODO: tmp solution
                        case "Affine":
                            newIterator = new Iterator { Transform = new Affine() };
                            break;
                        case "Foci":
                            newIterator = new Iterator { Transform = new Foci() };
                            break;
                        case "Loonie":
                            newIterator = new Iterator { Transform = new Loonie() };
                            break;
                        case "Spherical":
                            newIterator = new Iterator { Transform = new Spherical() };
                            break;
                        case "Waves":
                            newIterator = new Iterator { Transform = Waves.RandomWaves() };
                            break;
                        case "Moebius":
                            newIterator = new Iterator { Transform = new Moebius() };
                            break;
                        default:
                            newIterator = new Iterator { Transform = new Affine() };
                            break;
                    }
                    ifs.AddIterator(newIterator, false);
                    if (SelectedIterator != null)
                    {
                        SelectedIterator.iterator.WeightTo[newIterator] = 1.0;
                        newIterator.WeightTo[SelectedIterator.iterator] = 1.0;
                    }
                    RaisePropertyChanged("InvalidateParams");
                    HandleIteratorsChanged();
                }));
        }

        private RelayCommand _removeSelectedCommand;
        public RelayCommand RemoveSelectedCommand
        {
            get => _removeSelectedCommand ?? (
                _removeSelectedCommand = new RelayCommand(() =>
                {
                    ifs.RemoveIterator(SelectedIterator.iterator);
                    SelectedIterator = null;
                    RaisePropertyChanged("InvalidateParams");
                    HandleIteratorsChanged();
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
                        ifs.Palette = palettes[new Random().Next(palettes.Count)];
                        RaisePropertyChanged("InvalidateParams");
                    }
                }));
        }

    }
}
