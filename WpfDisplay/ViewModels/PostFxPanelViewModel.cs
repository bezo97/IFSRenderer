#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class PostFxPanelViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    [ObservableProperty] public partial ObservableCollection<PostFxLayerViewModel> PostEffectLayers { get; set; } = [];

    /// <summary>
    /// All loaded posteffect plugins available to add as layer.
    /// </summary>
    public IEnumerable<EffectPlugin> LoadedEffects => _workspace.LoadedEffects;

    public PostFxPanelViewModel(Workspace workspace)
    {
        _workspace = workspace;
        workspace.LoadedParamsChanged += (s, e) => RefreshInstances();
        RefreshInstances();
    }

    private void RefreshInstances()
    {
        PostEffectLayers.Clear();
        foreach (var instance in _workspace.Ifs.PostEffects)
        {
            PostEffectLayers.Add(new PostFxLayerViewModel(instance, _workspace));
        }
        OnPropertyChanged(nameof(PostEffectLayers));
    }

    [RelayCommand]
    private void AddLayer(EffectPlugin? effectPlugin)
    {
        if (effectPlugin == null) return;
        _workspace.TakeSnapshot();
        _workspace.Ifs.AddPostEffectLayer(new EffectLayer(effectPlugin));
        _workspace.Renderer.InvalidateDisplay();
        RefreshInstances();
    }

    [RelayCommand]
    private void RemoveLayer(PostFxLayerViewModel vm)
    {
        _workspace.TakeSnapshot();
        _workspace.Ifs.RemovePostEffectLayer(vm.Instance);
        _workspace.Renderer.InvalidateDisplay();
        RefreshInstances();
        _workspace.OnParamSourceRemoved(vm.Instance.Id);
    }

    [RelayCommand]
    private void MoveUp(PostFxLayerViewModel vm)
    {
        var list = _workspace.Ifs.PostEffects.ToList();
        int index = list.IndexOf(vm.Instance);
        if (index > 0)
        {
            _workspace.TakeSnapshot();
            _workspace.Ifs.MovePostEffectLayer(vm.Instance, index - 1);
            _workspace.Renderer.InvalidateDisplay();
            RefreshInstances();
        }
    }

    [RelayCommand]
    private void MoveDown(PostFxLayerViewModel vm)
    {
        var list = _workspace.Ifs.PostEffects.ToList();
        int index = list.IndexOf(vm.Instance);
        if (index < _workspace.Ifs.PostEffects.Count - 1)
        {
            _workspace.TakeSnapshot();
            _workspace.Ifs.MovePostEffectLayer(vm.Instance, index + 1);
            _workspace.Renderer.InvalidateDisplay();
            RefreshInstances();
        }
    }

    [RelayCommand]
    private async Task ReloadEffects()
    {
        try
        {
            await _workspace.ReloadPlugins();
            RefreshInstances();
            OnPropertyChanged(nameof(LoadedEffects));
        }
        catch (System.Exception ex)
        {
            _workspace.UpdateStatusText("Failed to reload effect plugins.");
            System.Windows.MessageBox.Show($"Failed to reload postfx plugins.\r\n{ex.Message}", "Plugin error");
        }
    }

    [RelayCommand]
    private static void OpenPostFxDirectory()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = App.PostEffectsDirectoryPath,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private static void EditPostFxSource(string filePath)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}
