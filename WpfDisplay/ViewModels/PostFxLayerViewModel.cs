using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class PostFxLayerViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    public EffectLayer Instance { get; }
    public List<INotifyPropertyChanged> Parameters { get; } = [];

    public PostFxLayerViewModel(EffectLayer instance, Workspace workspace)
    {
        Instance = instance;
        _workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        ReloadParameters();
    }

    public void ReloadParameters()
    {
        Parameters.Clear();
        Parameters.AddRange(Instance.RealParams.Select(v => new RealParamViewModel(v.Key, Instance, _workspace)));
        Parameters.AddRange(Instance.Vec3Params.Select(v => new Vec3ParamViewModel(v.Key, Instance, _workspace)));
    }

    public string Name => Instance.Effect.Name;
    public string Version => Instance.Effect.Version;
    public string Description => Instance.Effect.Description;

    public bool Enabled
    {
        get => Instance.Enabled;
        set
        {
            if (Instance.Enabled != value)
            {
                _workspace.TakeSnapshot();
                Instance.Enabled = value;
                _workspace.Renderer.InvalidateDisplay();
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }

    [RelayCommand]
    private void EditPluginSource()
    {
        //open plugin source file with the preferred text editor
        Process.Start(new ProcessStartInfo
        {
            FileName = Instance.Effect.FilePath,
            UseShellExecute = true
        });
    }

}
