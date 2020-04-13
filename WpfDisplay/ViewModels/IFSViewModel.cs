using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //TODO: remove, this is a test
        private RelayCommand _cycleSelectionCommand;
        public RelayCommand CycleSelectionCommand
        {
            get => _cycleSelectionCommand ?? (
                _cycleSelectionCommand = new RelayCommand(() =>
                {
                    SelectedIterator = IteratorViewModels[(IteratorViewModels.IndexOf(SelectedIterator) + 1) % IteratorViewModels.Count];
                }));
        }

        private RelayCommand _addIteratorCommand;
        public RelayCommand AddIteratorCommand
        {
            get => _addIteratorCommand ?? (
                _addIteratorCommand = new RelayCommand(() =>
                {
                    ifs.AddIterator(Iterator.RandomIterator, true);
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

        //renderer.LoadParams(new IFS(true));
        private RelayCommand _randomizeIfsCommand;
        public RelayCommand RandomizeIfsCommand
        {
            get => _randomizeIfsCommand ?? (
                _randomizeIfsCommand = new RelayCommand(() =>
                {
                    //TODO: 
                    SelectedIterator = null;
                }));
        }

    }
}
