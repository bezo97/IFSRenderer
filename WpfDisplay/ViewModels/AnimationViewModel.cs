using IFSEngine.Animation;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public class AnimationViewModel : ObservableObject
{
    private readonly Workspace _workspace;
    public AnimationViewModel(Workspace workspace)
    {
        this._workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }
    public AnimationManager AnimationManager => _workspace.AnimationManager;

    private ICommand _playCommand;
    public ICommand PlayCommand => _playCommand ??= new RelayCommand(Play);

    public void Play()
    {
        var t = 0f;
        var startTime = DateTime.Now;
        var endTime = startTime.AddSeconds(AnimationManager.AnimationLength);
        var length = (endTime - startTime).Ticks;
        Task.Run(() =>
        {
            while (t < 1f)
            {
                //Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    t = (float)(DateTime.Now - startTime).Ticks / (float)length;
                    AnimationManager.EvaluateAt(t);
                }//);
            }
        });
    }
}
