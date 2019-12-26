using GalaSoft.MvvmLight;
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
        private readonly IFS ifs;

        public ObservableCollection<IteratorViewModel> IteratorViewModels { get; set; }
        

        private IteratorViewModel selectedIterator;
        public IteratorViewModel SelectedIterator
        {
            get => selectedIterator; set
            {
                selectedIterator.IsSelected = false;
                Set(ref selectedIterator, value);
                selectedIterator.IsSelected = true;
            }
        }

        public IFSViewModel(IFS ifs)
        {
            this.ifs = ifs;
            IteratorViewModels = new ObservableCollection<IteratorViewModel>(ifs.Iterators.Select(i => new IteratorViewModel(i)));
            ifs.PropertyChanged += Ifs_PropertyChanged;
            HandleIteratorsChanged();
        }

        private void HandleIteratorsChanged()
        {
            var newIterators = ifs.Iterators.Where(i => !IteratorViewModels.Any(vm => vm.iterator == i));
            var removedIteratorVMs = IteratorViewModels.Where(vm => !ifs.Iterators.Any(i => vm.iterator == i));
            removedIteratorVMs.ToList().ForEach(vm => IteratorViewModels.Remove(vm));
            newIterators.ToList().ForEach(i => IteratorViewModels.Add(new IteratorViewModel(i)));

            //update connections vms:
            IteratorViewModels.ToList().ForEach(vm => HandleConnectionsChanged(vm));
        }
        private void HandleConnectionsChanged(IteratorViewModel vm)
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
        }
    }
}
