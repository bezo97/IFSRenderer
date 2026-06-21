using CommunityToolkit.Mvvm.ComponentModel;

using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public abstract partial class ParamViewModelBase<T> : ObservableObject
{
    protected readonly IParamSource source;
    protected readonly Workspace workspace;

    public string Name { get; protected set; }

    //TODO: min, max, increment, ..
    protected ParamViewModelBase(string name, IParamSource source, Workspace workspace)
    {
        Name = name;
        this.source = source;
        this.workspace = workspace;
        this.workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    protected void InvalidateRenderer()
    {
        if (source.RequiresParamsBufferInvalidation)
            workspace.Renderer.InvalidateParamsBuffer();
        else
            workspace.Renderer.InvalidateDisplay();
    }
}
