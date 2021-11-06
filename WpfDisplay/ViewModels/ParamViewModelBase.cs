using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public interface IParamViewModel : INotifyPropertyChanged { }
    public abstract class ParamViewModelBase<T> : ObservableObject, IParamViewModel
    {
        protected readonly Iterator iterator;
        protected readonly Workspace workspace;

        public string Name { get; protected set; }

        //TODO: min, max, increment, ..
        public ParamViewModelBase(string name, Iterator iterator, Workspace workspace)
        {
            this.Name = name;
            this.iterator = iterator;
            this.workspace = workspace;
        }
    }

}
