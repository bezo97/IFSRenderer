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
                var c = ifs.ViewSettings.BackgroundColor;
                return Color.FromRgb(c.R, c.G, c.B);
            }
            set
            {
                ifs.ViewSettings.BackgroundColor = System.Drawing.Color.FromArgb(255, value.R, value.G, value.B);
            }
        }

        public IFSViewModel(IFS ifs)
        {
            this.ifs = ifs;
            CameraSettingsViewModel = new CameraSettingsViewModel(ifs);
            ToneMappingViewModel = new ToneMappingViewModel(ifs);
            ifs.Iterators.ToList().ForEach(i => addNewIteratorVM(i));
            ifs.PropertyChanged += Ifs_PropertyChanged;
            HandleIteratorsChanged();
        }

        private IteratorViewModel addNewIteratorVM(Iterator i)
        {
            var ivm = new IteratorViewModel(i);
            ivm.PropertyChanged += (s, e) =>
            {
                RaisePropertyChanged("InvalidateParams");
            };
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
                ivm.XCoord = SelectedIterator.XCoord+(float)SelectedIterator.WeightedSize+(float)ivm.WeightedSize;
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
            newIterators.ToList().ForEach(i => addNewIteratorVM(i));

            //update connections vms:
            IteratorViewModels.ToList().ForEach(vm => HandleConnectionsChanged(vm));
        }
        public void HandleConnectionsChanged(IteratorViewModel vm)
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

        private void Ifs_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Iterators":
                    HandleIteratorsChanged();
                    break;
                default:
                    break;
            }
            RaisePropertyChanged("InvalidateParams");
        }

        private RelayCommand<string> _addIteratorCommand;
        public RelayCommand<string> AddIteratorCommand
        {
            get => _addIteratorCommand ?? (
                _addIteratorCommand = new RelayCommand<string>((name) =>
                {
                    Iterator preaffine = new Iterator { Transform = new Affine(), Opacity = 0.0 };
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
                            newIterator = new Iterator { Transform = Waves.RandomWaves };
                            break;
                        case "Moebius":
                            newIterator = new Iterator { Transform = new Moebius() };
                            break;
                        default:
                            newIterator = new Iterator { Transform = new Affine() };
                            break;
                    }
                    ifs.AddIterator(preaffine, false);
                    ifs.AddIterator(newIterator, false);
                    preaffine.WeightTo[newIterator] = 1.0;
                    newIterator.WeightTo[preaffine] = 1.0;
                    //
                    if (SelectedIterator != null)
                        SelectedIterator.iterator.WeightTo[preaffine] = 1.0;
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
                        var palettes = UFPalette.FromFile(path);
                        //TODO: Replace random choice with a palette picker here
                        //var palette = PalettePicker.ShowDialog(palettes);
                        ifs.Palette = palettes[new Random().Next(palettes.Count)];
                        RaisePropertyChanged("InvalidateParams");
                    }
                }));
        }

    }
}
