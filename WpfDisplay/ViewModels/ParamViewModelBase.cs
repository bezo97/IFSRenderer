using IFSEngine.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public abstract partial class ParamViewModelBase<T> : ObservableObject
{
    protected readonly Iterator iterator;
    protected readonly Workspace workspace;

    public string Name { get; protected set; }

    //TODO: min, max, increment, ..
    public ParamViewModelBase(string name, Iterator iterator, Workspace workspace)
    {
        Name = name;
        this.iterator = iterator;
        this.workspace = workspace;
        this.workspace.PropertyChanged += (s,e) => OnPropertyChanged(string.Empty);
    }
}
