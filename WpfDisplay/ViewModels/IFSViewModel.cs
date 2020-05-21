using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using IFSEngine.TransformFunctions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WpfDisplay.ViewModels
{
    public class IFSViewModel : ObservableObject
    {
        public readonly IFS ifs;

        public ObservableCollection<IteratorViewModel> IteratorViewModels { get; set; } = new ObservableCollection<IteratorViewModel>();


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

        public IFSViewModel(IFS ifs)
        {
            this.ifs = ifs;
            ifs.Iterators.ToList().ForEach(i => addNewIteratorVM(i));
            ifs.PropertyChanged += Ifs_PropertyChanged;
            HandleIteratorsChanged();
        }

        private IteratorViewModel addNewIteratorVM(Iterator i)
        {
            var ivm = new IteratorViewModel(i);
            ivm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "BaseWeight")
                    ifs.NormalizeBaseWeights();
                RaisePropertyChanged("InvalidateParams");
            };
            ivm.ViewChanged += (s, e) => { Redraw(); };
            ivm.Selected += (s, e) => SelectedIterator = ivm;
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
                    Iterator preaffine = new Iterator { Transform = new Affine() };
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

    }
}
